using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
namespace TaskAutomation.Logging
{
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
        public LogOutcome Outcome { get; internal set; } = LogOutcome.Running;
        public bool IsRunning => EndedAt is null && Outcome is LogOutcome.Running or LogOutcome.Paused;
        public JobExecutionState? JobState { get; internal set; }
    }

    public sealed class ExecutionLogEntry
    {
        public Guid Id { get; init; }
        public long Sequence { get; init; }
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
        bool HasMoreSessions { get; }

        ExecutionLogSession BeginJob(Guid jobId, string jobName, JobStartContext? startContext = null);
        void Write(ExecutionLogSession session, ExecutionLogLevel level, string message, string? details = null, string? stepId = null, string? stepType = null, long? durationMs = null, JobExecutionState? jobState = null);
        void Complete(ExecutionLogSession session, bool success, string? details = null, bool cancelled = false);
        IReadOnlyList<ExecutionLogEntry> ReadEntries(Guid sessionId, int maxEntries = 2000);
        Task<IReadOnlyList<ExecutionLogEntry>> ReadEntriesAsync(Guid sessionId, int maxEntries = 2000, CancellationToken cancellationToken = default);
        void ReloadSessions(int maxSessions = 200);
        void InitializeRun(ExecutionLogSession session, Job job, Guid instanceId);
        void RegisterSecrets(IEnumerable<string> values);
        void Record(ExecutionLogSession session, LogEvent entry);
        void Finish(ExecutionLogSession session, LogOutcome outcome, string reason);
    }

}
