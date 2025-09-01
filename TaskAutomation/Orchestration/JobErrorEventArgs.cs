using System;

namespace TaskAutomation.Orchestration
{
    public class JobErrorEventArgs : EventArgs
    {
        public string JobName { get; }
        public Exception Exception { get; }
        public string ErrorMessage { get; }

        public JobErrorEventArgs(string jobName, Exception exception)
        {
            JobName = jobName;
            Exception = exception;
            ErrorMessage = exception.Message;
        }
    }
}
