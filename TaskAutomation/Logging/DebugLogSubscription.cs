using TaskAutomation.Orchestration;

namespace TaskAutomation.Logging;

internal sealed class DebugLogSubscription : IDisposable
{
    private readonly JobDebugSession? _debug;
    private readonly ExecutionLogSession _run;
    private readonly IExecutionLogService _logs;
    private JobDebugSessionState? _previous;
    public DebugLogSubscription(JobDebugSession? debug, ExecutionLogSession run, IExecutionLogService logs)
    { _debug = debug; _run = run; _logs = logs; if (debug is not null) debug.Changed += Changed; }
    private void Changed()
    {
        if (_debug is null || _debug.State == _previous) return;
        _previous = _debug.State;
        _logs.Record(_run, new LogEvent
        {
            Code = LogCodes.RunState,
            Message = "Debugstatus geändert.",
            Parameters = new() { ["DebugState"] = _debug.State.ToString() },
            Context = new(StepId: _debug.CurrentStepId),
            Phase = StepLogEvents.Phase(_debug.Phase),
            Iteration = _debug.Iteration
        });
    }
    public void Dispose() { if (_debug is not null) _debug.Changed -= Changed; }
}
