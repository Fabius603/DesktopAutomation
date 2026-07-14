namespace TaskAutomation.Orchestration
{
    /// <summary>
    /// Minimal interface for starting and cancelling jobs via the dispatcher.
    /// Used by JobExecutor to avoid a circular dependency on IJobDispatcher.
    /// </summary>
    public interface IJobLauncher
    {
        Guid StartJob(Guid id, TaskAutomation.Logging.JobStartContext? startContext = null);
        Task StartJobAsync(Guid id, CancellationToken ct, TaskAutomation.Logging.JobStartContext? startContext = null);
        void CancelJob(Guid instanceId);
    }
}
