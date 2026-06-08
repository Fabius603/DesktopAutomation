using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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
            DateTimeOffset? endedAt = null)
        {
            Id = id;
            Kind = kind;
            SourceId = sourceId;
            Name = name;
            FilePath = filePath;
            StartedAt = startedAt ?? DateTimeOffset.Now;
            EndedAt = endedAt;
        }

        public Guid Id { get; }
        public ExecutionLogKind Kind { get; }
        public Guid SourceId { get; }
        public string Name { get; }
        public string FilePath { get; }
        public DateTimeOffset StartedAt { get; }
        public DateTimeOffset? EndedAt { get; internal set; }
        public bool IsRunning => EndedAt is null;
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
    }

    public interface IExecutionLogService
    {
        event EventHandler<ExecutionLogEntry>? EntryWritten;
        event EventHandler<ExecutionLogSession>? SessionChanged;

        IReadOnlyList<ExecutionLogSession> Sessions { get; }

        ExecutionLogSession BeginJob(Guid jobId, string jobName);
        void Write(ExecutionLogSession session, ExecutionLogLevel level, string message, string? details = null, string? stepId = null, string? stepType = null, long? durationMs = null);
        void Complete(ExecutionLogSession session, bool success, string? details = null);
        IReadOnlyList<ExecutionLogEntry> ReadEntries(Guid sessionId, int maxEntries = 2000);
        void ReloadSessions();
    }

    public sealed class ExecutionLogService : IExecutionLogService
    {
        private readonly object _gate = new();
        private readonly List<ExecutionLogSession> _sessions = new();
        private readonly Dictionary<Guid, List<ExecutionLogEntry>> _entries = new();
        private readonly string _rootDirectory;

        public event EventHandler<ExecutionLogEntry>? EntryWritten;
        public event EventHandler<ExecutionLogSession>? SessionChanged;

        public ExecutionLogService()
        {
            _rootDirectory = Path.Combine(AppContext.BaseDirectory, "Logs", "Executions");
            Directory.CreateDirectory(_rootDirectory);
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

        public ExecutionLogSession BeginJob(Guid jobId, string jobName)
            => Begin(ExecutionLogKind.Job, jobId, jobName);

        public void Write(
            ExecutionLogSession session,
            ExecutionLogLevel level,
            string message,
            string? details = null,
            string? stepId = null,
            string? stepType = null,
            long? durationMs = null)
        {
            var entry = new ExecutionLogEntry
            {
                SessionId = session.Id,
                Timestamp = DateTimeOffset.Now,
                Level = level,
                Message = message,
                Details = details,
                StepId = stepId,
                StepType = stepType,
                DurationMs = durationMs
            };

            lock (_gate)
            {
                if (!_entries.TryGetValue(session.Id, out var list))
                {
                    list = new List<ExecutionLogEntry>();
                    _entries[session.Id] = list;
                }

                list.Add(entry);
                File.AppendAllText(session.FilePath, FormatEntry(entry), Encoding.UTF8);
            }

            EntryWritten?.Invoke(this, entry);
        }

        public void Complete(ExecutionLogSession session, bool success, string? details = null)
        {
            session.EndedAt = DateTimeOffset.Now;
            var duration = session.EndedAt.Value - session.StartedAt;
            Write(
                session,
                success ? ExecutionLogLevel.Information : ExecutionLogLevel.Error,
                success ? "Ausführung beendet." : "Ausführung fehlerhaft beendet.",
                details,
                durationMs: (long)duration.TotalMilliseconds);
            SessionChanged?.Invoke(this, session);
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

        public void ReloadSessions()
        {
            var jobDirectory = Path.Combine(_rootDirectory, ExecutionLogKind.Job.ToString());
            Directory.CreateDirectory(jobDirectory);

            var sessions = Directory.EnumerateFiles(jobDirectory, "*.log")
                .Select(TryCreateSessionFromFile)
                .Where(session => session != null)
                .Cast<ExecutionLogSession>()
                .ToList();

            lock (_gate)
            {
                var existingPaths = new HashSet<string>(_sessions.Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);
                foreach (var session in sessions)
                {
                    if (existingPaths.Contains(session.FilePath))
                        continue;

                    _sessions.Add(session);
                }

                _sessions.Sort((a, b) => b.StartedAt.CompareTo(a.StartedAt));
            }
        }

        private ExecutionLogSession Begin(ExecutionLogKind kind, Guid sourceId, string name)
        {
            var sessionId = Guid.NewGuid();
            var directory = Path.Combine(_rootDirectory, kind.ToString());
            Directory.CreateDirectory(directory);

            var fileName = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}_{SafeFileName(name)}_{sessionId:N}.log";
            var session = new ExecutionLogSession(sessionId, kind, sourceId, name, Path.Combine(directory, fileName));

            lock (_gate)
            {
                _sessions.Insert(0, session);
                _entries[session.Id] = new List<ExecutionLogEntry>();
            }

            SessionChanged?.Invoke(this, session);
            Write(session, ExecutionLogLevel.Information, $"{kind} '{name}' gestartet.", $"SourceId={sourceId}");
            return session;
        }

        private static ExecutionLogSession? TryCreateSessionFromFile(string filePath)
        {
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

        private static string FormatEntry(ExecutionLogEntry entry)
        {
            var sb = new StringBuilder();
            sb.Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            sb.Append(" [").Append(entry.Level).Append(']');

            if (!string.IsNullOrWhiteSpace(entry.StepType))
                sb.Append(" [").Append(entry.StepType).Append(']');

            if (entry.DurationMs.HasValue)
                sb.Append(" [").Append(entry.DurationMs.Value).Append(" ms]");

            sb.Append(' ').Append(entry.Message);

            if (!string.IsNullOrWhiteSpace(entry.StepId))
                sb.Append(" StepId=").Append(entry.StepId);

            if (!string.IsNullOrWhiteSpace(entry.Details))
                sb.Append(" | ").Append(entry.Details);

            sb.AppendLine();
            return sb.ToString();
        }

        private static ExecutionLogEntry? TryParseEntry(Guid sessionId, string line)
        {
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

        private static IEnumerable<string> ReadLastLines(string filePath, int maxLines)
        {
            var queue = new Queue<string>(maxLines);
            foreach (var line in File.ReadLines(filePath, Encoding.UTF8))
            {
                if (queue.Count == maxLines)
                    queue.Dequeue();

                queue.Enqueue(line);
            }

            return queue.ToArray();
        }

        private static string SafeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            var safe = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "unnamed" : safe;
        }
    }
}
