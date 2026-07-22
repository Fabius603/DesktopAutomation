using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class DesktopDuplicationStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsMonitorCursorAndCompleteFrameMetadata()
    {
        using var bitmap = new Bitmap(4, 3);
        var timestamp = DateTime.UtcNow.AddMilliseconds(-5);
        var capture = new RecordingCaptureService(new CaptureFrame { Image = bitmap, Bounds = new(10, 20, 4, 3),
            Offset = new(10, 20), IsFresh = false, CaptureTimestampUtc = timestamp });
        var context = new PipelineContextStub { DesktopCaptureService = capture };
        var step = new DesktopDuplicationStep { Id = "capture", Settings = new() { DesktopIdx = 2, CaptureCursor = true } };
        var result = Assert.IsType<DesktopDuplicationResult>(await new DesktopDuplicationStepHandler().ExecuteAsync(step, context, default));
        Assert.Equal((2, true), Assert.Single(capture.Calls));
        Assert.Same(bitmap, result.Image);
        Assert.Equal(new Rectangle(10, 20, 4, 3), result.Bounds);
        Assert.Equal(new Point(10, 20), result.Offset);
        Assert.False(result.IsFresh);
        Assert.Equal(timestamp, result.CaptureTimestampUtc);
        Assert.Same(result, context.Results.GetRaw("capture"));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyFrameReturnsExecutedResultWithoutImage()
    {
        var result = Assert.IsType<DesktopDuplicationResult>(await new DesktopDuplicationStepHandler().ExecuteAsync(
            new DesktopDuplicationStep(), new PipelineContextStub { DesktopCaptureService = new RecordingCaptureService(CaptureFrame.Default) }, default));
        Assert.True(result.WasExecuted);
        Assert.False(result.HasImage);
    }

    [Fact]
    public async Task ExecuteAsync_CaptureFailureAndCancellationPropagateWithoutStoredResult()
    {
        var context = new PipelineContextStub { DesktopCaptureService = new RecordingCaptureService(null,
            new OperationCanceledException()) };
        var step = new DesktopDuplicationStep { Id = "capture" };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DesktopDuplicationStepHandler().ExecuteAsync(step, context, default));
        Assert.Null(context.Results.GetRaw("capture"));
    }

    private sealed class RecordingCaptureService(CaptureFrame? frame, Exception? error = null) : IDesktopCaptureService
    {
        public List<(int Monitor, bool Cursor)> Calls { get; } = [];
        public Task<CaptureFrame> CaptureAsync(int monitorIdx, CancellationToken ct, bool captureCursor = false)
        { Calls.Add((monitorIdx, captureCursor)); return error is null ? Task.FromResult(frame!) : Task.FromException<CaptureFrame>(error); }
        public void Dispose() { }
    }
}
