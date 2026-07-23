using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class ShowOnDesktopStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ValidDetectionsDisplaysAllItems()
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        var detections = new[] { new DetectionItem { Center = new(10, 20), Confidence = .9 },
            new DetectionItem { Center = new(30, 40), Confidence = .8 } };
        context.Results.Set<TemplateMatchingStep>(new TemplateMatchingResult
            { WasExecuted = true, Found = true, AllDetections = detections }, "source");
        var result = Assert.IsType<ShowOnDesktopResult>(await new ShowOnDesktopStepHandler().ExecuteAsync(
            Step("source", "AllDetections"), context, default));
        Assert.Equal(detections, Assert.Single(overlay.ResultCalls));
        Assert.Equal(0, overlay.ClearCalls);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePointResultIsConvertedToDetection()
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        context.Results.Set<TemplateMatchingStep>(new TemplateMatchingResult
            { WasExecuted = true, Found = true, Point = new Point(5, 6), Confidence = .75 }, "source");
        await new ShowOnDesktopStepHandler().ExecuteAsync(Step("source", "Point"), context, default);
        var item = Assert.Single(Assert.Single(overlay.ResultCalls));
        Assert.Equal(new Point(5, 6), item.Center);
        Assert.Equal(.75, item.Confidence);
    }

    [Fact]
    public async Task ExecuteAsync_BestBoundingBoxIsConvertedToDetection()
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        var bounds = new Rectangle(10, 20, 30, 40);
        context.Results.Set<YOLODetectionStep>(new YOLODetectionResult
        {
            WasExecuted = true,
            Found = true,
            BoundingBox = bounds,
            Confidence = .85
        }, "source");

        await new ShowOnDesktopStepHandler().ExecuteAsync(
            Step("source", "BoundingBox"), context, default);

        var item = Assert.Single(Assert.Single(overlay.ResultCalls));
        Assert.Equal(bounds, item.BoundingBox);
        Assert.Equal(new Point(25, 40), item.Center);
        Assert.Equal(.85, item.Confidence);
    }

    [Fact]
    public async Task ExecuteAsync_BoundingBoxCollectionDisplaysEveryRectangle()
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        var first = new Rectangle(10, 20, 30, 40);
        var second = new Rectangle(100, 200, 20, 10);
        context.Results.Set<YOLODetectionStep>(new YOLODetectionResult
        {
            WasExecuted = true,
            Found = true,
            AllDetections =
            [
                new DetectionItem { Center = new Point(25, 40), BoundingBox = first, Confidence = .9 },
                new DetectionItem { Center = new Point(110, 205), BoundingBox = second, Confidence = .8 }
            ]
        }, "source");

        await new ShowOnDesktopStepHandler().ExecuteAsync(
            Step("source", "AllDetections[].BoundingBox"), context, default);

        var items = Assert.Single(overlay.ResultCalls);
        Assert.Collection(items,
            item =>
            {
                Assert.Equal(first, item.BoundingBox);
                Assert.Equal(new Point(25, 40), item.Center);
                Assert.Equal(.9, item.Confidence);
            },
            item =>
            {
                Assert.Equal(second, item.BoundingBox);
                Assert.Equal(new Point(110, 205), item.Center);
                Assert.Equal(.8, item.Confidence);
            });
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("not-executed")]
    public async Task ExecuteAsync_UnavailableSourceClearsOverlayAndStillSucceeds(string scenario)
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        if (scenario == "not-executed") context.Results.Set<TemplateMatchingStep>(new TemplateMatchingResult(), "source");
        var result = Assert.IsType<ShowOnDesktopResult>(await new ShowOnDesktopStepHandler().ExecuteAsync(
            Step(scenario == "missing" ? "missing" : "source", "AllDetections"), context, default));
        Assert.Equal(1, overlay.ClearCalls);
        Assert.Empty(overlay.ResultCalls);
        Assert.True(result.Success);
    }

    private static ShowOnDesktopStep Step(string id, string path) => new() { Settings = new()
        { DetectionsSource = new() { SourceStepId = id, PropertyPath = path } } };
}
