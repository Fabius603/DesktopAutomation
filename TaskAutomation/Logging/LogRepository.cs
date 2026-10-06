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
    event EventHandler? StorageChanged { add { } remove { } }
    string DirectoryPath { get; }
    LogPrivacy Privacy { get; }
    LogEvent Append(LogEvent entry);
    LogRun SaveRun(LogRun run);
    long NextExecutionNumber(Guid sourceId);
    IReadOnlyList<LogRun> ReadRuns();
    LogPage Query(LogQuery query, CancellationToken cancellationToken = default);
    long SnapshotSequence => Query(new(PageSize: 1)).SnapshotSequence;
    LogPage QueryAll(LogQuery query, CancellationToken cancellationToken = default)
    {
        var first = Query(query with { PageSize = 10000 }, cancellationToken);
        var entries = first.Entries.ToList();
        var next = first.NextBeforeSequence;
        var issues = first.Issues.ToHashSet();
        while (next.HasValue)
        {
            var page = Query(query with { PageSize = 10000, SnapshotSequence = first.SnapshotSequence, BeforeSequence = next.Value }, cancellationToken);
            entries.AddRange(page.Entries); issues.UnionWith(page.Issues); next = page.NextBeforeSequence;
        }
        return first with { Entries = entries, NextBeforeSequence = null, Issues = issues.ToArray() };
    }
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
    private readonly ITimer _retentionTimer;
    private bool _retentionQueued;
    private bool _retentionDeferred;
    private static readonly TimeSpan RetentionInterval = TimeSpan.FromHours(1);
    private const long RetainedEventBytes = 500L * 1024 * 1024;
    private readonly Dictionary<Guid, LogRun> _runs = new();
    private readonly Dictionary<Guid, LogEvent> _pending = new();
    private readonly LogStepAggregation _aggregation = new();
    private readonly HashSet<Guid> _active = [];
    private readonly HashSet<string> _issues = [];
    private readonly HashSet<(Guid Run, Guid Problem, ExecutionLogLevel Level)> _problems = [];
    private readonly Dictionary<Guid, long> _counters = new();
    private long _sequence;
    private bool _disposed;
    private string? _eventPath;
    private DateOnly? _eventDate;
    private readonly Dictionary<string, SegmentCache> _segments = new();
    private const long CacheBytes = 64L * 1024 * 1024;
    private sealed record SegmentCache(long Length, DateTime LastWrite, LogEvent[] Entries);
    public long SnapshotSequence { get { lock (_gate) return _sequence; } }
    public string DirectoryPath { get; }
    public LogPrivacy Privacy { get; } = new();
    public event EventHandler<LogEvent>? EntryWritten;
    public event EventHandler<LogRun>? RunChanged;
    public event EventHandler? StorageChanged;

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
        ApplyRetention();
        _queue = Channel.CreateBounded<WriteRequest>(new BoundedChannelOptions(queueCapacity)
        { SingleReader = true, FullMode = BoundedChannelFullMode.Wait, AllowSynchronousContinuations = false });
        _writer = Task.Run(WriteAsync);
        _retentionTimer = _time.CreateTimer(_ => QueueRetention(), null, RetentionInterval, RetentionInterval);
    }

    private void QueueRetention(bool onlyIfDeferred = false)
    {
        lock (_gate)
        {
            if (_disposed || _retentionQueued || onlyIfDeferred && !_retentionDeferred) return;
            _retentionQueued = _queue.Writer.TryWrite(new WriteRequest(null, null, null, true));
        }
    }

    public LogEvent Append(LogEvent entry)
    {
        LogEvent safe;
        LogRun? changedRun = null;
        var summarized = false;
        lock (_gate)
        {
            if (_disposed) return Privacy.Sanitize(entry);
            safe = Privacy.Sanitize(entry with
            {
                SchemaVersion = LogEvent.CurrentSchema,
                Sequence = ++_sequence,
                Timestamp = entry.Timestamp == default ? _time.GetUtcNow() : entry.Timestamp,
                Category = LogPresentation.Category(entry)
            });
            if (safe.Context.RunId is { } aggregateRunId && _runs.TryGetValue(aggregateRunId, out var aggregateRun))
            {
                summarized = _aggregation.Observe(safe, ref aggregateRun, out var start, out var summaryChanged);
                if (start is not null) QueueEntry(start);
                if (summaryChanged)
                {
                    _runs[aggregateRunId] = aggregateRun;
                    if (_aggregation.ShouldSave(aggregateRunId, _time.GetUtcNow()))
                    {
                        if (!_queue.Writer.TryWrite(new WriteRequest(null, aggregateRunId, null))) MarkLoss(aggregateRunId, "storage.overload");
                        changedRun = _runs[aggregateRunId];
                    }
                }
            }
            if (!summarized) QueueEntry(safe);
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
                    changedRun = run;
                }
            }
        }
        if (!summarized || safe.Code == LogCodes.StepStarted) EntryWritten?.Invoke(this, safe);
        if (changedRun is not null) RunChanged?.Invoke(this, changedRun);
        return safe;
    }

    private void QueueEntry(LogEvent entry)
    {
        _pending[entry.Id] = entry;
        if (_queue.Writer.TryWrite(new WriteRequest(entry, null, null))) return;
        _pending.Remove(entry.Id);
        MarkLoss(entry.Context.RunId, "storage.overload");
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
                    IsComplete = run.IsComplete && current.IsComplete,
                    StepSummaries = current.StepSummaries,
                    CompletedIterations = current.CompletedIterations,
                    IterationDurationMs = current.IterationDurationMs
                };
            else run = run with { CatalogSequence = ++_sequence };
            _runs[run.Id] = run;
            if (run.EndedAt is null) _active.Add(run.Id); else _active.Remove(run.Id);
            if (run.EndedAt.HasValue) _aggregation.EndRun(run.Id);
            if (!_queue.Writer.TryWrite(new WriteRequest(null, run.Id, null))) MarkLoss(run.Id, "storage.overload");
        }
        RunChanged?.Invoke(this, run);
        if (run.EndedAt.HasValue) QueueRetention(onlyIfDeferred: true);
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
        return QueryCore(query, false, cancellationToken);
    }

    public LogPage QueryAll(LogQuery query, CancellationToken cancellationToken = default)
        => QueryCore(query, true, cancellationToken);

    private LogPage QueryCore(LogQuery query, bool allMatches, CancellationToken cancellationToken)
    {
        List<LogEvent> all;
        long snapshot;
        LogEvent[] pending;
        lock (_gate) { snapshot = Math.Min(query.SnapshotSequence, _sequence); pending = _pending.Values.Concat(_aggregation.PendingStarts).ToArray(); }
        lock (_ioGate) all = ReadStored(cancellationToken);
        all.AddRange(pending);
        var available = all.Where(entry => entry.Sequence <= snapshot).DistinctBy(entry => entry.Id).ToArray();
        var matches = available.Where(entry => Matches(entry, query)).ToArray();
        var page = matches.Where(entry => entry.Sequence < query.BeforeSequence)
            .OrderByDescending(entry => entry.Sequence).Take(allMatches ? int.MaxValue : query.PageSize + 1).ToArray();
        string[] issues;
        lock (_gate) issues = _issues.Where(issue => !query.RunId.HasValue || issue is not ("storage.overload" or "storage.write-failed"))
            .Concat(query.RunId is { } runId && _runs.TryGetValue(runId, out var run) && !run.IsComplete
            ? new[] { "run.incomplete" } : Array.Empty<string>()).ToArray();
        return new LogPage(allMatches ? page : page.Take(query.PageSize).ToArray(), !allMatches && page.Length > query.PageSize ? page[query.PageSize - 1].Sequence : null,
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
        && (!query.Category.HasValue || LogPresentation.Category(entry) == query.Category)
        && (string.IsNullOrWhiteSpace(query.Search) || new[] { entry.Message, entry.Details, entry.SourceName, entry.Code, entry.Area }
            .Concat(entry.Parameters.Values).Concat(entry.Paths.Select(path => path.Value))
            .Concat(new[] { entry.Trigger?.WatchedDirectory, entry.Trigger?.TargetName, entry.Trigger?.EventKind })
            .Any(value => value?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) == true));

    private List<LogEvent> ReadStored(CancellationToken ct)
    {
        var entries = new List<LogEvent>();
        try
        {
            var paths = Directory.GetFiles(DirectoryPath, "*.events.jsonl");
            foreach (var removed in _segments.Keys.Except(paths).ToArray()) _segments.Remove(removed);
            foreach (var path in paths)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var file = new FileInfo(path);
                    if (_segments.TryGetValue(path, out var cached) && cached.Length == file.Length && cached.LastWrite == file.LastWriteTimeUtc)
                    {
                        entries.AddRange(cached.Entries);
                        continue;
                    }
                    _segments.Remove(path);
                    var segment = new List<LogEvent>();
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    while (reader.ReadLine() is { } line)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            var entry = JsonSerializer.Deserialize<LogEvent>(line, JsonOptions);
                            if (entry is { SchemaVersion: LogEvent.CurrentSchema }) segment.Add(entry);
                        }
                        catch (JsonException) { AddIssue("events.corrupt"); }
                    }
                    entries.AddRange(segment);
                    if (file.Length <= CacheBytes)
                    {
                        while (_segments.Count > 0 && _segments.Values.Sum(item => item.Length) + file.Length > CacheBytes)
                            _segments.Remove(_segments.Keys.First());
                        _segments[path] = new(file.Length, file.LastWriteTimeUtc, segment.ToArray());
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
        while (await _queue.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            var batch = new List<WriteRequest>(256);
            while (batch.Count < 256 && _queue.Reader.TryRead(out var next))
            {
                batch.Add(next);
                if (next.Barrier is not null) break;
            }
            var entries = batch.Where(request => request.Entry is not null).Select(request => request.Entry!).ToArray();
            var runIds = batch.Select(request => request.RunId ?? request.Entry?.Context.RunId)
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
            var maintenance = batch.Any(request => request.Retention);
            if (maintenance) lock (_gate) _retentionQueued = false;
            var storageChanged = false;
            try
            {
                lock (_ioGate)
                {
                    if (entries.Length > 0)
                    {
                        var eventDate = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
                        if (_eventPath is null || _eventDate != eventDate || !File.Exists(_eventPath) || new FileInfo(_eventPath).Length > 10_000_000)
                        {
                            maintenance = true;
                            _eventPath = Path.Combine(DirectoryPath, $"{_time.GetUtcNow():yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.events.jsonl");
                            _eventDate = eventDate;
                        }
                        var text = new StringBuilder();
                        foreach (var entry in entries) text.Append(JsonSerializer.Serialize(entry, JsonOptions)).Append('\n');
                        File.AppendAllText(_eventPath, text.ToString(), Encoding.UTF8);
                        File.SetLastWriteTimeUtc(_eventPath, _time.GetUtcNow().UtcDateTime);
                        lock (_gate) foreach (var entry in entries) _pending.Remove(entry.Id);
                    }
                    if (batch.Any(request => request.Barrier is not null))
                        foreach (var currentRun in ReadRuns()) PersistRun(currentRun.Id);
                    else foreach (var runId in runIds) PersistRun(runId);
                    PersistHealth();
                    if (maintenance) storageChanged = ApplyRetention();
                }
                if (storageChanged || maintenance) StorageChanged?.Invoke(this, EventArgs.Empty);
                foreach (var request in batch) request.Barrier?.TrySetResult();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lock (_gate)
                {
                    foreach (var request in batch)
                        if (request.Entry is { } entry) { _pending.Remove(entry.Id); MarkLoss(entry.Context.RunId, "storage.write-failed"); }
                        else MarkLoss(request.RunId, "storage.write-failed");
                }
                foreach (var request in batch) request.Barrier?.TrySetException(new IOException("Structured logs could not be flushed.", ex));
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
    }

    private bool ApplyRetention()
    {
        var cutoff = _time.GetUtcNow().AddDays(-30);
        var changed = false;
        var failed = false;
        lock (_gate) _retentionDeferred = false;
        try
        {
            // Save counters and sequence before any destructive operation, including startup cleanup.
            PersistHealth();
            var files = Directory.GetFiles(DirectoryPath, "*.events.jsonl")
                .Select(path => new FileInfo(path)).OrderBy(file => file.LastWriteTimeUtc).ThenBy(file => file.Name).ToArray();
            var bytes = files.Sum(file => file.Length);
            foreach (var file in files)
            {
                if (file.LastWriteTimeUtc >= cutoff.UtcDateTime && bytes <= RetainedEventBytes) continue;
                try
                {
                    var affected = ReadSegmentRunIds(file.FullName);
                    // Unknown content may contain an active run: leave it for inspection.
                    if (affected is null) { failed = true; continue; }
                    lock (_gate)
                    {
                        if (affected.Any(id => _active.Contains(id))) { _retentionDeferred = true; continue; }
                        // Persist missing-evidence flags before deleting, so a crash cannot restore completeness.
                        foreach (var id in affected)
                            if (_runs.TryGetValue(id, out var run) && run.IsComplete)
                            {
                                _runs[id] = run with { IsComplete = false };
                                PersistRun(id);
                                changed = true;
                            }
                        var length = file.Length;
                        file.Delete();
                        bytes -= length;
                        _segments.Remove(file.FullName);
                        if (_eventPath == file.FullName) _eventPath = null;
                        changed = true;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed = true; }
            }
            var expiredRuns = ReadRuns().Where(run => run.EndedAt < cutoff
                || run.Outcome == LogOutcome.Interrupted && run.StartedAt < cutoff).ToArray();
            var retainedRuns = new HashSet<Guid>();
            var unknownRetainedRuns = false;
            if (expiredRuns.Length > 0)
                foreach (var path in Directory.GetFiles(DirectoryPath, "*.events.jsonl"))
                {
                    try
                    {
                        var ids = ReadSegmentRunIds(path);
                        if (ids is null) unknownRetainedRuns = true;
                        else retainedRuns.UnionWith(ids);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { unknownRetainedRuns = true; failed = true; }
                }
            foreach (var run in expiredRuns)
            {
                try
                {
                    lock (_gate)
                    {
                        if (unknownRetainedRuns || retainedRuns.Contains(run.Id) || _active.Contains(run.Id)
                            || _pending.Values.Any(entry => entry.Context.RunId == run.Id)) continue;
                        File.Delete(Path.Combine(DirectoryPath, $"{run.Id:N}.run.json"));
                        _runs.Remove(run.Id);
                        _problems.RemoveWhere(problem => problem.Run == run.Id);
                        changed = true;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed = true; }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed = true; }
        lock (_gate)
        {
            var hadFailure = _issues.Contains("storage.retention-failed");
            if (failed) _issues.Add("storage.retention-failed"); else _issues.Remove("storage.retention-failed");
            changed |= hadFailure != failed;
        }
        try { PersistHealth(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AddIssue("storage.retention-failed"); changed = true; }
        return changed;
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
    private HashSet<Guid>? ReadSegmentRunIds(string path)
    {
        var result = new HashSet<Guid>();
        foreach (var line in File.ReadLines(path))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<LogEvent>(line, JsonOptions);
                if (entry is not { SchemaVersion: LogEvent.CurrentSchema } || entry.Context is null) return null;
                if (entry.Context.RunId is { } id) result.Add(id);
            }
            catch (JsonException) { AddIssue("events.corrupt"); return null; }
        }
        return result;
    }
    private sealed record Health(long Sequence, string[] Issues, Dictionary<Guid, long>? Counters);

    public void Dispose()
    {
        lock (_gate) { if (_disposed) return; _disposed = true; _queue.Writer.TryComplete(); }
        _retentionTimer.Dispose();
        _writer.GetAwaiter().GetResult();
        try { foreach (var run in ReadRuns()) PersistRun(run.Id); PersistHealth(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AddIssue("storage.write-failed"); }
    }
    private sealed record WriteRequest(LogEvent? Entry, Guid? RunId, TaskCompletionSource? Barrier, bool Retention = false);
}
