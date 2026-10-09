using ImageDetection.YOLO;
using TaskAutomation.Orchestration;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.WindowsIntegration;
namespace TaskAutomation.Tests.Orchestration;

public sealed class ResourceOwnershipTests
{
    [Fact]
    public async Task SharedModel_RemainsLoadedUntilLastOwnerReleasesIt()
    {
        var manager = new RecordingYoloManager();
        var first = await ModelLifetime.AcquireAsync(manager, "model", default);
        var second = await ModelLifetime.AcquireAsync(manager, "model", default);
        first.Dispose();
        Assert.True(ModelLifetime.InUse(manager, "model"));
        Assert.Empty(manager.UnloadedModels);
        second.Dispose();
        Assert.False(ModelLifetime.InUse(manager, "model"));
        Assert.Equal(["model"], manager.UnloadedModels);
        second.Dispose();
        Assert.Single(manager.UnloadedModels);
    }

    [Fact]
    public async Task CancelledModelWaiter_DoesNotCancelAnotherOwnersInitialization()
    {
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new RecordingYoloManager { EnsureAction = _ => loaded.Task };
        using var cancel = new CancellationTokenSource();
        var first = ModelLifetime.AcquireAsync(manager, "model", cancel.Token);
        var second = ModelLifetime.AcquireAsync(manager, "model", default);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.Empty(manager.UnloadedModels);
        loaded.SetResult();
        using (await second.WaitAsync(TimeSpan.FromSeconds(5)))
            Assert.True(ModelLifetime.InUse(manager, "model"));
        Assert.Single(manager.UnloadedModels);
    }

    [Fact]
    public void RecordingIndicator_RemainsVisibleForAnotherActiveCapture()
    {
        using var overlay = new NoOpRecordingIndicator();
        var ownership = new RecordingIndicatorOwnership(overlay);
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        ownership.Show(first, new() { MonitorIndex = 0 });
        ownership.Show(second, new() { MonitorIndex = 1 });
        ownership.Release(second);
        Assert.True(overlay.IsRunning);
        Assert.Equal(0, overlay.StartedMonitorIndices.Last());
        ownership.Release(first);
        Assert.False(overlay.IsRunning);
    }

    [Fact]
    public void InputBlocking_OneJobEndingDoesNotReleaseAnotherJobsBlock()
    {
        var ownership = new InputBlockOwnership();
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        ownership.Acquire(first); ownership.Acquire(second); ownership.Acquire(first);
        Assert.False(ownership.Release(first));
        Assert.Equal(1, ownership.Count);
        Assert.False(ownership.Release(first));
        Assert.True(ownership.Release(second));
    }
}
