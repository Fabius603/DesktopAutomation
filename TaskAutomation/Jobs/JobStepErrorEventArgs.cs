using System;

namespace TaskAutomation.Jobs
{
    public class JobStepErrorEventArgs : EventArgs
    {
        public string JobName { get; }
        public string StepType { get; }
        public Exception Exception { get; }
        public string ErrorMessage { get; }

        public JobStepErrorEventArgs(string jobName, string stepType, Exception exception)
        {
            JobName = jobName;
            StepType = stepType;
            Exception = exception;
            ErrorMessage = exception.Message;
        }
    }
}
