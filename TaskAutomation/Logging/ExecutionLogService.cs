using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using TaskAutomation.Steps;

namespace TaskAutomation.Logging;

/// <summary>Job adapter over the canonical v2 repository. Never reads historical formats.</summary>
public sealed class ExecutionLogService : IExecutionLogService, IDisposable
{
    private readonly ILogRepository _repository;
    private readonly bool _ownsRepository;
    private readonly Dictionary<Guid, ExecutionLogSession> _sessions = new();
    private readonly object _gate = new();
    private int _requested = 200;
    public ExecutionLogService(ILogRepository repository) { _repository = repository; _repository.EntryWritten += OnEntry; }
    internal ExecutionLogService(string rootDirectory) : this(new LogRepository(rootDirectory)) { _ownsRepository = true; }
    public event EventHandler<ExecutionLogEntry>? EntryWritten;
    public event EventHandler<ExecutionLogSession>? SessionChanged;
    public IReadOnlyList<ExecutionLogSession> Sessions { get { lock (_gate) return _sessions.Values.OrderByDescending(s => s.StartedAt).Take(_requested).ToArray(); } }
    public bool HasMoreSessions => _repository.ReadRuns().Count(run => run.Source == LogSource.Job) > _requested;

    public ExecutionLogSession BeginJob(Guid jobId, string jobName, JobStartContext? startContext = null)
    {
        lock (_gate)
        {
            startContext ??= JobStartContext.Unknown;
            var id = Guid.NewGuid();
            var run = _repository.SaveRun(new LogRun
            {
                Id = id,
                InstanceId = startContext.InstanceId ?? id,
                SourceId = jobId,
                Source = LogSource.Job,
                Name = jobName,
                ExecutionNumber = _repository.NextExecutionNumber(jobId),
                StartedAt = DateTimeOffset.UtcNow,
                Origin = startContext.Source.ToString(),
                OriginName = startContext.SourceName,
                OriginId = startContext.SourceId,
                Trigger = startContext.Trigger,
                Context = new(id, startContext.InstanceId ?? id, startContext.TriggerId,
                    startContext.Source == JobStartSource.Automation ? startContext.SourceId : null,
                    ParentRunId: startContext.ParentRunId)
            });
            var session = FromRun(run);
            _sessions[id] = session;
            Record(session, new LogEvent { Code = LogCodes.RunStarted, Message = "Ausführung gestartet." });
            SessionChanged?.Invoke(this, session);
            return session;
        }
    }

    public void InitializeRun(ExecutionLogSession session, Job job, Guid instanceId)
    {
        var run = _repository.ReadRuns().First(run => run.Id == session.Id);
        _repository.SaveRun(run with { InstanceId = instanceId, Context = run.Context with { InstanceId = instanceId }, Steps = LogRunSnapshots.Steps(job) });
    }

    public void RegisterSecrets(IEnumerable<string> values) => _repository.Privacy.RegisterSecrets(values);
    public void Write(ExecutionLogSession session, ExecutionLogLevel level, string message, string? details = null,
        string? stepId = null, string? stepType = null, long? durationMs = null, JobExecutionState? jobState = null)
    {
        if (jobState.HasValue) session.JobState = jobState;
        Record(session, new LogEvent
        {
            Level = level,
            Message = message,
            Details = details,
            DurationMs = durationMs,
            Code = jobState.HasValue ? LogCodes.RunState : LogCodes.Message,
            ProblemId = level >= ExecutionLogLevel.Warning ? LogAmbient.Current.StepExecutionId : null,
            Context = LogAmbient.Current with { StepId = stepId ?? LogAmbient.Current.StepId },
            Parameters = new() { ["StepType"] = stepType, ["JobState"] = (jobState ?? session.JobState)?.ToString() }
        });
    }

    public void Record(ExecutionLogSession session, LogEvent entry)
    {
        var run = _repository.ReadRuns().First(run => run.Id == session.Id);
        if (entry.Parameters.GetValueOrDefault("DebugState") is { } state && run.EndedAt is null)
        {
            run = _repository.SaveRun(run with { Outcome = state == "Paused" ? LogOutcome.Paused : LogOutcome.Running });
            session.Outcome = run.Outcome;
            SessionChanged?.Invoke(this, session);
        }
        _repository.Append(entry with
        {
            Source = LogSource.Job,
            SourceId = session.SourceId,
            SourceName = session.Name,
            Area = entry.Area.Length > 0 ? entry.Area : "Execution",
            Context = entry.Context with
            {
                RunId = session.Id,
                InstanceId = run.InstanceId,
                TriggerId = run.Context.TriggerId,
                AutomationId = run.Context.AutomationId,
                ParentRunId = run.Context.ParentRunId
            }
        });
    }

    // Existing UI/test callers delegate to the canonical completion rule.
    public void Complete(ExecutionLogSession session, bool success, string? details = null, bool cancelled = false)
        => Finish(session, cancelled ? LogOutcome.Stopped : success ? LogOutcome.Successful : LogOutcome.Failed, details ?? "Unknown");
    public void Finish(ExecutionLogSession session, LogOutcome outcome, string reason)
    {
        var run = _repository.ReadRuns().First(run => run.Id == session.Id);
        var ended = DateTimeOffset.UtcNow;
        outcome = LogOutcomeRules.Complete(outcome, run.ErrorCount, run.WarningCount);
        var final = _repository.SaveRun(run with
        {
            EndedAt = ended,
            DurationMs = (long)(ended - run.StartedAt).TotalMilliseconds,
            Outcome = outcome,
            CompletionReason = reason
        });
        session.EndedAt = final.EndedAt; session.DurationMs = final.DurationMs; session.Outcome = outcome;
        Record(session, new LogEvent
        {
            Code = LogCodes.RunCompleted,
            Level = outcome == LogOutcome.Failed ? ExecutionLogLevel.Error : ExecutionLogLevel.Information,
            Message = outcome == LogOutcome.Stopped ? "Ausführung gestoppt."
                : outcome == LogOutcome.Failed ? "Ausführung fehlerhaft beendet." : "Ausführung beendet.",
            DurationMs = final.DurationMs,
            Parameters = new() { ["Outcome"] = outcome.ToString(), ["Reason"] = reason }
        });
        SessionChanged?.Invoke(this, session);
    }

    public IReadOnlyList<ExecutionLogEntry> ReadEntries(Guid sessionId, int maxEntries = 2000)
        => maxEntries <= 0 ? [] : _repository.Query(new(RunId: sessionId, PageSize: Math.Min(maxEntries, 10000)))
            .Entries.OrderBy(entry => entry.Sequence).Select(ToEntry).ToArray();
    public Task<IReadOnlyList<ExecutionLogEntry>> ReadEntriesAsync(Guid sessionId, int maxEntries = 2000, CancellationToken cancellationToken = default)
        => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); return ReadEntries(sessionId, maxEntries); }, cancellationToken);
    public void ReloadSessions(int maxSessions = 200)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSessions);
        _requested = maxSessions;
        lock (_gate)
            foreach (var run in _repository.ReadRuns().Where(run => run.Source == LogSource.Job))
                if (!_sessions.ContainsKey(run.Id)) _sessions[run.Id] = FromRun(run);
    }
    private ExecutionLogSession FromRun(LogRun run) => new(run.Id, ExecutionLogKind.Job, run.SourceId, run.Name,
        Path.Combine(_repository.DirectoryPath, $"{run.Id:N}.run.json"), run.StartedAt, run.EndedAt, run.ExecutionNumber,
        new JobStartContext(Enum.TryParse<JobStartSource>(run.Origin, out var origin) ? origin : JobStartSource.Unknown,
            run.OriginName, run.OriginId, run.InstanceId, run.Context.TriggerId, run.Context.ParentRunId, run.Trigger), run.DurationMs)
    { Outcome = run.Outcome };
    private static ExecutionLogEntry ToEntry(LogEvent entry) => new()
    {
        Id = entry.Id,
        Sequence = entry.Sequence,
        SessionId = entry.Context.RunId ?? Guid.Empty,
        Timestamp = entry.Timestamp,
        Level = entry.Level,
        Message = entry.Message,
        Details = entry.Details,
        StepId = entry.Context.StepId,
        StepType = entry.Parameters.GetValueOrDefault("StepType"),
        DurationMs = entry.DurationMs,
        JobState = Enum.TryParse<JobExecutionState>(entry.Parameters.GetValueOrDefault("JobState"), out var state) ? state : null
    };
    private void OnEntry(object? sender, LogEvent entry) { if (entry.Source == LogSource.Job) EntryWritten?.Invoke(this, ToEntry(entry)); }
    public void Dispose() { _repository.EntryWritten -= OnEntry; if (_ownsRepository && _repository is IDisposable disposable) disposable.Dispose(); }
}
