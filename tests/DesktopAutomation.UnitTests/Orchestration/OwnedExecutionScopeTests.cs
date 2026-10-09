using TaskAutomation.Orchestration;
namespace TaskAutomation.Tests.Orchestration;

public sealed class OwnedExecutionScopeTests
{

    [Fact]
    public async Task Admission_WaitsForCapacityAndReleasesItAfterCompletion()
    {
        using var scope = new OwnedExecutionScope(maxConcurrentExecutions: 1);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = await scope.StartAsync(_ => release.Task, default);
        var secondAdmission = scope.StartAsync(_ => Task.CompletedTask, default);
        Assert.False(secondAdmission.IsCompleted);
        Assert.Equal(1, scope.PendingCount);
        release.SetResult();
        var second = await secondAdmission.WaitAsync(TimeSpan.FromSeconds(5));
        await second.WaitAsync(TimeSpan.FromSeconds(5));
        await first;
        await scope.DrainAsync();
    }

    [Fact]
    public async Task Admission_CancelledWaitNeverStartsOperation()
    {
        using var scope = new OwnedExecutionScope(maxConcurrentExecutions: 1);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = await scope.StartAsync(_ => release.Task, default);
        using var cancellation = new CancellationTokenSource();
        var launched = false;
        var pending = scope.StartAsync(_ => { launched = true; return Task.CompletedTask; }, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.False(launched);
        release.SetResult();
        await first;
        await scope.DrainAsync();
    }

    [Fact]
    public async Task Drain_WaitsForParallelWork_WithoutCancellingNormalCompletion()
    {
        using var scope = new OwnedExecutionScope();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var child = scope.Start(async token => { started.SetResult(); await release.Task; token.ThrowIfCancellationRequested(); }, default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var drain = scope.DrainAsync();
        Assert.False(drain.IsCompleted);
        release.SetResult();
        await drain.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(child.IsCompletedSuccessfully);
        Assert.Equal(0, scope.PendingCount);
    }

    [Fact]
    public async Task Failure_StopsSiblings_AndIsReturnedToOwner()
    {
        var siblingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scope = new OwnedExecutionScope();
        scope.Start(async token => { siblingStarted.SetResult(); try { await Task.Delay(Timeout.Infinite, token); } finally { stopObserved.TrySetResult(); } }, default);
        await siblingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        scope.Start(_ => Task.FromException(new InvalidOperationException("failed")), default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.DrainAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        await stopObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(scope.Failure);
    }

    [Fact]
    public async Task StopBudget_Escalates_ButRetainsOwnershipUntilTermination()
    {
        using var stop = new CancellationTokenSource();
        using var scope = new OwnedExecutionScope();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var forced = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        scope.Start(async _ =>
        {
            using var registration = scope.ForceToken.Register(() => forced.TrySetResult());
            started.SetResult();
            await release.Task;
        }, stop.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        stop.Cancel();
        var drain = scope.DrainAsync(TimeSpan.Zero, stop.Token);
        await forced.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(scope.StopEscalated);
        Assert.False(drain.IsCompleted);
        Assert.Equal(1, scope.PendingCount);
        release.SetResult();
        await drain.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
