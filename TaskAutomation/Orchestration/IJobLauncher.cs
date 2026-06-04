namespace TaskAutomation.Orchestration
{
    /// <summary>
    /// Minimal interface for starting and cancelling jobs via the dispatcher.
    /// Used by JobExecutor to avoid a circular dependency on IJobDispatcher.
    /// </summary>
    public interface IJobLauncher
    {
        Guid StartJob(Guid id);
        Task StartJobAsync(Guid id, CancellationToken ct);
        void CancelJob(Guid instanceId);
    }
}
