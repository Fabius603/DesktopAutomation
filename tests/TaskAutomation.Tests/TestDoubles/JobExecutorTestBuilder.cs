using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Steps;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed class JobExecutorTestBuilder
{
    private readonly List<Job> _jobs = [];
    public RecordingDesktopResultOverlay Overlay { get; } = new();
    public RecordingExecutionLogService Logs { get; } = new();
    public ControlledDelayService Delay { get; } = new();
    public DelegateScriptExecutor Scripts { get; } = new();
    public SequenceWindowsStateService WindowsStates { get; private set; } = new(new NetworkConnectivityQueryResult());
    public RecordingWindowsSettingService WindowsSettings { get; } = new();

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
            new NoOpCameraCaptureService(),
            Logs,
            Delay,
            WindowsStates,
            windowsSettingService: WindowsSettings);
        await executor.ReloadJobsAsync();
        await executor.ReloadMakrosAsync();
        return executor;
    }
}

internal sealed class RecordingWindowsSettingService : IWindowsSystemSettingService
{
    public List<WindowsSettingChange> Changes { get; } = [];

    public Task<WindowsSettingChangeResult> ChangeAsync(
        WindowsSettingChange change,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Changes.Add(change);
        return Task.FromResult(new WindowsSettingChangeResult
        {
            Success = true,
            Status = WindowsCapabilityStatus.Success,
            SettingId = change.SettingId,
            AppliedValue = change.Parameters.Values.FirstOrDefault() ?? string.Empty
        });
    }
}
