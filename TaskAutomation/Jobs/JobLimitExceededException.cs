using System;

namespace TaskAutomation.Jobs
{
    /// <summary>
    /// Wird ausgelöst, wenn die maximale Anzahl gleichzeitig laufender Jobs überschritten wird.
    /// </summary>
    public sealed class JobLimitExceededException : InvalidOperationException
    {
        public int Limit { get; }
        public string JobName { get; }

        public JobLimitExceededException(string jobName, int limit)
            : base($"Maximale Anzahl gleichzeitig laufender Jobs ({limit}) erreicht. Job '{jobName}' wurde nicht gestartet.")
        {
            JobName = jobName;
            Limit   = limit;
        }
    }
}
