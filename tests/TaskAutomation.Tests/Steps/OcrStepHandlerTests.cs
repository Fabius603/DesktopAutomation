using System.Drawing;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class OcrStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_OffsetsWordCoordinatesFromRoiAndCapture()
    {
        using var bitmap = new Bitmap(20, 20);
        var timestamp = DateTime.UtcNow;
        var context = new PipelineContextStub();
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult
        {
            WasExecuted = true,
            Image = bitmap,
            Bounds = new PixelRegion(10, 20, 20, 20),
            Offset = new PixelPoint(10, 20),
            CaptureTimestampUtc = timestamp
        }, "capture");
        var service = new RecordingOcrService(new OcrRecognition(
            "Ready",
            .92,
            [new OcrRecognitionLine("Ready", .92, new PixelRegion(1, 1, 2, 1))],
            [new OcrRecognitionWord("Ready", .92, new PixelRegion(1, 1, 2, 1))]));
        var step = new OcrStep
        {
            Id = "ocr",
            Settings = new()
            {
                ImageSource = Binding("capture", "image", "Image"),
                EnableROI = true,
                ROI = new PixelRegion(4, 5, 8, 6),
                Languages = "eng",
                PageLayout = OcrPageLayout.TextLine
            }
        };

        var result = Assert.IsType<OcrResult>(await new OcrStepHandler(service).ExecuteAsync(step, context, default));

        Assert.Equal(new Size(8, 6), service.ImageSize);
        Assert.Equal("eng", service.Options!.Languages);
        Assert.Equal(OcrPageLayout.TextLine, service.Options.PageLayout);
        Assert.True(result.Found);
        Assert.Equal("Ready", result.Text);
        Assert.Equal(.92, result.Confidence);
        Assert.Equal(.92, result.MinimumConfidence);
        Assert.Equal(1, result.LineCount);
        Assert.Equal(1, result.WordCount);
        Assert.Equal(0, Assert.Single(result.Lines).Position);
        Assert.Equal(0, Assert.Single(result.Words).Position);
        Assert.Equal(0, Assert.Single(result.Words).LinePosition);
        Assert.Equal(new PixelRegion(15, 26, 2, 1), Assert.Single(result.Words).BoundingBox);
        Assert.Equal(new PixelRegion(15, 26, 2, 1), result.BoundingBox);
        Assert.Equal(new PixelRegion(14, 25, 8, 6), result.AppliedRoi);
        Assert.Equal(timestamp, result.SourceCaptureTimestampUtc);
        Assert.Same(result, context.Results.GetRaw(step.Id));
    }

    [Fact]
    public async Task RecognizeAsync_ReadsBundledEnglishModel()
    {
        using var bitmap = new Bitmap(440, 120);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var font = new Font("Arial", 48, FontStyle.Bold, GraphicsUnit.Pixel))
        {
            graphics.Clear(Color.White);
            graphics.DrawString("OCR 42", font, Brushes.Black, new PointF(12, 24));
        }
        using var service = new TesseractOcrService(NullLogger<TesseractOcrService>.Instance);

        var result = await service.RecognizeAsync(bitmap,
            new OcrRecognitionOptions("eng", OcrPageLayout.TextLine), default);

        Assert.Contains("OCR 42", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.Words);
        Assert.All(result.Words, word => Assert.False(word.BoundingBox.IsEmpty));
    }

    [Fact]
    public void ResultContract_ExposesUsableLineAndWordProperties()
    {
        var contract = Assert.IsType<ResultTypeDescriptor>(
            StepResultMetadata.GetResultType(nameof(OcrResult)));

        Assert.Contains(contract.Properties, property => property.Name == "Lines[].Text"
            && property.StableId == "lines.text" && property.Cardinality == ResultCardinality.Collection);
        Assert.Contains(contract.Properties, property => property.Name == "Words[].BoundingBox"
            && property.StableId == "words.bounding_box" && property.Cardinality == ResultCardinality.Collection);
        Assert.Contains(contract.Properties, property => property.Name == "Words[].LinePosition"
            && property.StableId == "words.line_position" && property.Cardinality == ResultCardinality.Collection);
        Assert.Contains(contract.Properties, property => property.Name == "MinimumConfidence"
            && property.StableId == "minimum_confidence");

        var words = Assert.Single(contract.Properties, property => property.Name == "Words[].Text");
        Assert.True(StepResultMetadata.TryReadValue(new OcrResult
        {
            Words = [new() { Text = "Save" }, new() { Text = "Cancel" }]
        }, words, out var values));
        Assert.Equal(["Save", "Cancel"], Assert.IsAssignableFrom<IEnumerable<object?>>(values).Cast<string>());
    }

    private static ResultBinding Binding(string stepId, string propertyId, string propertyPath) => new()
    {
        SourceStepId = stepId,
        PropertyId = propertyId,
        PropertyPath = propertyPath
    };

    private sealed class RecordingOcrService(OcrRecognition recognition) : IOcrService
    {
        public Size ImageSize { get; private set; }
        public OcrRecognitionOptions? Options { get; private set; }

        public Task<OcrRecognition> RecognizeAsync(
            Bitmap image,
            OcrRecognitionOptions options,
            CancellationToken cancellationToken)
        {
            ImageSize = image.Size;
            Options = options;
            return Task.FromResult(recognition);
        }

        public void Dispose() { }
    }
}
