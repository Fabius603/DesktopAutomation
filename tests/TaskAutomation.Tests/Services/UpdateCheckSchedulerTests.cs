using DesktopAutomationApp.Services;

namespace TaskAutomation.Tests.Services;

public sealed class UpdateCheckSchedulerTests
{
    [Fact]
    public async Task Scheduler_ChecksImmediatelyAndAgainAfterInterval()
    {
        var secondCheck = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCount = 0;

        using var scheduler = new UpdateCheckScheduler(
            () =>
            {
                if (Interlocked.Increment(ref checkCount) >= 2)
                    secondCheck.TrySetResult();
                return Task.CompletedTask;
            },
            TimeSpan.FromMilliseconds(20));

        await secondCheck.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(Volatile.Read(ref checkCount) >= 2);
    }

    [Fact]
    public async Task Dispose_StopsFutureChecks()
    {
        var firstCheck = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCount = 0;
        var scheduler = new UpdateCheckScheduler(
            () =>
            {
                Interlocked.Increment(ref checkCount);
                firstCheck.TrySetResult();
                return Task.CompletedTask;
            },
            TimeSpan.FromSeconds(10));

        await firstCheck.Task.WaitAsync(TimeSpan.FromSeconds(2));
        scheduler.Dispose();
        var countAfterDispose = Volatile.Read(ref checkCount);
        await Task.Delay(50);

        Assert.Equal(countAfterDispose, Volatile.Read(ref checkCount));
    }
}
