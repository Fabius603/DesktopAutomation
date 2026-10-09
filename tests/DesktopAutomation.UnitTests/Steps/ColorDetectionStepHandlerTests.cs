using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class ColorDetectionStepHandlerTests
{
    [Fact]
    public async Task SeparateColorComponents_RetainDifferentConfidenceValues()
    {
        using var bitmap = new Bitmap(40, 20);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
            graphics.FillRectangle(Brushes.Red, 2, 2, 8, 8);
            using var nearRed = new SolidBrush(Color.FromArgb(230, 0, 0));
            graphics.FillRectangle(nearRed, 22, 2, 8, 8);
        }
        var context = new PipelineContextStub();
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult { WasExecuted = true, Image = bitmap }, "capture");
        var step = new ColorDetectionStep
        {
            Settings = new()
            {
                ColorHex = "#FF0000",
                ConfidenceThreshold = .8,
                ImageSource = ResultBinding.ForStepResult("capture", "image")
            }
        };
        var result = Assert.IsType<ColorDetectionResult>(await new ColorDetectionStepHandler().ExecuteAsync(step, context, default));
        Assert.Equal(2, result.AllDetections.Count);
        Assert.True(result.AllDetections[0].Confidence > result.AllDetections[1].Confidence);
        Assert.Equal(result.Confidence, result.AllDetections[0].Confidence);
        context.ColorDetector?.Dispose();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_PreservesCaptureIdentityForHitsAndMisses(bool matching)
    {
        using var bitmap = new Bitmap(40, 40);
        using (var graphics = Graphics.FromImage(bitmap))
            graphics.Clear(matching ? Color.Blue : Color.Red);
        var capturedAt = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        var context = new PipelineContextStub();
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult
        {
            WasExecuted = true,
            Image = bitmap,
            IsFresh = false,
            CaptureTimestampUtc = capturedAt,
            FrameVersion = 42,
            FrameTimestamp = 123456
        }, "capture");
        var step = new ColorDetectionStep
        {
            Settings = new()
            {
                ColorHex = "#0000FF",
                ImageSource = new ResultBinding { SourceStepId = "capture", PropertyPath = "Image" }
            }
        };

        var result = Assert.IsType<ColorDetectionResult>(
            await new ColorDetectionStepHandler().ExecuteAsync(step, context, default));

        Assert.Equal(matching, result.Found);
        Assert.False(result.SourceCaptureIsFresh);
        Assert.Equal(capturedAt, result.SourceCaptureTimestampUtc);
        Assert.Equal(42, result.SourceFrameVersion);
        Assert.Equal(123456, result.SourceFrameTimestamp);
    }
}
