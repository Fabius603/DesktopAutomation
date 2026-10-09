using TaskAutomation.Orchestration;

namespace TaskAutomation.Jobs;

public sealed record JobExecutionOutcome(JobExecutionState State)
{
    public void EnsureSuccessful()
    {
        if (State == JobExecutionState.Cancelled) throw new OperationCanceledException("Child job was stopped.");
        if (State != JobExecutionState.Completed) throw new InvalidOperationException("Child job did not complete successfully.");
    }
}
