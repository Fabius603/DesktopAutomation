using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Localization;

public sealed record StepErrorPresentation(string StepName, string Message, string ErrorCode)
{
    public static StepErrorPresentation Create(JobStepErrorEventArgs error)
    {
        var resourceKey = error.ErrorKind switch
        {
            StepErrorKind.FileNotFound => "Error.JobStepExecution.FileNotFound",
            StepErrorKind.DirectoryNotFound => "Error.JobStepExecution.DirectoryNotFound",
            StepErrorKind.AccessDenied => "Error.JobStepExecution.AccessDenied",
            StepErrorKind.TimedOut => "Error.JobStepExecution.TimedOut",
            StepErrorKind.InvalidConfiguration => "Error.JobStepExecution.InvalidConfiguration",
            StepErrorKind.InputOutput => "Error.JobStepExecution.InputOutput",
            StepErrorKind.ExternalProgram => "Error.JobStepExecution.ExternalProgram",
            _ => "Error.JobStepExecution.Unexpected"
        };

        return new StepErrorPresentation(
            StepLocalization.Type(error.StepType),
            Loc.Get(resourceKey),
            error.ErrorCode);
    }
}
