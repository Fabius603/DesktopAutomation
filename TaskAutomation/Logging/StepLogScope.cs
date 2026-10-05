using System.Diagnostics;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Logging;

/// <summary>The single lifecycle boundary for ordinary steps and control-flow operations.</summary>
internal sealed class StepLogScope : IDisposable
{
    private static readonly AsyncLocal<(string Phase, int? Iteration)?> Observation = new();
    public static string? CurrentPhase => Observation.Value?.Phase;
    public static int? CurrentIteration => Observation.Value?.Iteration;
    private readonly (string Phase, int? Iteration)? _previousObservation;
    private readonly IExecutionLogService _logs;
    private readonly ExecutionLogSession _run;
    private readonly JobStep _step;
    private readonly string _phase;
    private readonly int? _iteration;
    private readonly IDisposable _context;
    private readonly Stopwatch _duration = Stopwatch.StartNew();
    private bool _ended;

    public StepLogScope(IExecutionLogService logs, ExecutionLogSession run, JobStep step, string phase, int? iteration = null)
    {
        _logs = logs; _run = run; _step = step; _phase = phase; _iteration = iteration;
        _previousObservation = Observation.Value;
        Observation.Value = (StepLogEvents.Phase(phase), iteration);
        _context = LogAmbient.Push(LogAmbient.Current with { StepId = step.Id, StepExecutionId = Guid.NewGuid() });
        _logs.Record(run, StepLogEvents.Create(step, LogCodes.StepStarted, phase, iteration));
    }

    public void Complete(JobStep? observed = null, object? result = null, string reason = "Completed",
        ExecutionLogLevel? level = null, LogBranchDecision? branch = null, bool skipped = false)
    {
        if (_ended) return;
        _ended = true;
        var cancelled = result is UserChoiceResult { WasCancelled: true };
        if (reason == "Completed" && result is IActionExecutionResult { Success: false })
            reason = result is JobExecutionResult ? "StartRejected" : "ActionUnsuccessful";
        _logs.Record(_run, StepLogEvents.Create(observed ?? _step,
            cancelled ? LogCodes.StepCancelled : skipped ? LogCodes.StepSkipped : LogCodes.StepCompleted,
            _phase, _iteration, level ?? StepLogEvents.ResultLevel(result),
            cancelled ? "UserCancelled" : reason, _duration.ElapsedMilliseconds, result: result) with
        { BranchDecision = branch });
    }

    public void Fail(Exception error, JobStep? observed = null)
    {
        if (_ended) return;
        _ended = true;
        var cancelled = error is OperationCanceledException;
        _logs.Record(_run, StepLogEvents.Create(observed ?? _step,
            cancelled ? LogCodes.StepCancelled : LogCodes.StepFailed, _phase, _iteration,
            cancelled ? ExecutionLogLevel.Information : ExecutionLogLevel.Error,
            cancelled ? "Cancellation" : "Exception", _duration.ElapsedMilliseconds, error: cancelled ? null : error));
    }

    public static void Skip(IExecutionLogService logs, ExecutionLogSession run, JobStep step, string phase, int? iteration, string reason)
    {
        using var context = LogAmbient.Push(LogAmbient.Current with { StepId = step.Id, StepExecutionId = Guid.NewGuid() });
        logs.Record(run, StepLogEvents.Create(step, LogCodes.StepSkipped, phase, iteration, reason: reason));
    }

    public void Dispose() { Observation.Value = _previousObservation; _context.Dispose(); }
}
