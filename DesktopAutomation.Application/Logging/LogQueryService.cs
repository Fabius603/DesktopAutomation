using TaskAutomation.Logging;
using TaskAutomation.Jobs;

namespace DesktopAutomation.Application.Logging;

public sealed record RunQuery(Guid? SourceId = null, LogOutcome? Outcome = null, DateTimeOffset? From = null,
    DateTimeOffset? Until = null, string? Search = null, int Offset = 0, int PageSize = 50,
    long SnapshotSequence = long.MaxValue, LogSource? Source = null, bool OnlyProblems = false, bool OnlyNewProblems = false);
public sealed record RunPage(IReadOnlyList<LogRun> Runs, int Total, int? NextOffset, long SnapshotSequence);
public sealed record LogOverview(IReadOnlyList<LogRun> RecentRuns, IReadOnlyList<LogRun> ActiveRuns,
    IReadOnlyList<LogRun> ProblemRuns, LogPage RecentAutomationEvents,
    int TotalRuns = 0, int ProblemRunCount = 0, long SnapshotSequence = long.MaxValue, int NewProblemRunCount = 0, IReadOnlyList<LogRun>? NewProblemRuns = null);
public sealed record RunDetails(LogRun Run, IReadOnlyList<LogStepExecution> Steps, LogReadState State,
    IReadOnlyList<string> Issues, IReadOnlyList<LogStepDisplay>? DisplaySteps = null,
    LogEvent? PrimaryProblem = null, IReadOnlyDictionary<StepLogOutcome, long>? StepCounts = null);
public sealed record LogNavigation(Guid? RunId, Guid? JobId, string? StepId, bool CanOpenStep, string? UnavailableReason,
    bool DefinitionChanged = false);
public sealed record LogEventGroup(string Code, string? DiagnosticCode, Guid? SourceId, string? StepId,
    int Count, DateTimeOffset FirstAt, DateTimeOffset LastAt, IReadOnlyList<Guid> EventIds);

/// <summary>Application use cases for the four log screens. All results originate in v2 data.</summary>
public sealed partial class LogQueryService(ILogRepository repository, LogAttentionService? attention = null)
{
    public Task<LogPage> SearchAsync(LogQuery query, CancellationToken ct = default)
        => Task.Run(() => repository.Query(query, ct), ct);
    public Task<RunPage> RunsAsync(RunQuery query, CancellationToken ct = default) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        if (query.Offset < 0 || query.PageSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(query));
        var snapshot = Math.Min(query.SnapshotSequence, repository.SnapshotSequence);
        var runs = SelectRuns(query, snapshot, ct);
        return new RunPage(runs.Skip(query.Offset).Take(query.PageSize).ToArray(), runs.Length,
            query.Offset + query.PageSize < runs.Length ? query.Offset + query.PageSize : null, snapshot);
    }, ct);
    public Task<LogOverview> OverviewAsync(CancellationToken ct = default) => OverviewAsync(new RunQuery(Source: LogSource.Job), ct);
    public Task<LogOverview> OverviewAsync(RunQuery query, CancellationToken ct = default) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        var snapshot = Math.Min(query.SnapshotSequence, repository.SnapshotSequence);
        var runs = SelectRuns(query, snapshot, ct);
        var problems = runs.Where(IsProblem).ToArray();
        var newIds = attention?.NewRunIds(runs, snapshot, ct) ?? problems.Select(run => run.Id).ToHashSet();
        return new LogOverview(runs.Take(20).ToArray(), runs.Where(run => run.Outcome is LogOutcome.Running or LogOutcome.Paused).ToArray(),
            problems.Take(20).ToArray(),
            repository.Query(new(Source: LogSource.Automation, PageSize: 20, From: query.From, Until: query.Until,
                Search: query.Search, SnapshotSequence: snapshot), ct), runs.Length, problems.Length, snapshot, newIds.Count, runs.Where(run => newIds.Contains(run.Id)).Take(20).ToArray());
    }, ct);

    public Task<RunDetails?> RunAsync(Guid runId, CancellationToken ct = default) => Task.Run(() =>
    {
        var run = repository.ReadRuns().FirstOrDefault(run => run.Id == runId);
        if (run is null) return null;
        var events = ReadAll(new(RunId: runId), ct, out var state, out var issues);
        if (!run.IsComplete && state is LogReadState.Available or LogReadState.Empty) state = LogReadState.Partial;
        var steps = LogTimeline.Build(run, events);
        var problem = events.Where(entry => entry.Level >= ExecutionLogLevel.Warning
            && entry.Code is not (LogCodes.RunCompleted or LogCodes.RunState))
            .OrderByDescending(entry => entry.Level).ThenByDescending(entry => entry.DiagnosticCode is not null)
            .ThenBy(entry => entry.Sequence).FirstOrDefault();
        return new RunDetails(run, steps, state, issues, steps.Select(step => LogPresentation.Step(step, run.Steps)).ToArray(),
            problem, steps.GroupBy(step => step.Outcome).ToDictionary(group => group.Key, group => group.Sum(step => step.Summary?.Count ?? 1)));
    }, ct);

    public Task<IReadOnlyList<LogEventGroup>> GroupsAsync(LogQuery query, CancellationToken ct = default)
        => Task.Run<IReadOnlyList<LogEventGroup>>(() => ReadAll(query, ct, out _, out _)
            .GroupBy(entry => (entry.Code, entry.DiagnosticCode, entry.SourceId, entry.Context.StepId, entry.Message))
            .Select(group => new LogEventGroup(group.Key.Code, group.Key.DiagnosticCode, group.Key.SourceId,
                group.Key.StepId, group.Count(), group.Min(entry => entry.Timestamp), group.Max(entry => entry.Timestamp),
                group.Select(entry => entry.Id).ToArray())).ToArray(), ct);

    public IReadOnlyList<LogEvent> ReadAll(LogQuery query, CancellationToken ct, out LogReadState state, out IReadOnlyList<string> issues)
    {
        var page = repository.QueryAll(query, ct);
        state = page.State;
        issues = page.Issues;
        return page.Entries.OrderBy(entry => entry.Sequence).ToArray();
    }

    public LogNavigation Navigation(LogEvent entry, IEnumerable<Job> currentJobs)
    {
        var run = repository.ReadRuns().FirstOrDefault(run => run.Id == entry.Context.RunId
            || entry.Context.InstanceId.HasValue && run.InstanceId == entry.Context.InstanceId);
        if (run is null) return new(null, null, entry.Context.StepId, false, "RunUnavailable");
        return Navigation(run.Id, entry.Context.StepId, currentJobs);
    }

    public LogNavigation Navigation(Guid runId, string? stepId, IEnumerable<Job> currentJobs)
    {
        var run = repository.ReadRuns().FirstOrDefault(item => item.Id == runId);
        if (run is null) return new(null, null, stepId, false, "RunUnavailable");
        var job = currentJobs.FirstOrDefault(job => job.Id == run.SourceId);
        var stepExists = stepId is not null && run.Steps.Any(step => step.Id == stepId)
            && job?.EnumerateAllSteps().Any(step => step.Id == stepId) == true;
        return new(run.Id, run.SourceId, stepId, stepExists,
            job is null ? "JobDeleted" : stepId is not null && !stepExists ? "StepDeleted" : null,
            job is not null && !run.Steps.SequenceEqual(LogRunSnapshots.Steps(job)));
    }
}
