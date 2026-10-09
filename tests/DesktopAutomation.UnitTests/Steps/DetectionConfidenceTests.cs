using System.Drawing;
using ImageDetection.Model;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class DetectionConfidenceTests
{
    [Fact]
    public async Task Yolo_EachDetectionRetainsItsOwnConfidenceAndOffset()
    {
        using var image = new Bitmap(20, 20);
        var manager = new RecordingYoloManager
        {
            DetectionResult = new DetectionResult
            {
                Success = true,
                Confidence = .9f,
                CenterPoint = new Point(1, 2),
                AllResults = [new DetectionResult { Success = true, Confidence = .9f, CenterPoint = new Point(1, 2) },
                          new DetectionResult { Success = true, Confidence = .4f, CenterPoint = new Point(3, 4) }]
            }
        };
        var context = new PipelineContextStub { YoloManager = manager };
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult
        { WasExecuted = true, Image = image, Offset = new(10, 20) }, "capture");
        var step = new YOLODetectionStep
        {
            Settings = new()
            {
                Model = "model",
                ClassName = "object",
                ImageSource = ResultBinding.ForStepResult("capture", "image")
            }
        };
        var result = Assert.IsType<YOLODetectionResult>(await new YOLOStepHandler().ExecuteAsync(step, context, default));
        Assert.Equal(.9, result.AllDetections[0].Confidence, 6);
        Assert.Equal(.4, result.AllDetections[1].Confidence, 6);
        Assert.Equal(new TaskAutomation.Contracts.Geometry.PixelPoint(13, 24), result.AllDetections[1].Center);
    }
}
