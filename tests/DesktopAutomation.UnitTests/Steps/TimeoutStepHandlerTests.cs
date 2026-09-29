using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class TimeoutStepHandlerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2500)]
    public async Task ExecuteAsync_ForwardsExactDelayAndStoresSuccess(int milliseconds)
    {
        var delay = new RecordingDelay();
        var context = new PipelineContextStub();
        var step = new TimeoutStep { Id = "timeout", Settings = new() { DelayMs = milliseconds } };
        var result = Assert.IsType<TimeoutResult>(await new TimeoutStepHandler(delay).ExecuteAsync(step, context, default));
        Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), Assert.Single(delay.Delays));
        Assert.True(result.Success);
        Assert.True(result.WasExecuted);
        Assert.Same(result, context.Results.GetRaw("timeout"));
    }

    [Fact]
    public async Task ExecuteAsync_CancellationFromDelayPropagatesAndDoesNotStoreResult()
    {
        var delay = new RecordingDelay { ThrowCancellation = true };
        var context = new PipelineContextStub();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new TimeoutStepHandler(delay)
            .ExecuteAsync(new TimeoutStep { Id = "timeout" }, context, new CancellationToken(true)));
        Assert.Null(context.Results.GetRaw("timeout"));
    }

    private sealed class RecordingDelay : TaskAutomation.Timing.IPreciseDelayService
    {
        public List<TimeSpan> Delays { get; } = [];
        public bool ThrowCancellation { get; init; }
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        { Delays.Add(delay); if (ThrowCancellation) throw new OperationCanceledException(cancellationToken); return Task.CompletedTask; }
        public Task DelayUntilAsync(long targetTimestamp, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
