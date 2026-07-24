using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class CameraCaptureStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_CapturesSelectedCameraAndStoresCompleteImageResult()
    {
        using var bitmap = new Bitmap(8, 6);
        var timestamp = DateTime.UtcNow.AddMilliseconds(-10);
        var service = new RecordingCameraCaptureService(
            new CameraCaptureFrame(bitmap, timestamp));
        var context = new PipelineContextStub { CameraCaptureService = service };
        var step = new CameraCaptureStep
        {
            Id = "camera",
            Settings = new() { CameraId = "device-path", CameraName = "USB Camera" }
        };

        var result = Assert.IsType<CameraCaptureResult>(
            await new CameraCaptureStepHandler().ExecuteAsync(step, context, default));

        Assert.Equal("device-path", Assert.Single(service.CapturedIds));
        Assert.Same(bitmap, result.Image);
        Assert.Equal(new Rectangle(0, 0, 8, 6), result.Bounds);
        Assert.Equal(Point.Empty, result.Offset);
        Assert.True(result.IsFresh);
        Assert.Equal(timestamp, result.CaptureTimestampUtc);
        Assert.Same(result, context.Results.GetRaw("camera"));
    }

    [Fact]
    public async Task ExecuteAsync_CaptureFailureDoesNotStoreResult()
    {
        var service = new RecordingCameraCaptureService(
            error: new InvalidOperationException("camera unavailable"));
        var context = new PipelineContextStub { CameraCaptureService = service };
        var step = new CameraCaptureStep
            { Id = "camera", Settings = new() { CameraId = "missing" } };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new CameraCaptureStepHandler().ExecuteAsync(step, context, default));

        Assert.Null(context.Results.GetRaw("camera"));
    }

    [Fact]
    public async Task ExecuteAsync_CancellationPropagatesWithoutStoredResult()
    {
        var service = new RecordingCameraCaptureService(
            error: new OperationCanceledException());
        var context = new PipelineContextStub { CameraCaptureService = service };
        var step = new CameraCaptureStep
            { Id = "camera", Settings = new() { CameraId = "device-path" } };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new CameraCaptureStepHandler().ExecuteAsync(step, context, default));

        Assert.Null(context.Results.GetRaw("camera"));
    }

    private sealed class RecordingCameraCaptureService(
        CameraCaptureFrame? frame = null,
        Exception? error = null) : ICameraCaptureService
    {
        public List<string> CapturedIds { get; } = [];
        public IReadOnlyList<CameraDeviceInfo> GetAvailableCameras() => [];

        public Task<CameraCaptureFrame> CaptureAsync(
            string cameraId,
            CancellationToken cancellationToken)
        {
            CapturedIds.Add(cameraId);
            return error is null
                ? Task.FromResult(frame!)
                : Task.FromException<CameraCaptureFrame>(error);
        }

        public void Dispose() { }
    }
}
