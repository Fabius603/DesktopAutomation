namespace TaskAutomation.Logging;

public enum ExecutionLogKind { Job, Makro }
public enum ExecutionLogLevel { Debug, Information, Warning, Error }
public enum LogSource { Job, Automation, Application, Makro }
public enum LogOutcome { Running, Paused, Successful, WithWarnings, WithErrors, Failed, Stopped, Interrupted, Unknown }
public enum StepLogOutcome { Running, Successful, Warning, Failed, Skipped, Cancelled, NotExecuted, Pending, Unknown }
public enum LogReadState { Available, Empty, Partial, Unavailable }

public static class LogCodes
{
    public const string Message = "diagnostic.message";
    public const string RunStarted = "run.started";
    public const string RunCompleted = "run.completed";
    public const string RunState = "run.state";
    public const string StartRejected = "run.start-rejected";
    public const string StepStarted = "step.started";
    public const string StepCompleted = "step.completed";
    public const string StepFailed = "step.failed";
    public const string StepSkipped = "step.skipped";
    public const string StepCancelled = "step.cancelled";
    public const string StepOutput = "step.output";
    public const string StepBackgroundStarted = "step.background-started";
    public const string StepBackgroundCompleted = "step.background-completed";
    public const string StepBackgroundFailed = "step.background-failed";
    public const string Trigger = "automation.trigger";
    public const string AutomationDecision = "automation.decision";
    public const string StorageGap = "storage.gap";
    public const string FileUnavailable = "LOG-FILE-004";
    public const string AccessDenied = "LOG-ACCESS-001";
    public const string Unexpected = "LOG-UNEXPECTED-001";
}

public sealed record LogContext(Guid? RunId = null, Guid? InstanceId = null,
    Guid? TriggerId = null, Guid? AutomationId = null, string? StepId = null, Guid? StepExecutionId = null,
    Guid? ParentRunId = null);

public sealed record LogEvent
{
    public const int CurrentSchema = 2;
    public int SchemaVersion { get; init; } = CurrentSchema;
    public Guid Id { get; init; } = Guid.NewGuid();
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public LogSource Source { get; init; }
    public Guid? SourceId { get; init; }
    public string SourceName { get; init; } = "";
    public string Area { get; init; } = "";
    public ExecutionLogLevel Level { get; init; } = ExecutionLogLevel.Information;
    public string Code { get; init; } = LogCodes.Message;
    public string Message { get; init; } = "";
    public string? Details { get; init; }
    public LogContext Context { get; init; } = new();
    public Guid? ProblemId { get; init; }
    public string? DiagnosticCode { get; init; }
    public string? Phase { get; init; }
    public int? Iteration { get; init; }
    public long? DurationMs { get; init; }
    public Dictionary<string, string?> Parameters { get; init; } = new();
    public LogBranchDecision? BranchDecision { get; init; }
}

public sealed record LogConditionOutcome(int Position, string State, string Operator);
public sealed record LogBranchDecision(bool ParentActive, bool BranchActive, string Reason,
    string? MatchMode, IReadOnlyList<LogConditionOutcome> Conditions);

public sealed record LogStepSnapshot(string Id, string TypeId, int Position, string Phase, bool Enabled);
public sealed record LogRun
{
    public long CatalogSequence { get; init; }
    public int SchemaVersion { get; init; } = LogEvent.CurrentSchema;
    public Guid Id { get; init; }
    public Guid InstanceId { get; init; }
    public Guid SourceId { get; init; }
    public LogSource Source { get; init; }
    public string Name { get; init; } = "";
    public long ExecutionNumber { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public long? DurationMs { get; init; }
    public LogOutcome Outcome { get; init; } = LogOutcome.Running;
    public string? CompletionReason { get; init; }
    public string Origin { get; init; } = "Unknown";
    public string? OriginName { get; init; }
    public Guid? OriginId { get; init; }
    public LogContext Context { get; init; } = new();
    public IReadOnlyList<LogStepSnapshot> Steps { get; init; } = [];
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public Guid? PrimaryProblemId { get; init; }
    public long LostEntries { get; init; }
    public bool IsComplete { get; init; } = true;
}

public sealed record LogQuery(LogSource? Source = null, Guid? SourceId = null, Guid? RunId = null,
    DateTimeOffset? From = null, DateTimeOffset? Until = null,
    ExecutionLogLevel MinimumLevel = ExecutionLogLevel.Debug, string? Search = null,
    string? Area = null, bool OnlyProblems = false, int PageSize = 100,
    long BeforeSequence = long.MaxValue, long SnapshotSequence = long.MaxValue,
    long AfterSequence = 0, Guid? TriggerId = null);
public sealed record LogPage(IReadOnlyList<LogEvent> Entries, long? NextBeforeSequence,
    long SnapshotSequence, int MatchCount, int AvailableCount, LogReadState State,
    IReadOnlyList<string> Issues);
public sealed record LogStepExecution(LogStepSnapshot Step, Guid? ExecutionId, string Phase, int? Iteration,
    StepLogOutcome Outcome, string? Reason, long? DurationMs, IReadOnlyList<LogEvent> Events);
public sealed record LogAction(string Kind, Guid? RunId = null, string? StepId = null, string? Path = null);
public sealed record LogDiagnostic(string Code, string CauseKey, string HelpKey, IReadOnlyList<LogAction> Actions);
public sealed record AutomationTriggerContext(Guid AutomationId, Guid TriggerId, DateTimeOffset ObservedAt,
    string? Kind = null, Dictionary<string, string?>? Parameters = null)
{
    public static implicit operator AutomationTriggerContext(Guid id) => new(id, Guid.NewGuid(), DateTimeOffset.UtcNow);
    public static implicit operator Guid(AutomationTriggerContext context) => context.AutomationId;
}
