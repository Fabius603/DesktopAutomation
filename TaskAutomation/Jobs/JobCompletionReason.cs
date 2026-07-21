namespace TaskAutomation.Jobs;

public enum JobCompletionReason
{
    Completed,
    EndJobStep,
    Cancelled,
    StepFailed
}
