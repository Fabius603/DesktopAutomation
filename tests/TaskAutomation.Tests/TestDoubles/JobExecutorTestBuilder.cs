using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed class JobExecutorTestBuilder
{
    private readonly List<Job> _jobs = [];
    public RecordingDesktopResultOverlay Overlay { get; } = new();
    public RecordingExecutionLogService Logs { get; } = new();
    public ControlledDelayService Delay { get; } = new();
    public DelegateScriptExecutor Scripts { get; } = new();
    public SequenceWindowsStateService WindowsStates { get; private set; } = new(new NetworkConnectivityQueryResult());

    public JobExecutorTestBuilder WithJobs(params Job[] jobs) { _jobs.AddRange(jobs); return this; }
    public JobExecutorTestBuilder WithWindowsStates(params WindowsStateQueryResult[] states)
    { WindowsStates = new(states); return this; }

    public async Task<JobExecutor> BuildAsync()
    {
        var executor = new JobExecutor(
            NullLogger<JobExecutor>.Instance,
            new InMemoryRepository<Job>(_jobs),
            new InMemoryRepository<Makro>(),
            new NoOpMakroExecutor(),
            Scripts,
            new NoOpRecordingIndicator(),
            new NoOpYoloManager(),
            new NoOpImageDisplayService(),
            Overlay,
            new NoOpDesktopCaptureService(),
            Logs,
            Delay,
            WindowsStates);
        await executor.ReloadJobsAsync();
        await executor.ReloadMakrosAsync();
        return executor;
    }
}
