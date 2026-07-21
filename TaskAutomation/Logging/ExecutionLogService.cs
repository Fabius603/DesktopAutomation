using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Orchestration;

namespace TaskAutomation.Logging
{
    public enum ExecutionLogKind
    {
        Job
    }

    public enum ExecutionLogLevel
    {
        Debug,
        Information,
        Warning,
        Error
    }

    public sealed class ExecutionLogSession
    {
        internal ExecutionLogSession(
            Guid id,
            ExecutionLogKind kind,
            Guid sourceId,
            string name,
            string filePath,
            DateTimeOffset? startedAt = null,
            DateTimeOffset? endedAt = null,
            long executionNumber = 0,
            JobStartContext? startContext = null,
            long? durationMs = null)
        {
            Id = id;
            Kind = kind;
            SourceId = sourceId;
            Name = name;
            FilePath = filePath;
            StartedAt = startedAt ?? DateTimeOffset.Now;
            EndedAt = endedAt;
            ExecutionNumber = executionNumber;
            StartContext = startContext ?? JobStartContext.Unknown;
            DurationMs = durationMs;
        }

        public Guid Id { get; }
        public ExecutionLogKind Kind { get; }
        public Guid SourceId { get; }
        public string Name { get; }
        public string FilePath { get; }
        public DateTimeOffset StartedAt { get; }
        public DateTimeOffset? EndedAt { get; internal set; }
        public long ExecutionNumber { get; }
        public JobStartContext StartContext { get; }
        public long? DurationMs { get; internal set; }
        public bool IsRunning => EndedAt is null;
        public JobExecutionState? JobState { get; internal set; }
    }

    public sealed class ExecutionLogEntry
    {
        public Guid SessionId { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public ExecutionLogLevel Level { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? Details { get; init; }
        public string? StepId { get; init; }
        public string? StepType { get; init; }
        public long? DurationMs { get; init; }
        public JobExecutionState? JobState { get; init; }
    }

    public interface IExecutionLogService
    {
        event EventHandler<ExecutionLogEntry>? EntryWritten;
        event EventHandler<ExecutionLogSession>? SessionChanged;

        IReadOnlyList<ExecutionLogSession> Sessions { get; }

        ExecutionLogSession BeginJob(Guid jobId, string jobName, JobStartContext? startContext = null);
        void Write(ExecutionLogSession session, ExecutionLogLevel level, string message, string? details = null, string? stepId = null, string? stepType = null, long? durationMs = null, JobExecutionState? jobState = null);
        void Complete(ExecutionLogSession session, bool success, string? details = null, bool cancelled = false);
        IReadOnlyList<ExecutionLogEntry> ReadEntries(Guid sessionId, int maxEntries = 2000);
        Task<IReadOnlyList<ExecutionLogEntry>> ReadEntriesAsync(Guid sessionId, int maxEntries = 2000, CancellationToken cancellationToken = default);
        void ReloadSessions();
    }

    public sealed class ExecutionLogService : IExecutionLogService, IDisposable
    {
        private readonly object _gate = new();
        private readonly List<ExecutionLogSession> _sessions = new();
        private readonly Dictionary<Guid, List<ExecutionLogEntry>> _entries = new();
        private readonly string _rootDirectory;
        private readonly BlockingCollection<PendingLogWrite> _writeQueue;
        private readonly Thread _writerThread;
        private readonly string _counterFilePath;
        private readonly Dictionary<Guid, long> _jobCounters = new();
        private bool _disposed;
        private const int MaxInMemorySessions = 200;
        private const int MaxEntriesPerSession = 3000;
        private const int MaxPendingLogWrites = 8192;
        private const int MaxWritesPerBatch = 256;
        private const int MaxStoredLogFiles = 1000;
        private const long MaxLogFileBytes = 50L * 1024 * 1024;
        private const long RetainedLogFileBytes = 25L * 1024 * 1024;
        private const long MaxStoredLogBytes = 500L * 1024 * 1024;
        private static readonly TimeSpan MaxLogAge = TimeSpan.FromDays(30);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };
        private long _droppedWrites;

        public event EventHandler<ExecutionLogEntry>? EntryWritten;
        public event EventHandler<ExecutionLogSession>? SessionChanged;

        public ExecutionLogService()
        {
            _rootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesktopAutomation",
                "Logs",
                "Executions");
            Directory.CreateDirectory(_rootDirectory);
            _counterFilePath = Path.Combine(_rootDirectory, "job-counters.json");
            LoadJobCounters();
            _writeQueue = new BlockingCollection<PendingLogWrite>(MaxPendingLogWrites);
            _writerThread = new Thread(WriteLogFiles)
            {
                IsBackground = true,
                Name = "ExecutionLogWriter"
            };
            _writerThread.Start();
            ReloadSessions();
        }

        public IReadOnlyList<ExecutionLogSession> Sessions
        {
            get
            {
                lock (_gate)
                    return _sessions.ToArray();
            }
        }

        public ExecutionLogSession BeginJob(Guid jobId, string jobName, JobStartContext? startContext = null)
            => Begin(ExecutionLogKind.Job, jobId, jobName, startContext ?? JobStartContext.Unknown);

        public void Write(
            ExecutionLogSession session,
            ExecutionLogLevel level,
            string message,
            string? details = null,
            string? stepId = null,
            string? stepType = null,
            long? durationMs = null,
            JobExecutionState? jobState = null)
        {
            if (jobState.HasValue)
                session.JobState = jobState;
            var entry = new ExecutionLogEntry
            {
                SessionId = session.Id,
                Timestamp = DateTimeOffset.Now,
                Level = level,
                Message = message,
                Details = details,
                StepId = stepId,
                StepType = stepType,
                DurationMs = durationMs,
                JobState = jobState ?? session.JobState
            };

            lock (_gate)
            {
                if (!_entries.TryGetValue(session.Id, out var list))
                {
                    list = new List<ExecutionLogEntry>();
                    _entries[session.Id] = list;
                }

                list.Add(entry);
                if (list.Count > MaxEntriesPerSession)
                    list.RemoveRange(0, list.Count - MaxEntriesPerSession);
            }

            if (!_disposed && !_writeQueue.IsAddingCompleted
                && !_writeQueue.TryAdd(new PendingLogWrite(session.FilePath, entry)))
            {
                Interlocked.Increment(ref _droppedWrites);
            }
            EntryWritten?.Invoke(this, entry);
        }

        public void Complete(ExecutionLogSession session, bool success, string? details = null, bool cancelled = false)
        {
            session.EndedAt = DateTimeOffset.Now;
            var duration = session.EndedAt.Value - session.StartedAt;
            session.DurationMs = (long)duration.TotalMilliseconds;
            var level = success || cancelled
                ? ExecutionLogLevel.Information
                : ExecutionLogLevel.Error;
            var message = cancelled
                ? "Ausführung gestoppt."
                : success
                    ? "Ausführung beendet."
                    : "Ausführung fehlerhaft beendet.";
            Write(
                session,
                level,
                message,
                details,
                durationMs: (long)duration.TotalMilliseconds);
            SessionChanged?.Invoke(this, session);
            WriteSessionMetadata(session);
        }

        public IReadOnlyList<ExecutionLogEntry> ReadEntries(Guid sessionId, int maxEntries = 2000)
        {
            ExecutionLogSession? session;
            lock (_gate)
            {
                session = _sessions.FirstOrDefault(s => s.Id == sessionId);
                if (session == null)
                    return Array.Empty<ExecutionLogEntry>();

                if (_entries.TryGetValue(sessionId, out var list) && list.Count > 0)
                    return Tail(list, maxEntries);
            }

            if (!File.Exists(session.FilePath))
                return Array.Empty<ExecutionLogEntry>();

            return ReadLastLines(session.FilePath, maxEntries)
                .Select(line => TryParseEntry(session.Id, line))
                .Where(entry => entry != null)
                .Cast<ExecutionLogEntry>()
                .ToArray();
        }

        public Task<IReadOnlyList<ExecutionLogEntry>> ReadEntriesAsync(
            Guid sessionId,
            int maxEntries = 2000,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = ReadEntries(sessionId, maxEntries);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken);
        }

        public void ReloadSessions()
        {
            var jobDirectory = Path.Combine(_rootDirectory, ExecutionLogKind.Job.ToString());
            Directory.CreateDirectory(jobDirectory);
            ApplyRetentionPolicy(jobDirectory);

            var sessions = Directory.EnumerateFiles(jobDirectory, "*.log")
                .Select(TryCreateSessionFromFile)
                .Where(session => session != null)
                .Cast<ExecutionLogSession>()
                .ToList();

            lock (_gate)
            {
                var discoveredPaths = new HashSet<string>(sessions.Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);
                _sessions.RemoveAll(session => !session.IsRunning && !discoveredPaths.Contains(session.FilePath));

                var existingPaths = new HashSet<string>(_sessions.Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);
                foreach (var session in sessions)
                {
                    if (existingPaths.Contains(session.FilePath))
                        continue;

                    _sessions.Add(session);
                }

                _sessions.Sort((a, b) => b.StartedAt.CompareTo(a.StartedAt));
                TrimInMemorySessionsLocked();

                var retainedSessionIds = _sessions.Select(session => session.Id).ToHashSet();
                foreach (var sessionId in _entries.Keys.Where(id => !retainedSessionIds.Contains(id)).ToList())
                    _entries.Remove(sessionId);
            }
        }

        private ExecutionLogSession Begin(ExecutionLogKind kind, Guid sourceId, string name, JobStartContext startContext)
        {
            var sessionId = Guid.NewGuid();
            var directory = Path.Combine(_rootDirectory, kind.ToString());
            Directory.CreateDirectory(directory);

            var fileName = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}_{SafeFileName(name)}_{sessionId:N}.log";
            long executionNumber;
            lock (_gate)
            {
                executionNumber = _jobCounters.GetValueOrDefault(sourceId) + 1;
                _jobCounters[sourceId] = executionNumber;
                SaveJobCountersLocked();
            }
            var session = new ExecutionLogSession(
                sessionId, kind, sourceId, name, Path.Combine(directory, fileName),
                executionNumber: executionNumber, startContext: startContext);
            File.WriteAllText(session.FilePath, string.Empty, Encoding.UTF8);
            WriteSessionMetadata(session);

            lock (_gate)
            {
                _sessions.Insert(0, session);
                _entries[session.Id] = new List<ExecutionLogEntry>();
                TrimInMemorySessionsLocked();
            }

            SessionChanged?.Invoke(this, session);
            Write(session, ExecutionLogLevel.Information, $"{kind} '{name}' gestartet.",
                $"SourceId={sourceId}, Run={executionNumber}, Origin={startContext.Source}, OriginName={startContext.SourceName}");
            return session;
        }

        private static ExecutionLogSession? TryCreateSessionFromFile(string filePath)
        {
            var metadataPath = filePath + ".meta.json";
            if (File.Exists(metadataPath))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<SessionMetadata>(File.ReadAllText(metadataPath), JsonOptions);
                    if (metadata != null)
                    {
                        return new ExecutionLogSession(
                            metadata.Id, metadata.Kind, metadata.SourceId, metadata.Name, filePath,
                            metadata.StartedAt, metadata.EndedAt ?? File.GetLastWriteTime(filePath),
                            metadata.ExecutionNumber, metadata.StartContext, metadata.DurationMs);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (JsonException) { }
            }
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var parts = fileName.Split('_');
            if (parts.Length < 3)
                return null;

            if (!DateTimeOffset.TryParseExact(
                    parts[0],
                    "yyyyMMdd-HHmmss-fff",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var startedAt))
            {
                startedAt = File.GetCreationTime(filePath);
            }

            var idText = parts[^1];
            var id = Guid.TryParseExact(idText, "N", out var parsedId) ? parsedId : Guid.NewGuid();
            var name = string.Join("_", parts.Skip(1).Take(parts.Length - 2));
            if (string.IsNullOrWhiteSpace(name))
                name = "Job";

            // Logs loaded from disk are never current in-memory runs.
            var endedAt = File.GetLastWriteTime(filePath);
            return new ExecutionLogSession(id, ExecutionLogKind.Job, Guid.Empty, name, filePath, startedAt, endedAt);
        }

        private void LoadJobCounters()
        {
            try
            {
                if (!File.Exists(_counterFilePath)) return;
                var stored = JsonSerializer.Deserialize<Dictionary<Guid, long>>(File.ReadAllText(_counterFilePath), JsonOptions);
                if (stored == null) return;
                foreach (var pair in stored) _jobCounters[pair.Key] = pair.Value;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (JsonException) { }
        }

        private void SaveJobCountersLocked()
        {
            try
            {
                var temporaryPath = _counterFilePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(_jobCounters, JsonOptions), Encoding.UTF8);
                File.Move(temporaryPath, _counterFilePath, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void WriteSessionMetadata(ExecutionLogSession session)
        {
            try
            {
                var metadata = new SessionMetadata(
                    session.Id, session.Kind, session.SourceId, session.Name, session.StartedAt,
                    session.EndedAt, session.ExecutionNumber, session.StartContext, session.DurationMs);
                File.WriteAllText(session.FilePath + ".meta.json", JsonSerializer.Serialize(metadata, JsonOptions), Encoding.UTF8);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string FormatEntry(ExecutionLogEntry entry)
        {
            return JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
        }

        private static ExecutionLogEntry? TryParseEntry(Guid sessionId, string line)
        {
            if (line.StartsWith('{'))
            {
                try
                {
                    return JsonSerializer.Deserialize<ExecutionLogEntry>(line, JsonOptions);
                }
                catch (JsonException)
                {
                    return null;
                }
            }

            if (line.Length < 27)
                return null;

            if (!DateTimeOffset.TryParseExact(
                    line[..23],
                    "yyyy-MM-dd HH:mm:ss.fff",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var timestamp))
            {
                return null;
            }

            var rest = line[23..].TrimStart();
            if (!TryReadBracket(ref rest, out var levelText) || !Enum.TryParse(levelText, out ExecutionLogLevel level))
                return null;

            string? stepType = null;
            long? durationMs = null;
            while (rest.StartsWith("[", StringComparison.Ordinal))
            {
                if (!TryReadBracket(ref rest, out var value))
                    break;

                if (value.EndsWith(" ms", StringComparison.OrdinalIgnoreCase)
                    && long.TryParse(value[..^3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDuration))
                {
                    durationMs = parsedDuration;
                }
                else
                {
                    stepType ??= value;
                }
            }

            string message = rest;
            string? details = null;
            string? stepId = null;

            var detailsIndex = rest.IndexOf(" | ", StringComparison.Ordinal);
            if (detailsIndex >= 0)
            {
                message = rest[..detailsIndex];
                details = rest[(detailsIndex + 3)..];
            }

            var stepIdIndex = message.IndexOf(" StepId=", StringComparison.Ordinal);
            if (stepIdIndex >= 0)
            {
                stepId = message[(stepIdIndex + 8)..];
                message = message[..stepIdIndex];
            }

            return new ExecutionLogEntry
            {
                SessionId = sessionId,
                Timestamp = timestamp,
                Level = level,
                Message = message,
                Details = details,
                StepId = stepId,
                StepType = stepType,
                DurationMs = durationMs
            };
        }

        private static bool TryReadBracket(ref string value, out string content)
        {
            content = string.Empty;
            value = value.TrimStart();
            if (!value.StartsWith("[", StringComparison.Ordinal))
                return false;

            var end = value.IndexOf(']');
            if (end <= 0)
                return false;

            content = value[1..end];
            value = value[(end + 1)..].TrimStart();
            return true;
        }

        private static IReadOnlyList<ExecutionLogEntry> Tail(List<ExecutionLogEntry> entries, int maxEntries)
            => entries.Count <= maxEntries ? entries.ToArray() : entries.Skip(entries.Count - maxEntries).ToArray();

        private void TrimInMemorySessionsLocked()
        {
            if (_sessions.Count <= MaxInMemorySessions)
                return;

            var retained = _sessions
                .Take(MaxInMemorySessions)
                .Select(s => s.Id)
                .ToHashSet();

            _sessions.RemoveRange(MaxInMemorySessions, _sessions.Count - MaxInMemorySessions);

            foreach (var sessionId in _entries.Keys.Where(id => !retained.Contains(id)).ToList())
                _entries.Remove(sessionId);
        }

        private static IEnumerable<string> ReadLastLines(string filePath, int maxLines)
        {
            if (maxLines <= 0)
                return Array.Empty<string>();

            const int blockSize = 64 * 1024;
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                blockSize,
                FileOptions.RandomAccess);

            if (stream.Length == 0)
                return Array.Empty<string>();

            var chunks = new List<byte[]>();
            var remaining = stream.Length;
            var newlineCount = 0;
            while (remaining > 0 && newlineCount <= maxLines)
            {
                var count = (int)Math.Min(blockSize, remaining);
                remaining -= count;
                stream.Position = remaining;

                var buffer = new byte[count];
                stream.ReadExactly(buffer);
                chunks.Add(buffer);
                newlineCount += buffer.Count(value => value == (byte)'\n');
            }

            var totalLength = chunks.Sum(chunk => chunk.Length);
            var bytes = new byte[totalLength];
            var offset = 0;
            for (var index = chunks.Count - 1; index >= 0; index--)
            {
                Buffer.BlockCopy(chunks[index], 0, bytes, offset, chunks[index].Length);
                offset += chunks[index].Length;
            }

            var text = Encoding.UTF8.GetString(bytes);
            var lines = text.Split('\n');
            var end = lines.Length;
            while (end > 0 && string.IsNullOrEmpty(lines[end - 1]))
                end--;

            var start = Math.Max(0, end - maxLines);
            return lines[start..end]
                .Select(line => line.TrimStart('\uFEFF').TrimEnd('\r'))
                .ToArray();
        }

        private static void ApplyRetentionPolicy(string directory)
        {
            try
            {
                var cutoff = DateTime.UtcNow - MaxLogAge;
                var files = Directory.EnumerateFiles(directory, "*.log")
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .ToList();

                foreach (var expired in files.Where(file => file.LastWriteTimeUtc < cutoff).ToList())
                {
                    TryDelete(expired);
                    files.Remove(expired);
                }

                var retainedBytes = 0L;
                for (var index = 0; index < files.Count; index++)
                {
                    var file = files[index];
                    retainedBytes += file.Exists ? file.Length : 0;
                    if (index >= MaxStoredLogFiles || retainedBytes > MaxStoredLogBytes)
                        TryDelete(file);
                }
            }
            catch (IOException)
            {
                // Retention must not prevent the application from starting.
            }
            catch (UnauthorizedAccessException)
            {
                // Retention must not prevent the application from starting.
            }
        }

        private static void TryDelete(FileInfo file)
        {
            try
            {
                var metadataPath = file.FullName + ".meta.json";
                file.Delete();
                if (File.Exists(metadataPath)) File.Delete(metadataPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void CompactIfNecessary(string filePath)
        {
            var file = new FileInfo(filePath);
            if (!file.Exists || file.Length <= MaxLogFileBytes)
                return;

            var tempPath = filePath + ".compact";
            try
            {
                using (var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    source.Position = Math.Max(0, source.Length - RetainedLogFileBytes);
                    if (source.Position > 0)
                    {
                        while (source.Position < source.Length && source.ReadByte() != '\n')
                        {
                        }
                    }

                    using var target = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    source.CopyTo(target);
                }

                File.Move(tempPath, filePath, true);
            }
            catch (IOException)
            {
                TryDelete(new FileInfo(tempPath));
            }
            catch (UnauthorizedAccessException)
            {
                TryDelete(new FileInfo(tempPath));
            }
        }

        private static string SafeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            var safe = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "unnamed" : safe;
        }

        private void WriteLogFiles()
        {
            while (!_writeQueue.IsCompleted)
            {
                try
                {
                    if (!_writeQueue.TryTake(out var first, Timeout.Infinite))
                        continue;

                    var batch = new List<PendingLogWrite>(MaxWritesPerBatch) { first };
                    while (batch.Count < MaxWritesPerBatch && _writeQueue.TryTake(out var next, 20))
                        batch.Add(next);

                    var dropped = Interlocked.Exchange(ref _droppedWrites, 0);
                    if (dropped > 0)
                    {
                        var marker = new ExecutionLogEntry
                        {
                            SessionId = first.Entry.SessionId,
                            Timestamp = DateTimeOffset.Now,
                            Level = ExecutionLogLevel.Warning,
                            Message = $"{dropped} Logeinträge wurden wegen Überlastung nicht in die Datei geschrieben."
                        };
                        batch.Insert(0, new PendingLogWrite(first.FilePath, marker));
                    }

                    foreach (var group in batch.GroupBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase))
                    {
                        var text = string.Concat(group.Select(item => FormatEntry(item.Entry)));
                        File.AppendAllText(group.Key, text, Encoding.UTF8);
                        CompactIfNecessary(group.Key);
                    }
                }
                catch (InvalidOperationException) when (_writeQueue.IsCompleted)
                {
                    break;
                }
                catch (IOException)
                {
                    // Logging must never break job execution. In-memory live logs still contain the entry.
                }
                catch (UnauthorizedAccessException)
                {
                    // Logging must never break job execution. In-memory live logs still contain the entry.
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _writeQueue.CompleteAdding();
            if (!_writerThread.Join(TimeSpan.FromSeconds(5)))
                return;

            _writeQueue.Dispose();
        }

        private readonly record struct PendingLogWrite(string FilePath, ExecutionLogEntry Entry);
        private sealed record SessionMetadata(
            Guid Id,
            ExecutionLogKind Kind,
            Guid SourceId,
            string Name,
            DateTimeOffset StartedAt,
            DateTimeOffset? EndedAt,
            long ExecutionNumber,
            JobStartContext StartContext,
            long? DurationMs);
    }
}
