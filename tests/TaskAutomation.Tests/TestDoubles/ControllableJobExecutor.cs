using ImageCapture.DesktopDuplication.RecordingIndicator;
using ImageDetection.YOLO;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed record ExecutorInvocation(Guid JobId, JobStartContext Context,
    JobExecutionCancellation Cancellation, TaskCompletionSource Completion);

internal sealed class ControllableJobExecutor : IJobExecutor
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Job> _jobs;
    private readonly Dictionary<string, Makro> _makros;
    public event EventHandler<JobErrorEventArgs>? JobErrorOccurred;
    public event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;
    public List<ExecutorInvocation> Invocations { get; } = [];
    public IReadOnlyDictionary<string, Job> AllJobs => _jobs;
    public IReadOnlyDictionary<string, Makro> AllMakros => _makros;
    public IYoloManager YoloManager { get; } = new NoOpYoloManager();
    public IMakroExecutor MakroExecutor { get; }
    public Job? CurrentJob => null;

    public ControllableJobExecutor(IEnumerable<Job> jobs, IEnumerable<Makro>? makros = null,
        IMakroExecutor? makroExecutor = null)
    {
        _jobs = jobs.ToDictionary(job => job.Id.ToString(), StringComparer.OrdinalIgnoreCase);
        _makros = (makros ?? []).ToDictionary(makro => makro.Id.ToString(), StringComparer.OrdinalIgnoreCase);
        MakroExecutor = makroExecutor ?? new NoOpMakroExecutor();
    }

    public Task ExecuteJob(Guid jobId, JobStartContext startContext, JobExecutionCancellation cancellation)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate) Invocations.Add(new(jobId, startContext, cancellation, completion));
        return completion.Task;
    }

    public Task ExecuteJob(string jobName, CancellationToken ct = default) => Task.CompletedTask;
    public Task ExecuteJob(Guid jobId, CancellationToken ct = default) => Task.CompletedTask;
    public Task ExecuteJob(Guid jobId, JobStartContext startContext, CancellationToken ct = default) => Task.CompletedTask;
    public Task ReloadJobsAsync() => Task.CompletedTask;
    public Task ReloadMakrosAsync() => Task.CompletedTask;
    public void StartRecordingOverlay(RecordingIndicatorOptions? options = null) { }
    public void StopRecordingOverlay() { }
    public void ReportStepError(string stepType, Exception exception) =>
        JobStepErrorOccurred?.Invoke(this, new JobStepErrorEventArgs(string.Empty, stepType, exception));
    public void RaiseJobError(JobErrorEventArgs error) => JobErrorOccurred?.Invoke(this, error);

    public ExecutorInvocation[] SnapshotInvocations()
    {
        lock (_gate) return Invocations.ToArray();
    }
}
