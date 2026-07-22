using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.Steps;

public sealed class WindowsStateQueryStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_MapsCompleteSnapshotAndStoresResult()
    {
        var capturedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var snapshot = new WindowsStateSnapshot
        {
            Status = WindowsCapabilityStatus.Success, CapturedAt = capturedAt, Exists = true, IsActive = true,
            IsConnected = true, IsEnabled = true, IsMuted = true, IsCharging = true, PendingRestart = true,
            Count = 3, Value = 12, Percentage = 72.5, FreeSpaceGb = 8.25, Name = "Device", Id = "id",
            Text = "text", Path = "path", Connectivity = WindowsConnectivity.Internet,
            ConnectionType = WindowsConnectionType.WiFi, PowerSource = WindowsPowerSource.Ac,
            SessionState = WindowsSessionState.Active, DeviceState = WindowsDeviceState.Connected,
            OnOffState = WindowsOnOffState.On, Items = ["one", "two"]
        };
        var service = new SequenceWindowsStateService(snapshot);
        var context = new PipelineContextStub();
        var step = new WindowsStateQueryStep
        {
            Id = "audio", Settings = new() { QueryType = "audio.volume", Parameters = new() { ["X"] = "Y" } }
        };

        var result = Assert.IsType<WindowsStateQueryResult>(await new WindowsStateQueryStepHandler(service)
            .ExecuteAsync(step, context, CancellationToken.None));

        Assert.True(result.WasExecuted);
        Assert.Equal(72.5, result.Percentage);
        Assert.True(result.IsMuted);
        Assert.Equal(capturedAt, result.CapturedAt);
        Assert.Equal(["one", "two"], result.Items);
        Assert.Same(result, context.Results.GetRaw("audio"));
        Assert.Equal("audio.volume", service.Queries.Single().QueryType);
        Assert.Equal("Y", service.Queries.Single().Parameters["x"]);
    }

    [Fact]
    public async Task ExecuteAsync_RepeatedCallsUseFreshServiceValues()
    {
        var service = new SequenceWindowsStateService(
            new WindowsStateSnapshot { Percentage = 25, IsMuted = false },
            new WindowsStateSnapshot { Percentage = 70, IsMuted = true });
        var handler = new WindowsStateQueryStepHandler(service);
        var context = new PipelineContextStub();
        var step = new WindowsStateQueryStep { Id = "audio", Settings = new() { QueryType = "audio.volume" } };

        var first = Assert.IsType<WindowsStateQueryResult>(await handler.ExecuteAsync(step, context, default));
        var second = Assert.IsType<WindowsStateQueryResult>(await handler.ExecuteAsync(step, context, default));

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
        var service = new SequenceWindowsStateService(new WindowsStateSnapshot
            { Status = WindowsCapabilityStatus.AccessDenied, ErrorCode = "ACCESS_DENIED", ErrorMessage = "denied" });
        var result = Assert.IsType<WindowsStateQueryResult>(await new WindowsStateQueryStepHandler(service)
            .ExecuteAsync(new WindowsStateQueryStep(), new PipelineContextStub(), default));
        Assert.False(result.IsAvailable);
        Assert.Equal(WindowsCapabilityStatus.AccessDenied, result.Status);
        Assert.Equal("ACCESS_DENIED", result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledTokenDoesNotQueryService()
    {
        var service = new SequenceWindowsStateService(new WindowsStateSnapshot());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new WindowsStateQueryStepHandler(service)
            .ExecuteAsync(new WindowsStateQueryStep(), new PipelineContextStub(), cts.Token));
        Assert.Empty(service.Queries);
    }
}
