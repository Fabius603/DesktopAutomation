using TaskAutomation.Logging;
using TaskAutomation.Jobs;

namespace DesktopAutomation.Application.Logging;

public sealed record RunQuery(Guid? SourceId = null, LogOutcome? Outcome = null, DateTimeOffset? From = null,
    DateTimeOffset? Until = null, string? Search = null, int Offset = 0, int PageSize = 50,
    long SnapshotSequence = long.MaxValue);
public sealed record RunPage(IReadOnlyList<LogRun> Runs, int Total, int? NextOffset, long SnapshotSequence);
public sealed record LogOverview(IReadOnlyList<LogRun> RecentRuns, IReadOnlyList<LogRun> ActiveRuns,
    IReadOnlyList<LogRun> ProblemRuns, LogPage RecentAutomationEvents);
public sealed record RunDetails(LogRun Run, IReadOnlyList<LogStepExecution> Steps, LogReadState State,
    IReadOnlyList<string> Issues);
public sealed record LogNavigation(Guid? RunId, Guid? JobId, string? StepId, bool CanOpenStep, string? UnavailableReason,
    bool DefinitionChanged = false);
public sealed record LogEventGroup(string Code, string? DiagnosticCode, Guid? SourceId, string? StepId,
    int Count, DateTimeOffset FirstAt, DateTimeOffset LastAt, IReadOnlyList<Guid> EventIds);

/// <summary>Application use cases for the four log screens. All results originate in v2 data.</summary>
public sealed class LogQueryService(ILogRepository repository)
{
    public Task<LogPage> SearchAsync(LogQuery query, CancellationToken ct = default)
        => Task.Run(() => repository.Query(query, ct), ct);
    public Task<RunPage> RunsAsync(RunQuery query, CancellationToken ct = default) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        if (query.Offset < 0 || query.PageSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(query));
        var snapshot = repository.Query(new(PageSize: 1, SnapshotSequence: query.SnapshotSequence), ct).SnapshotSequence;
        var runs = repository.ReadRuns().Where(run => run.CatalogSequence <= snapshot
            && (!query.SourceId.HasValue || run.SourceId == query.SourceId)
            && (!query.Outcome.HasValue || run.Outcome == query.Outcome)
            && (!query.From.HasValue || run.StartedAt >= query.From)
            && (!query.Until.HasValue || run.StartedAt < query.Until)
            && (string.IsNullOrWhiteSpace(query.Search) || run.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || run.OriginName?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) == true)).ToArray();
        return new RunPage(runs.Skip(query.Offset).Take(query.PageSize).ToArray(), runs.Length,
            query.Offset + query.PageSize < runs.Length ? query.Offset + query.PageSize : null, snapshot);
    }, ct);
    public Task<LogOverview> OverviewAsync(CancellationToken ct = default) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        var runs = repository.ReadRuns();
        return new LogOverview(runs.Take(20).ToArray(), runs.Where(run => run.Outcome is LogOutcome.Running or LogOutcome.Paused).ToArray(),
            runs.Where(run => run.ErrorCount > 0 || run.WarningCount > 0 || !run.IsComplete || run.Outcome == LogOutcome.Failed).Take(20).ToArray(),
            repository.Query(new(Source: LogSource.Automation, PageSize: 20), ct));
    }, ct);

    public Task<RunDetails?> RunAsync(Guid runId, CancellationToken ct = default) => Task.Run(() =>
    {
        var run = repository.ReadRuns().FirstOrDefault(run => run.Id == runId);
        if (run is null) return null;
        var events = ReadAll(new(RunId: runId), ct, out var state, out var issues);
        if (!run.IsComplete && state is LogReadState.Available or LogReadState.Empty) state = LogReadState.Partial;
        return new RunDetails(run, LogTimeline.Build(run, events), state, issues);
    }, ct);

    public Task<IReadOnlyList<LogEventGroup>> GroupsAsync(LogQuery query, CancellationToken ct = default)
        => Task.Run<IReadOnlyList<LogEventGroup>>(() => ReadAll(query, ct, out _, out _)
            .GroupBy(entry => (entry.Code, entry.DiagnosticCode, entry.SourceId, entry.Context.StepId, entry.Message))
            .Select(group => new LogEventGroup(group.Key.Code, group.Key.DiagnosticCode, group.Key.SourceId,
                group.Key.StepId, group.Count(), group.Min(entry => entry.Timestamp), group.Max(entry => entry.Timestamp),
                group.Select(entry => entry.Id).ToArray())).ToArray(), ct);

    public IReadOnlyList<LogEvent> ReadAll(LogQuery query, CancellationToken ct, out LogReadState state, out IReadOnlyList<string> issues)
    {
        var events = new List<LogEvent>();
        var collectedIssues = new HashSet<string>();
        var pageQuery = query with { PageSize = 1000 };
        state = LogReadState.Empty;
        do
        {
            var page = repository.Query(pageQuery, ct);
            foreach (var issue in page.Issues) collectedIssues.Add(issue);
            events.AddRange(page.Entries);
            state = page.State;
            if (page.NextBeforeSequence is null) break;
            pageQuery = pageQuery with { BeforeSequence = page.NextBeforeSequence.Value, SnapshotSequence = page.SnapshotSequence };
        } while (true);
        issues = collectedIssues.ToArray();
        return events.OrderBy(entry => entry.Sequence).ToArray();
    }

    public LogNavigation Navigation(LogEvent entry, IEnumerable<Job> currentJobs)
    {
        var run = repository.ReadRuns().FirstOrDefault(run => run.Id == entry.Context.RunId
            || entry.Context.InstanceId.HasValue && run.InstanceId == entry.Context.InstanceId);
        if (run is null) return new(null, null, entry.Context.StepId, false, "RunUnavailable");
        var job = currentJobs.FirstOrDefault(job => job.Id == run.SourceId);
        var stepExists = entry.Context.StepId is { } stepId && job?.EnumerateAllSteps().Any(step => step.Id == stepId) == true;
        return new(run.Id, run.SourceId, entry.Context.StepId, stepExists,
            job is null ? "JobDeleted" : entry.Context.StepId is not null && !stepExists ? "StepDeleted" : null,
            job is not null && !run.Steps.SequenceEqual(LogRunSnapshots.Steps(job)));
    }
}
