using Common.ApplicationData;
using Common.JsonRepository;
using TaskAutomation.Logging;

namespace DesktopAutomation.Application.Logging;

public enum LogAttentionState { New, Seen, Resolved }
public sealed record LogAttentionProblem(string Key, Guid RunId, Guid? EventId, ExecutionLogLevel Level,
    long Sequence, string Revision, bool IsSummary, LogAttentionState State);
public sealed record LogAttentionDetail(LogAttentionProblem? Selected, LogAttentionProblem? Summary, LogAttentionProblem? Next = null);
public sealed record LogAcknowledgement(Guid RunId, LogAttentionState State, ExecutionLogLevel Level, long Sequence, string Revision);
public sealed record LogAttentionDocument
{
    public int SchemaVersion { get; init; } = 1;
    public Dictionary<string, LogAcknowledgement> Entries { get; init; } = new();
}

/// <summary>Owns persistent acknowledgement independently of immutable execution evidence.</summary>
public sealed class LogAttentionService : IDisposable
{
    private readonly ILogRepository _repository;
    private readonly LogQueryService _queries;
    private readonly string _directory;
    private readonly object _gate = new();
    private JsonRepository<LogAttentionDocument>? _store;
    private LogAttentionDocument? _document;
    private bool _disposed;
    public bool ReadFailed { get; private set; }
    public bool CleanupFailed { get; private set; }
    public event EventHandler? Changed;

    public LogAttentionService(ILogRepository repository, string? directory = null)
    {
        _repository = repository; _queries = new(repository); _directory = directory ?? AppPaths.LogAttentionDirectory;
        _repository.StorageChanged += OnStorageChanged;
    }
    private JsonRepository<LogAttentionDocument> Store => _store ??= new(new() { DirectoryPath = _directory }, _ => "attention");
    private LogAttentionDocument Document()
    {
        lock (_gate)
        {
            if (_document is not null) return _document;
            try
            {
                var loaded = Store.LoadAsync("attention").GetAwaiter().GetResult();
                ReadFailed = Store.LoadErrors.Count > 0 || loaded is not null && (loaded.SchemaVersion != 1 || loaded.Entries is null);
                _document = ReadFailed ? new() : loaded ?? new();
                if (!ReadFailed)
                {
                    var retained = _repository.ReadRuns().Select(run => run.Id).ToHashSet();
                    var next = RetainedEntries(_document, retained);
                    if (next.Count != _document.Entries.Count)
                    {
                        try
                        {
                            var pruned = new LogAttentionDocument { Entries = next };
                            Store.SaveAsync(pruned).GetAwaiter().GetResult();
                            _document = pruned;
                        }
                        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { CleanupFailed = true; }
                    }
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { ReadFailed = true; _document = new(); }
            return _document;
        }
    }
    private static Dictionary<string, LogAcknowledgement> RetainedEntries(LogAttentionDocument document, HashSet<Guid> retained)
        => document.Entries.Where(pair => retained.Contains(pair.Value.RunId)).ToDictionary(pair => pair.Key, pair => pair.Value);
    private static bool IsEvidence(LogEvent entry) => entry.Level >= ExecutionLogLevel.Warning
        && entry.Code is not (LogCodes.RunCompleted or LogCodes.RunState);
    private static string Key(Guid runId, LogEvent entry) => $"{runId:D}:{(entry.ProblemId.HasValue ? "problem" : "event")}:{entry.ProblemId ?? entry.Id:D}";
    private Guid? RunId(LogEvent entry) => entry.Context.RunId ?? _repository.ReadRuns().FirstOrDefault(run =>
        entry.Context.InstanceId.HasValue && run.InstanceId == entry.Context.InstanceId)?.Id;
    private static LogAttentionState State(LogAttentionProblem problem, IReadOnlyDictionary<string, LogAcknowledgement> entries)
    {
        if (!entries.TryGetValue(problem.Key, out var seen) || seen.State is not (LogAttentionState.Seen or LogAttentionState.Resolved)
            || problem.Level > seen.Level || problem.IsSummary && problem.Revision != seen.Revision) return LogAttentionState.New;
        return seen.State;
    }
    internal IReadOnlyList<LogAttentionProblem> Problems(IReadOnlyList<LogRun> runs, long snapshot, CancellationToken ct)
    {
        var entries = new Dictionary<string, LogAcknowledgement>(Document().Entries);
        if (runs.Count == 0) return [];
        var events = _queries.ReadAll(new(MinimumLevel: ExecutionLogLevel.Warning, SnapshotSequence: snapshot), ct, out _, out _)
            .Where(IsEvidence).ToArray();
        var runIds = runs.GroupBy(run => run.InstanceId).ToDictionary(group => group.Key, group => group.First().Id);
        var byRun = events.Select(entry => (Entry: entry, RunId: entry.Context.RunId ??
            (entry.Context.InstanceId is { } instance && runIds.TryGetValue(instance, out var id) ? id : (Guid?)null)))
            .Where(item => item.RunId.HasValue).GroupBy(item => item.RunId!.Value).ToDictionary(group => group.Key, group => group.Select(item => item.Entry).ToArray());
        var result = new List<LogAttentionProblem>();
        foreach (var run in runs)
        {
            ct.ThrowIfCancellationRequested();
            var observed = byRun.GetValueOrDefault(run.Id) ?? [];
            foreach (var group in observed.GroupBy(entry => Key(run.Id, entry)))
            {
                var primary = group.OrderByDescending(entry => entry.Level).ThenBy(entry => entry.Sequence).First();
                var problem = new LogAttentionProblem(group.Key, run.Id, primary.Id, primary.Level, group.Max(entry => entry.Sequence), "", false, LogAttentionState.New);
                result.Add(problem with { State = State(problem, entries) });
            }
            var errors = observed.Where(entry => entry.Level == ExecutionLogLevel.Error).Select(entry => entry.ProblemId ?? entry.Id).Distinct().Count();
            var warnings = observed.Where(entry => entry.Level == ExecutionLogLevel.Warning).Select(entry => entry.ProblemId ?? entry.Id).Distinct().Count();
            if (LogQueryService.IsProblem(run) && (observed.Length == 0 || !run.IsComplete || errors < run.ErrorCount || warnings < run.WarningCount))
            {
                var revision = $"{run.Outcome}:{run.ErrorCount}:{run.WarningCount}:{run.LostEntries}:{run.IsComplete}";
                var summary = new LogAttentionProblem($"{run.Id:D}:summary", run.Id, null,
                    run.ErrorCount > 0 || run.Outcome is LogOutcome.Failed or LogOutcome.Interrupted or LogOutcome.WithErrors ? ExecutionLogLevel.Error : ExecutionLogLevel.Warning,
                    snapshot, revision, true, LogAttentionState.New);
                result.Add(summary with { State = State(summary, entries) });
            }
        }
        return result;
    }
    private static LogAttentionProblem? NextProblem(IEnumerable<LogAttentionProblem> problems) => problems
        .Where(problem => !problem.IsSummary && problem.State == LogAttentionState.New)
        .OrderByDescending(problem => problem.Level).ThenBy(problem => problem.Sequence).FirstOrDefault();
    internal HashSet<Guid> NewRunIds(IReadOnlyList<LogRun> runs, long snapshot, CancellationToken ct)
        => Problems(runs, snapshot, ct).Where(problem => problem.State == LogAttentionState.New).Select(problem => problem.RunId).ToHashSet();
    public Task<LogAttentionDetail> DetailAsync(LogEvent? observed, Guid? runId, CancellationToken ct = default) => Task.Run(() =>
    {
        runId ??= observed is not null ? RunId(observed) : null;
        var runs = _repository.ReadRuns().Where(run => run.Id == runId).ToArray();
        var problems = Problems(runs, long.MaxValue, ct);
        var selected = observed is not null && runId.HasValue ? problems.FirstOrDefault(problem => problem.Key == Key(runId.Value, observed)) : null;
        if (selected is not null && observed is not null)
        {
            selected = selected with { EventId = observed.Id, Level = observed.Level, Sequence = observed.Sequence };
            selected = selected with { State = State(selected, Document().Entries) };
        }
        return new LogAttentionDetail(selected, problems.FirstOrDefault(problem => problem.IsSummary),
            NextProblem(problems));
    }, ct);
    public Task<LogAttentionProblem?> NextNewAsync(Guid runId, CancellationToken ct = default) => Task.Run(() =>
        NextProblem(Problems(_repository.ReadRuns().Where(run => run.Id == runId).ToArray(), long.MaxValue, ct)), ct);
    public Task<IReadOnlyList<LogAttentionProblem>> ScopeAsync(RunQuery query, CancellationToken ct = default) => Task.Run(() =>
    {
        var snapshot = Math.Min(query.SnapshotSequence, _repository.SnapshotSequence);
        return Problems(_queries.SelectRuns(query with { OnlyNewProblems = false }, snapshot, ct), snapshot, ct);
    }, ct);
    public Task<bool> MarkSeenAsync(LogEvent observed, CancellationToken ct = default)
    {
        if (!IsEvidence(observed) || RunId(observed) is not { } runId) return Task.FromResult(true);
        return ChangeAsync([new(Key(runId, observed), runId, observed.Id, observed.Level, observed.Sequence, "", false, LogAttentionState.New)], LogAttentionState.Seen, ct);
    }
    public Task<bool> ChangeAsync(IEnumerable<LogAttentionProblem> viewed, LogAttentionState state, CancellationToken ct = default)
    {
        var captured = viewed.ToArray();
        return Task.Run(() =>
        {
            var changed = false;
            lock (_gate)
            {
                if (_disposed) return false;
                ct.ThrowIfCancellationRequested();
                var current = Document();
                var retained = _repository.ReadRuns().Select(run => run.Id).ToHashSet();
                var next = RetainedEntries(current, retained);
                foreach (var problem in captured.Where(problem => retained.Contains(problem.RunId)))
                {
                    if (state == LogAttentionState.New) { changed |= next.Remove(problem.Key); continue; }
                    var prior = next.GetValueOrDefault(problem.Key);
                    // Displaying an old observation cannot downgrade an explicit resolution or acknowledge a newer severity.
                    if (state == LogAttentionState.Seen && prior is not null && prior.Level >= problem.Level
                        && (!problem.IsSummary || prior.Revision == problem.Revision)) continue;
                    var acknowledgement = new LogAcknowledgement(problem.RunId, state,
                        prior is not null && prior.Level > problem.Level ? prior.Level : problem.Level,
                        Math.Max(prior?.Sequence ?? 0, problem.Sequence), problem.Revision);
                    if (prior == acknowledgement) continue;
                    next[problem.Key] = acknowledgement; changed = true;
                }
                changed |= next.Count != current.Entries.Count;
                if (!changed) return true;
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var document = new LogAttentionDocument { Entries = next };
                    Store.SaveAsync(document).GetAwaiter().GetResult();
                    _document = document; ReadFailed = false;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
            }
            if (changed) Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }, ct);
    }
    private void OnStorageChanged(object? sender, EventArgs args)
    {
        // The repository raises this on its background writer, outside its locks.
        // Reuse the same atomic update path as explicit acknowledgements.
        var success = ChangeAsync([], LogAttentionState.New).GetAwaiter().GetResult();
        bool statusChanged;
        lock (_gate)
        {
            if (_disposed) return;
            statusChanged = CleanupFailed != !success;
            CleanupFailed = !success;
        }
        if (statusChanged) Changed?.Invoke(this, EventArgs.Empty);
    }
    public void Dispose()
    {
        _repository.StorageChanged -= OnStorageChanged;
        lock (_gate) { _disposed = true; _store?.Dispose(); }
    }
}
