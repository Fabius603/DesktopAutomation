using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using Point = TaskAutomation.Contracts.Geometry.PixelPoint;
using Rectangle = TaskAutomation.Contracts.Geometry.PixelRegion;

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
            Settings = new()
            {
                CameraId = "device-path",
                CameraName = "USB Camera",
                QualityMode = CameraQualityMode.Specific,
                Width = 1920,
                Height = 1080,
                FramesPerSecond = 30,
                PixelFormat = "MJPG"
            }
        };

        var result = Assert.IsType<CameraCaptureResult>(
            await new CameraCaptureStepHandler().ExecuteAsync(step, context, default));

        Assert.Equal("device-path", Assert.Single(service.CapturedIds));
        var options = Assert.Single(service.CapturedOptions);
        Assert.Equal(CameraQualityMode.Specific, options.QualityMode);
        Assert.Equal(1920, options.Width);
        Assert.Equal(1080, options.Height);
        Assert.Equal(30, options.FramesPerSecond);
        Assert.Equal("MJPG", options.PixelFormat);
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
        public List<CameraCaptureOptions> CapturedOptions { get; } = [];
        public IReadOnlyList<CameraDeviceInfo> GetAvailableCameras() => [];
        public IReadOnlyList<CameraCaptureMode> GetSupportedModes(string cameraId) => [];

        public Task<CameraCaptureFrame> CaptureAsync(
            string cameraId,
            CameraCaptureOptions options,
            CancellationToken cancellationToken)
        {
            CapturedIds.Add(cameraId);
            CapturedOptions.Add(options);
            return error is null
                ? Task.FromResult(frame!)
                : Task.FromException<CameraCaptureFrame>(error);
        }

        public void Dispose() { }
    }
}
