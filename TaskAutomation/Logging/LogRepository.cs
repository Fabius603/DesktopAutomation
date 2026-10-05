using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Common.ApplicationData;

namespace TaskAutomation.Logging;

public interface ILogRepository
{
    event EventHandler<LogEvent>? EntryWritten;
    event EventHandler<LogRun>? RunChanged;
    string DirectoryPath { get; }
    LogPrivacy Privacy { get; }
    LogEvent Append(LogEvent entry);
    LogRun SaveRun(LogRun run);
    long NextExecutionNumber(Guid sourceId);
    IReadOnlyList<LogRun> ReadRuns();
    LogPage Query(LogQuery query, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>Only schema v2 files are discoverable. Old logs and existing configuration paths are untouched.</summary>
public sealed class LogRepository : ILogRepository, IDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new JsonStringEnumConverter() } };
    private readonly object _gate = new();
    private readonly object _ioGate = new();
    private readonly Channel<WriteRequest> _queue;
    private readonly Task _writer;
    private readonly TimeProvider _time;
    private readonly Dictionary<Guid, LogRun> _runs = new();
    private readonly Dictionary<Guid, LogEvent> _pending = new();
    private readonly HashSet<Guid> _active = [];
    private readonly HashSet<string> _issues = [];
    private readonly HashSet<(Guid Run, Guid Problem, ExecutionLogLevel Level)> _problems = [];
    private readonly Dictionary<Guid, long> _counters = new();
    private long _sequence;
    private bool _disposed;
    private string? _eventPath;
    public string DirectoryPath { get; }
    public LogPrivacy Privacy { get; } = new();
    public event EventHandler<LogEvent>? EntryWritten;
    public event EventHandler<LogRun>? RunChanged;

    public LogRepository() : this(AppPaths.StructuredLogsDirectory) { }
    public LogRepository(string directory, TimeProvider? time = null, int queueCapacity = 8192)
    {
        DirectoryPath = Path.GetFullPath(directory);
        _time = time ?? TimeProvider.System;
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.run.json"))
            {
                try
                {
                    var run = JsonSerializer.Deserialize<LogRun>(File.ReadAllText(path), JsonOptions);
                    if (run is not { SchemaVersion: LogEvent.CurrentSchema }) continue;
                    // No inferred end time: an old process never confirms a normal completion.
                    _runs[run.Id] = run.EndedAt is null ? run with { Outcome = LogOutcome.Interrupted, IsComplete = false } : run;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { _issues.Add("run.unreadable"); }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AddIssue("storage.unavailable"); }
        var stored = ReadStored(CancellationToken.None);
        _sequence = stored.Count == 0 ? 0 : stored.Max(entry => entry.Sequence);
        foreach (var entry in stored.Where(entry => entry.Context.RunId.HasValue && entry.Level >= ExecutionLogLevel.Warning))
            _problems.Add((entry.Context.RunId!.Value, entry.ProblemId ?? entry.Id, entry.Level));
        LoadHealth();
        foreach (var run in _runs.Values) _counters[run.SourceId] = Math.Max(_counters.GetValueOrDefault(run.SourceId), run.ExecutionNumber);
        try { ApplyRetention(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AddIssue("storage.unavailable"); }
        _queue = Channel.CreateBounded<WriteRequest>(new BoundedChannelOptions(queueCapacity)
        { SingleReader = true, FullMode = BoundedChannelFullMode.Wait, AllowSynchronousContinuations = false });
        _writer = Task.Run(WriteAsync);
    }

    public LogEvent Append(LogEvent entry)
    {
        LogEvent safe;
        lock (_gate)
        {
            if (_disposed) return Privacy.Sanitize(entry);
            safe = Privacy.Sanitize(entry with
            {
                SchemaVersion = LogEvent.CurrentSchema,
                Sequence = ++_sequence,
                Timestamp = entry.Timestamp == default ? _time.GetUtcNow() : entry.Timestamp
            });
            _pending[safe.Id] = safe;
            if (!_queue.Writer.TryWrite(new WriteRequest(safe, null, null)))
            {
                _pending.Remove(safe.Id);
                MarkLoss(safe.Context.RunId, "storage.overload");
            }
            if (safe.Context.RunId is { } id && _runs.TryGetValue(id, out var run))
            {
                // Count problems by their stable problem ID; generic warning messages remain separate observations.
                var error = safe.Level == ExecutionLogLevel.Error;
                var warning = safe.Level == ExecutionLogLevel.Warning;
                if ((error || warning) && safe.Code is not (LogCodes.RunCompleted or LogCodes.RunState)
                    && _problems.Add((id, safe.ProblemId ?? safe.Id, safe.Level)))
                {
                    run = run with
                    {
                        ErrorCount = run.ErrorCount + (error ? 1 : 0),
                        WarningCount = run.WarningCount + (warning ? 1 : 0),
                        PrimaryProblemId = run.PrimaryProblemId ?? safe.ProblemId ?? safe.Id
                    };
                    if (run.EndedAt.HasValue)
                        run = run with { Outcome = LogOutcomeRules.Complete(run.Outcome, run.ErrorCount, run.WarningCount) };
                    _runs[id] = run;
                }
            }
        }
        EntryWritten?.Invoke(this, safe);
        return safe;
    }

    public LogRun SaveRun(LogRun run)
    {
        lock (_gate)
        {
            if (_disposed) return Privacy.Sanitize(run);
            run = Privacy.Sanitize(run);
            if (_runs.TryGetValue(run.Id, out var current))
                run = run with
                {
                    ErrorCount = current.ErrorCount,
                    WarningCount = current.WarningCount,
                    CatalogSequence = current.CatalogSequence,
                    PrimaryProblemId = current.PrimaryProblemId,
                    LostEntries = current.LostEntries,
                    IsComplete = run.IsComplete && current.IsComplete
                };
            else run = run with { CatalogSequence = ++_sequence };
            _runs[run.Id] = run;
            if (run.EndedAt is null) _active.Add(run.Id); else _active.Remove(run.Id);
            if (!_queue.Writer.TryWrite(new WriteRequest(null, run.Id, null))) MarkLoss(run.Id, "storage.overload");
        }
        RunChanged?.Invoke(this, run);
        return run;
    }

    public IReadOnlyList<LogRun> ReadRuns()
    {
        lock (_gate) return _runs.Values.OrderByDescending(run => run.StartedAt).ThenBy(run => run.Id).ToArray();
    }

    public long NextExecutionNumber(Guid sourceId)
    {
        lock (_gate)
        {
            var next = _counters.GetValueOrDefault(sourceId) + 1;
            _counters[sourceId] = next;
            return next;
        }
    }

    public LogPage Query(LogQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.PageSize);
        if (query.PageSize > 10000) throw new ArgumentOutOfRangeException(nameof(query.PageSize));
        List<LogEvent> all;
        long snapshot;
        LogEvent[] pending;
        lock (_gate) { snapshot = Math.Min(query.SnapshotSequence, _sequence); pending = _pending.Values.ToArray(); }
        lock (_ioGate) all = ReadStored(cancellationToken);
        all.AddRange(pending);
        var available = all.Where(entry => entry.Sequence <= snapshot).DistinctBy(entry => entry.Id).ToArray();
        var matches = available.Where(entry => Matches(entry, query)).ToArray();
        var page = matches.Where(entry => entry.Sequence < query.BeforeSequence)
            .OrderByDescending(entry => entry.Sequence).Take(query.PageSize + 1).ToArray();
        string[] issues;
        lock (_gate) issues = _issues.Concat(query.RunId is { } runId && _runs.TryGetValue(runId, out var run) && !run.IsComplete
            ? new[] { "run.incomplete" } : Array.Empty<string>()).ToArray();
        return new LogPage(page.Take(query.PageSize).ToArray(), page.Length > query.PageSize ? page[query.PageSize - 1].Sequence : null,
            snapshot, matches.Length, available.Length,
            issues.Length > 0 ? (available.Length == 0 ? LogReadState.Unavailable : LogReadState.Partial)
                : matches.Length == 0 ? LogReadState.Empty : LogReadState.Available, issues);
    }

    private static bool Matches(LogEvent entry, LogQuery query)
        => (!query.Source.HasValue || entry.Source == query.Source)
        && (!query.SourceId.HasValue || entry.SourceId == query.SourceId)
        && (!query.RunId.HasValue || entry.Context.RunId == query.RunId)
        && entry.Sequence > query.AfterSequence
        && (!query.TriggerId.HasValue || entry.Context.TriggerId == query.TriggerId)
        && (!query.From.HasValue || entry.Timestamp >= query.From)
        && (!query.Until.HasValue || entry.Timestamp < query.Until)
        && entry.Level >= query.MinimumLevel
        && (!query.OnlyProblems || entry.Level >= ExecutionLogLevel.Warning)
        && (query.Area is null || entry.Area == query.Area)
        && (string.IsNullOrWhiteSpace(query.Search) || new[] { entry.Message, entry.Details, entry.SourceName, entry.Code, entry.Area }
            .Concat(entry.Parameters.Values).Any(value => value?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) == true));

    private List<LogEvent> ReadStored(CancellationToken ct)
    {
        var entries = new List<LogEvent>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.events.jsonl"))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    while (reader.ReadLine() is { } line)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            var entry = JsonSerializer.Deserialize<LogEvent>(line, JsonOptions);
                            if (entry is { SchemaVersion: LogEvent.CurrentSchema }) entries.Add(entry);
                        }
                        catch (JsonException) { AddIssue("events.corrupt"); }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AddIssue("events.unreadable"); }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AddIssue("storage.unavailable"); }
        return entries;
    }

    private void MarkLoss(Guid? runId, string issue)
    {
        _issues.Add(issue);
        if (runId is { } id && _runs.TryGetValue(id, out var run))
            _runs[id] = run with { LostEntries = run.LostEntries + 1, IsComplete = false };
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _queue.Writer.WriteAsync(new WriteRequest(null, null, barrier), cancellationToken).ConfigureAwait(false);
        await barrier.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteAsync()
    {
        await foreach (var request in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                if (request.Entry is { } entry)
                {
                    lock (_ioGate)
                    {
                        if (_eventPath is null || !File.Exists(_eventPath) || new FileInfo(_eventPath).Length > 10_000_000)
                        {
                            ApplyRetention();
                            _eventPath = Path.Combine(DirectoryPath, $"{_time.GetUtcNow():yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.events.jsonl");
                        }
                        File.AppendAllText(_eventPath, JsonSerializer.Serialize(entry, JsonOptions) + "\n", Encoding.UTF8);
                        lock (_gate) _pending.Remove(entry.Id);
                        if (entry.Context.RunId.HasValue) PersistRun(entry.Context.RunId.Value);
                    }
                }
                if (request.RunId is { } runId) PersistRun(runId);
                if (request.Barrier is { } barrier)
                {
                    foreach (var currentRun in ReadRuns()) PersistRun(currentRun.Id);
                    PersistHealth();
                    barrier.TrySetResult();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lock (_gate)
                {
                    if (request.Entry is { } entry) { _pending.Remove(entry.Id); MarkLoss(entry.Context.RunId, "storage.write-failed"); }
                    else MarkLoss(request.RunId, "storage.write-failed");
                }
                request.Barrier?.TrySetException(new IOException("Structured logs could not be flushed.", ex));
            }
        }
    }

    private void PersistRun(Guid id)
    {
        LogRun? run;
        lock (_gate) if (!_runs.TryGetValue(id, out run)) return;
        var path = Path.Combine(DirectoryPath, $"{id:N}.run.json");
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(run, JsonOptions), Encoding.UTF8);
        File.Move(path + ".tmp", path, true);
        PersistHealth();
    }

    private void ApplyRetention()
    {
        var cutoff = _time.GetUtcNow().AddDays(-30);
        long bytes = 0;
        foreach (var file in Directory.EnumerateFiles(DirectoryPath, "*.events.jsonl")
                     .Select(path => new FileInfo(path)).OrderByDescending(file => file.LastWriteTimeUtc))
        {
            bytes += file.Length;
            if (file.LastWriteTimeUtc >= cutoff.UtcDateTime && bytes <= 500L * 1024 * 1024) continue;
            var affected = ReadSegmentRunIds(file.FullName);
            lock (_gate) if (affected.Any(id => _active.Contains(id))) continue;
            try
            {
                file.Delete();
                lock (_gate)
                {
                    foreach (var id in affected)
                        if (_runs.TryGetValue(id, out var run)) _runs[id] = run with { IsComplete = false };
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _issues.Add("storage.retention-failed"); }
        }
        foreach (var run in ReadRuns().Where(run => run.EndedAt < cutoff
                     || run.Outcome == LogOutcome.Interrupted && run.StartedAt < cutoff).ToArray())
        {
            try { File.Delete(Path.Combine(DirectoryPath, $"{run.Id:N}.run.json")); lock (_gate) _runs.Remove(run.Id); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _issues.Add("storage.retention-failed"); }
        }
    }

    private void AddIssue(string issue) { lock (_gate) _issues.Add(issue); }
    private void LoadHealth()
    {
        try
        {
            var path = Path.Combine(DirectoryPath, "health.json");
            if (!File.Exists(path)) return;
            var health = JsonSerializer.Deserialize<Health>(File.ReadAllText(path), JsonOptions);
            if (health is null) return;
            _sequence = Math.Max(_sequence, health.Sequence);
            foreach (var issue in health.Issues) AddIssue(issue);
            foreach (var pair in health.Counters ?? []) _counters[pair.Key] = pair.Value;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { AddIssue("health.unreadable"); }
    }
    private void PersistHealth()
    {
        Health health;
        lock (_gate) health = new(_sequence, _issues.ToArray(), new(_counters));
        var path = Path.Combine(DirectoryPath, "health.json");
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(health, JsonOptions));
        File.Move(path + ".tmp", path, true);
    }
    private HashSet<Guid> ReadSegmentRunIds(string path)
    {
        var result = new HashSet<Guid>();
        foreach (var line in File.ReadLines(path))
        {
            try { if (JsonSerializer.Deserialize<LogEvent>(line, JsonOptions)?.Context.RunId is { } id) result.Add(id); }
            catch (JsonException) { AddIssue("events.corrupt"); }
        }
        return result;
    }
    private sealed record Health(long Sequence, string[] Issues, Dictionary<Guid, long>? Counters);

    public void Dispose()
    {
        lock (_gate) { if (_disposed) return; _disposed = true; _queue.Writer.TryComplete(); }
        _writer.GetAwaiter().GetResult();
        try { foreach (var run in ReadRuns()) PersistRun(run.Id); PersistHealth(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AddIssue("storage.write-failed"); }
    }
    private sealed record WriteRequest(LogEvent? Entry, Guid? RunId, TaskCompletionSource? Barrier);
}
