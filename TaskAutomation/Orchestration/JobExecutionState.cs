namespace TaskAutomation.Orchestration;

/// <summary>Der einheitliche Laufzeitstatus einer einzelnen Job-Instanz.</summary>
public enum JobExecutionState
{
    Starting,
    RunningStartSteps,
    RunningSteps,
    StopRequested,
    RunningEndSteps,
    ForceStopRequested,
    Completed,
    Cancelled,
    Failed
}

public static class JobExecutionStateExtensions
{
    public static bool IsTerminal(this JobExecutionState state)
        => state is JobExecutionState.Completed
            or JobExecutionState.Cancelled
            or JobExecutionState.Failed;

    public static bool CanRequestStop(this JobExecutionState state)
        => state is JobExecutionState.Starting
            or JobExecutionState.RunningStartSteps
            or JobExecutionState.RunningSteps;
}
