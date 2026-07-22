using System.Drawing;
using ImageDetection.Model;
using Rect = OpenCvSharp.Rect;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class YOLOStepHandlerTests
{
    [Theory]
    [InlineData("", "person")]
    [InlineData("model", "")]
    [InlineData(" ", "person")]
    [InlineData("model", " ")]
    public async Task ExecuteAsync_MissingConfigurationThrows(string model, string className)
    {
        var step = Step(model, className);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new YOLOStepHandler().ExecuteAsync(step, new PipelineContextStub(), default));
    }

    [Fact]
    public async Task ExecuteAsync_MissingImageSkipsModelAndDetection()
    {
        var manager = new RecordingYoloManager();
        var result = Assert.IsType<YOLODetectionResult>(await new YOLOStepHandler().ExecuteAsync(
            Step(), new PipelineContextStub { YoloManager = manager }, default));
        Assert.True(result.WasExecuted);
        Assert.False(result.Found);
        Assert.Empty(manager.EnsureCalls);
        Assert.Empty(manager.DetectCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsConfigurationAndStaticRoi()
    {
        using var bitmap = new Bitmap(100, 80);
        var manager = new RecordingYoloManager { DetectionResult = new DetectionResult() };
        var context = Context(manager, bitmap);
        var step = Step();
        step.Settings.ConfidenceThreshold = .73f;
        step.Settings.EnableROI = true;
        step.Settings.ROI = new Rect(4, 5, 30, 20);
        var result = Assert.IsType<YOLODetectionResult>(await new YOLOStepHandler().ExecuteAsync(step, context, default));
        Assert.False(result.Found);
        Assert.Equal("model", Assert.Single(manager.EnsureCalls).Model);
        var call = Assert.Single(manager.DetectCalls);
        Assert.Equal("person", call.ClassName);
        Assert.Same(bitmap, call.Image);
        Assert.Equal(.73f, call.Threshold);
        Assert.Equal(new Rectangle(4, 5, 30, 20), call.Roi);
    }

    [Fact]
    public async Task ExecuteAsync_MapsSuccessfulDetectionToGlobalCoordinatesAndMetadata()
    {
        using var bitmap = new Bitmap(100, 80);
        var capturedAt = new DateTime(2026, 7, 22, 10, 11, 12, DateTimeKind.Utc);
        var secondary = new DetectionResult { Success = true, CenterPoint = new Point(7, 8),
            BoundingBox = new Rectangle(2, 3, 10, 11), Confidence = .66f };
        var raw = new DetectionResult { Success = true, CenterPoint = new Point(20, 30),
            BoundingBox = new Rectangle(10, 15, 20, 25), Confidence = .91f, AllResults = [secondary] };
        var manager = new RecordingYoloManager { DetectionResult = raw };
        var context = Context(manager, bitmap, new Point(100, 200), capturedAt, false);
        var result = Assert.IsType<YOLODetectionResult>(await new YOLOStepHandler().ExecuteAsync(Step(), context, default));
        Assert.True(result.Found);
        Assert.Equal(new Point(120, 230), result.Point);
        Assert.Equal(new Rectangle(110, 215, 20, 25), result.BoundingBox);
        Assert.Equal(.91, result.Confidence, 3);
        Assert.False(result.SourceCaptureIsFresh);
        Assert.Equal(capturedAt, result.SourceCaptureTimestampUtc);
        var item = Assert.Single(result.AllDetections);
        Assert.Equal(new Point(107, 208), item.Center);
        Assert.Equal(new Rectangle(102, 203, 10, 11), item.BoundingBox);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesCancellationFromModelInitialization()
    {
        using var bitmap = new Bitmap(10, 10);
        var manager = new RecordingYoloManager
        {
            EnsureAction = ct => Task.FromCanceled(ct)
        };
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new YOLOStepHandler().ExecuteAsync(Step(), Context(manager, bitmap), source.Token));
        Assert.Empty(manager.DetectCalls);
    }

    private static YOLODetectionStep Step(string model = "model", string className = "person") => new()
    {
        Settings = new()
        {
            Model = model,
            ClassName = className,
            ImageSource = new() { SourceStepId = "capture", PropertyPath = "Image" }
        }
    };

    private static PipelineContextStub Context(RecordingYoloManager manager, Bitmap bitmap,
        Point? offset = null, DateTime? capturedAt = null, bool fresh = true)
    {
        var context = new PipelineContextStub { YoloManager = manager };
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult
        {
            WasExecuted = true,
            Image = bitmap,
            Bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            Offset = offset ?? Point.Empty,
            CaptureTimestampUtc = capturedAt ?? DateTime.UtcNow,
            IsFresh = fresh
        }, "capture");
        return context;
    }
}
