using System.Drawing;
using TaskAutomation.Events;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class ShowImageStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_RawImageDisplaysOriginalAndTracksWindow()
    {
        using var bitmap = new Bitmap(20, 20);
        var display = new NoOpImageDisplayService();
        var context = Context(display, bitmap);
        var result = Assert.IsType<ShowImageResult>(await new ShowImageStepHandler().ExecuteAsync(Step(), context, default));
        var call = Assert.Single(display.DisplayCalls);
        Assert.Same(bitmap, call.Image);
        Assert.Equal(ImageDisplayType.Raw, call.DisplayType);
        Assert.Contains("preview", context.OpenedWindowNames);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_DetectionsDisplaysProcessedImage()
    {
        using var bitmap = new Bitmap(30, 30);
        var display = new NoOpImageDisplayService();
        var context = Context(display, bitmap);
        context.Results.Set<TemplateMatchingStep>(new TemplateMatchingResult { WasExecuted = true, Found = true,
            AllDetections = [new DetectionItem { Center = new Point(10, 10), BoundingBox = new Rectangle(5, 5, 10, 10), Confidence = .9 }] }, "detect");
        var step = Step();
        step.Settings.DetectionsSource = new() { SourceStepId = "detect", PropertyPath = "AllDetections" };
        var result = Assert.IsType<ShowImageResult>(await new ShowImageStepHandler().ExecuteAsync(step, context, default));
        var call = Assert.Single(display.DisplayCalls);
        Assert.NotSame(bitmap, call.Image);
        Assert.Equal(ImageDisplayType.Processed, call.DisplayType);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_MissingImageReturnsFailureWithoutOpeningWindow()
    {
        var display = new NoOpImageDisplayService();
        var context = new PipelineContextStub { ImageDisplayService = display };
        var result = Assert.IsType<ShowImageResult>(await new ShowImageStepHandler().ExecuteAsync(Step(), context, default));
        Assert.False(result.Success);
        Assert.Empty(display.DisplayCalls);
        Assert.Empty(context.OpenedWindowNames);
    }

    private static PipelineContextStub Context(NoOpImageDisplayService display, Bitmap bitmap)
    {
        var context = new PipelineContextStub { ImageDisplayService = display };
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult { WasExecuted = true, Image = bitmap,
            Bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height) }, "capture");
        return context;
    }
    private static ShowImageStep Step() => new() { Settings = new() { WindowName = "preview",
        ImageSource = new() { SourceStepId = "capture", PropertyPath = "Image" } } };
}
