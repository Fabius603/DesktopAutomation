using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.Steps;

public sealed class WindowsStateQueryStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_MarksTypedServiceResultAndStoresIt()
    {
        var capturedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var snapshot = new AudioVolumeQueryResult
        {
            Status = WindowsCapabilityStatus.Success, CapturedAt = capturedAt, Exists = true,
            IsMuted = true, Percentage = 72.5, Id = "id", OnOffState = WindowsOnOffState.On
        };
        var service = new SequenceWindowsStateService(snapshot);
        var context = new PipelineContextStub();
        var step = new WindowsStateQueryStep
        {
            Id = "audio", Settings = new() { QueryType = "audio.volume", Parameters = new() { ["X"] = "Y" } }
        };

        var result = Assert.IsType<AudioVolumeQueryResult>(await new WindowsStateQueryStepHandler(service)
            .ExecuteAsync(step, context, CancellationToken.None));

        Assert.True(result.WasExecuted);
        Assert.Equal(72.5, result.Percentage);
        Assert.True(result.IsMuted);
        Assert.Equal(capturedAt, result.CapturedAt);
        Assert.Same(result, context.Results.GetRaw("audio"));
        Assert.Equal("audio.volume", service.Queries.Single().QueryType);
        Assert.Equal("Y", service.Queries.Single().Parameters["x"]);
    }

    [Fact]
    public async Task ExecuteAsync_RepeatedCallsUseFreshServiceValues()
    {
        var service = new SequenceWindowsStateService(
            new AudioVolumeQueryResult { Percentage = 25, IsMuted = false },
            new AudioVolumeQueryResult { Percentage = 70, IsMuted = true });
        var handler = new WindowsStateQueryStepHandler(service);
        var context = new PipelineContextStub();
        var step = new WindowsStateQueryStep { Id = "audio", Settings = new() { QueryType = "audio.volume" } };

        var first = Assert.IsType<AudioVolumeQueryResult>(await handler.ExecuteAsync(step, context, default));
        var second = Assert.IsType<AudioVolumeQueryResult>(await handler.ExecuteAsync(step, context, default));

        Assert.Equal(25, first.Percentage);
        Assert.False(first.IsMuted);
        Assert.Equal(70, second.Percentage);
        Assert.True(second.IsMuted);
        Assert.Same(second, context.Results.GetRaw("audio"));
        Assert.Equal(2, service.Queries.Count);
    }

    [Fact]
    public async Task ExecuteAsync_FailureSnapshotRemainsDistinguishableFromFalseState()
    {
        var service = new SequenceWindowsStateService(new NetworkConnectivityQueryResult
            { Status = WindowsCapabilityStatus.AccessDenied, ErrorCode = "ACCESS_DENIED", ErrorMessage = "denied" });
        var result = Assert.IsType<NetworkConnectivityQueryResult>(await new WindowsStateQueryStepHandler(service)
            .ExecuteAsync(new WindowsStateQueryStep(), new PipelineContextStub(), default));
        Assert.NotEqual(WindowsCapabilityStatus.Success, result.Status);
        Assert.Equal(WindowsCapabilityStatus.AccessDenied, result.Status);
        Assert.Equal("ACCESS_DENIED", result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledTokenDoesNotQueryService()
    {
        var service = new SequenceWindowsStateService(new NetworkConnectivityQueryResult());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new WindowsStateQueryStepHandler(service)
            .ExecuteAsync(new WindowsStateQueryStep(), new PipelineContextStub(), cts.Token));
        Assert.Empty(service.Queries);
    }
}
