using TaskAutomation.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Automations;

namespace DesktopAutomation.Application.Logging;

public sealed record AutomationHistoryQuery(Guid? AutomationId = null, DateTimeOffset? From = null,
    DateTimeOffset? Until = null, string? Search = null, int Offset = 0, int PageSize = 50,
    long SnapshotSequence = long.MaxValue);
public sealed record LogRunLink(Guid InstanceId, Guid? RunId, Guid? SourceId, LogSource? Source,
    string? Name, long? ExecutionNumber, LogOutcome? Outcome, string? UnavailableReason);
public sealed record AutomationHistoryItem(Guid TriggerId, Guid AutomationId, string Name, DateTimeOffset ObservedAt,
    LogTriggerSnapshot? Trigger, string Reason, DateTimeOffset? EligibleAt, IReadOnlyList<LogRunLink> StartedRuns,
    IReadOnlyList<LogRunLink> RelatedRuns, IReadOnlyList<LogEvent> Events, bool IsComplete, string TitleKey);
public sealed record AutomationHistoryPage(IReadOnlyList<AutomationHistoryItem> Items, int Total, int? NextOffset,
    long SnapshotSequence, LogReadState State, IReadOnlyList<string> Issues);
public sealed record AutomationLogChoice(Guid Id, string Name, bool DefinitionExists, string? WatchedDirectory,
    Guid? TargetId, string? TargetName);
public sealed record LogEventDetails(LogEvent Event, LogEventDisplay Display, LogRunLink? Run,
    int? StepPosition, LogReadState State, IReadOnlyList<string> Issues);

public sealed partial class LogQueryService
{
    public static LogText AutomationImpact(AutomationHistoryItem item) => new("Logs.Ui.AutomationImpact." +
        (item.Reason == "AlreadyRunning" ? "Existing" : item.StartedRuns.Any(run => run.RunId.HasValue) ? "Started" : "NoConfirmedRun"), new Dictionary<string, string?>());
    public static LogText AutomationSummary(AutomationHistoryItem item) => new(item.TitleKey + ".Detail",
        new Dictionary<string, string?> { ["Target"] = item.Trigger?.TargetName });
    public static bool IsProblem(LogRun run) => run.ErrorCount > 0 || run.WarningCount > 0 || !run.IsComplete
        || run.Outcome is LogOutcome.Failed or LogOutcome.WithErrors or LogOutcome.WithWarnings or LogOutcome.Interrupted or LogOutcome.Unknown;
    internal LogRun[] SelectRuns(RunQuery query, long snapshot, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var runs = repository.ReadRuns().Where(run => run.CatalogSequence <= snapshot
            && (!query.Source.HasValue || run.Source == query.Source)
            && (!query.SourceId.HasValue || run.SourceId == query.SourceId)
            && (!query.Outcome.HasValue || run.Outcome == query.Outcome)
            && (!query.OnlyProblems || IsProblem(run))
            && (!query.From.HasValue || run.StartedAt >= query.From)
            && (!query.Until.HasValue || run.StartedAt < query.Until)
            && (string.IsNullOrWhiteSpace(query.Search) || run.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || run.OriginName?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) == true)).ToArray();
        if (!query.OnlyNewProblems) return runs;
        var unseen = attention?.NewRunIds(runs, snapshot, ct) ?? runs.Where(IsProblem).Select(run => run.Id).ToHashSet();
        return runs.Where(run => unseen.Contains(run.Id)).ToArray();
    }
    private static LogRunLink Link(Guid instance, IReadOnlyList<LogRun> runs)
    {
        var run = runs.FirstOrDefault(item => item.InstanceId == instance);
        return new(instance, run?.Id, run?.SourceId, run?.Source, run?.Name, run?.ExecutionNumber, run?.Outcome,
            run is null ? "RunUnavailable" : null);
    }
    public Task<AutomationHistoryPage> AutomationHistoryAsync(AutomationHistoryQuery query, CancellationToken ct = default) => Task.Run(() =>
    {
        if (query.Offset < 0 || query.PageSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(query));
        var checkpoint = Math.Min(query.SnapshotSequence, repository.SnapshotSequence);
        // Assemble the complete retained trigger group before applying text/time filters.
        var entries = ReadAll(new(Source: LogSource.Automation, SourceId: query.AutomationId, SnapshotSequence: checkpoint), ct, out var state, out var issues);
        var runs = repository.ReadRuns();
        var rows = entries.Where(entry => entry.Context.TriggerId.HasValue).GroupBy(entry => entry.Context.TriggerId!.Value).Select(group =>
        {
            ct.ThrowIfCancellationRequested();
            var observations = group.OrderBy(entry => entry.Sequence).ToArray();
            var trigger = observations.FirstOrDefault(entry => entry.Code == LogCodes.Trigger);
            var decision = observations.LastOrDefault(entry => entry.Code == LogCodes.AutomationDecision);
            var reason = decision?.Parameters.GetValueOrDefault("Reason") ?? "Unknown";
            var starts = decision?.Context.InstanceId is { } instance ? new[] { Link(instance, runs) } : [];
            var relatedIds = decision?.RelatedInstanceIds.ToArray() ?? [];
            if (relatedIds.Length == 0 && decision?.Parameters.GetValueOrDefault("RelatedInstances") is { } legacy)
                relatedIds = legacy.Split(',').Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToArray();
            var title = starts.Any(link => link.RunId.HasValue) && reason is "StartRequested" or "RestartRequested" or "ParallelStartRequested"
                ? "Log.Automation.Started" : AutomationTitle(reason);
            return new AutomationHistoryItem(group.Key, observations[0].Context.AutomationId ?? observations[0].SourceId ?? Guid.Empty,
                trigger?.SourceName ?? observations[0].SourceName, trigger?.Timestamp ?? observations[0].Timestamp,
                trigger?.Trigger, reason, DateTimeOffset.TryParse(decision?.Parameters.GetValueOrDefault("EligibleAt"),
                    System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var eligible) ? eligible : null,
                starts, relatedIds.Distinct().Select(id => Link(id, runs)).ToArray(), observations,
                trigger is not null && decision is not null && state is not (LogReadState.Partial or LogReadState.Unavailable), title);
        }).Where(item => (!query.From.HasValue || item.ObservedAt >= query.From)
            && (!query.Until.HasValue || item.ObservedAt < query.Until)
            && (string.IsNullOrWhiteSpace(query.Search) || item.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || item.Reason.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || item.Events.Any(entry => new[] { entry.Message, entry.Details }.Concat(entry.Parameters.Values)
                    .Any(value => value?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) == true))))
            .OrderByDescending(item => item.ObservedAt).ThenBy(item => item.TriggerId).ToArray();
        return new AutomationHistoryPage(rows.Skip(query.Offset).Take(query.PageSize).ToArray(), rows.Length,
            query.Offset + query.PageSize < rows.Length ? query.Offset + query.PageSize : null, checkpoint,
            rows.Any(item => !item.IsComplete) && state is not LogReadState.Unavailable ? LogReadState.Partial : rows.Length == 0 && state == LogReadState.Available ? LogReadState.Empty : state, issues);
    }, ct);
    public static string AutomationTitle(string reason) => "Log.Automation." + (reason switch
    {
        "StartRequested" or "RestartRequested" or "ParallelStartRequested" => "Requested",
        "AlreadyRunning" => "AlreadyRunning",
        "Cooldown" => "Cooldown",
        "OutsideWindow" => "OutsideWindow",
        "Paused" => "Paused",
        "Disabled" => "Disabled",
        "StopRequested" => "StopRequested",
        "StartRejected" => "Rejected",
        "Failed" => "Failed",
        "Cancelled" => "Cancelled",
        _ => "Unknown"
    });
    public Task<IReadOnlyList<AutomationLogChoice>> AutomationChoicesAsync(IEnumerable<AutomationDefinition> definitions, CancellationToken ct = default)
    {
        var current = definitions.ToArray();
        return Task.Run<IReadOnlyList<AutomationLogChoice>>(() =>
        {
            var entries = ReadAll(new(Source: LogSource.Automation), ct, out _, out _);
            var history = entries.Where(entry => entry.SourceId.HasValue).GroupBy(entry => entry.SourceId!.Value).ToDictionary(group => group.Key, group => group.Last());
            return current.Select(item => item.Id).Concat(history.Keys).Distinct().Select(id =>
            {
                var definition = current.FirstOrDefault(item => item.Id == id);
                history.TryGetValue(id, out var last);
                var trigger = entries.LastOrDefault(entry => entry.SourceId == id && entry.Trigger is not null)?.Trigger;
                return new AutomationLogChoice(id, definition?.Name ?? last?.SourceName ?? id.ToString(), definition is not null,
                    definition is null ? trigger?.WatchedDirectory : (definition.Trigger as FileSystemAutomationTrigger)?.DirectoryPath,
                    definition?.Action.JobId ?? definition?.Action.MakroId ?? trigger?.TargetId, definition?.Action.Name ?? trigger?.TargetName);
            }).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }, ct);
    }
    public Task<LogEventDetails?> EventDetailsAsync(Guid id, CancellationToken ct = default) => Task.Run(() =>
    {
        var entries = ReadAll(new(), ct, out var state, out var issues);
        var entry = entries.FirstOrDefault(item => item.Id == id);
        entry ??= repository.ReadRuns().SelectMany(run => run.StepSummaries).Select(summary => summary.LastEvent).FirstOrDefault(item => item.Id == id);
        if (entry is null) return null;
        if (entry.Context.StepExecutionId is { } execution)
            entry = entry with
            {
                Paths = entry.Paths.Concat(entries.Where(item => item.Context.RunId == entry.Context.RunId && item.Context.StepExecutionId == execution)
                .SelectMany(item => item.Paths)).Distinct().ToArray()
            };
        var runs = repository.ReadRuns();
        var run = runs.FirstOrDefault(item => item.Id == entry.Context.RunId || item.InstanceId == entry.Context.InstanceId);
        return new LogEventDetails(entry, LogPresentation.Event(entry), run is null ? null : Link(run.InstanceId, runs),
            run?.Steps.FirstOrDefault(step => step.Id == entry.Context.StepId)?.Position, state, issues);
    }, ct);
}
