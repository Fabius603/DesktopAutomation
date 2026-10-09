using System.Drawing;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class OcrStepHandler(IOcrService ocrService) : JobStepHandler<OcrStep, OcrResult>
{
    protected override async Task<OcrResult> ExecuteCoreAsync(
        OcrStep step,
        IStepPipelineContext context,
        CancellationToken cancellationToken)
    {
        var input = ResultBindingResolver.ResolveCapture(context.Results, step.Settings.ImageSource);
        var capture = input.Capture;
        var captureResult = new OcrResult
        {
            SourceCaptureIsFresh = capture.IsFresh,
            SourceCaptureTimestampUtc = capture.CaptureTimestampUtc,
            SourceFrameVersion = capture.FrameVersion,
            SourceFrameTimestamp = capture.FrameTimestamp
        };
        if (input.Image is null)
            return captureResult with { WasExecuted = true };

        var dynamicRoi = DynamicRoiResolver.Resolve(
            step.Settings.DynamicRoiSource,
            capture,
            context,
            step.Settings.EnableROI ? step.Settings.ROI : null);
        var localRoi = dynamicRoi ?? (step.Settings.EnableROI ? step.Settings.ROI : null);
        localRoi = ClipToImage(localRoi, input.Image);

        if (localRoi is { IsEmpty: true })
            return captureResult with { WasExecuted = true, AppliedRoi = localRoi, UsedDynamicRoi = dynamicRoi.HasValue };

        using var source = Crop(input.Image, localRoi);
        var recognition = await ocrService.RecognizeAsync(source,
            new OcrRecognitionOptions(step.Settings.Languages, step.Settings.PageLayout), cancellationToken)
            .ConfigureAwait(false);

        var roiOffset = localRoi is { } roi ? new PixelPoint(roi.X, roi.Y) : default;
        PixelRegion ToGlobal(PixelRegion region) => new(
            region.X + roiOffset.X + capture.Offset.X,
            region.Y + roiOffset.Y + capture.Offset.Y,
            region.Width,
            region.Height);

        var lines = recognition.Lines
            .Select((line, index) => new OcrLineResult
            {
                Text = line.Text,
                Confidence = line.Confidence,
                BoundingBox = ToGlobal(line.BoundingBox),
                Position = index
            })
            .ToArray();
        var words = recognition.Words
            .Select((word, index) => new OcrWordResult
            {
                Text = word.Text,
                Confidence = word.Confidence,
                BoundingBox = ToGlobal(word.BoundingBox),
                Position = index,
                LinePosition = FindLinePosition(word.BoundingBox, recognition.Lines)
            })
            .ToArray();
        var boundingBox = Union(words.Select(word => word.BoundingBox));

        return captureResult with
        {
            WasExecuted = true,
            Found = words.Length > 0,
            Text = recognition.Text,
            Confidence = recognition.Confidence,
            MinimumConfidence = words.Length == 0 ? 0 : words.Min(word => word.Confidence),
            BoundingBox = boundingBox,
            LineCount = lines.Length,
            WordCount = words.Length,
            Lines = lines,
            Words = words,
            AppliedRoi = localRoi is { } applied ? new PixelRegion(
                applied.X + capture.Offset.X, applied.Y + capture.Offset.Y, applied.Width, applied.Height) : null,
            UsedDynamicRoi = dynamicRoi.HasValue,
            SourceCaptureIsFresh = capture.IsFresh,
            SourceCaptureTimestampUtc = capture.CaptureTimestampUtc
        };
    }

    protected override OcrResult CreateDefault() => OcrResult.Default;

    private static PixelRegion? ClipToImage(PixelRegion? region, Bitmap image)
    {
        if (region is not { } value) return null;
        var clipped = value.Intersect(new PixelRegion(0, 0, image.Width, image.Height));
        return clipped;
    }

    private static Bitmap Crop(Bitmap source, PixelRegion? region)
    {
        if (region is not { } roi) return (Bitmap)source.Clone();
        return source.Clone(new Rectangle(roi.X, roi.Y, roi.Width, roi.Height), source.PixelFormat);
    }

    private static PixelRegion? Union(IEnumerable<PixelRegion> regions)
    {
        var values = regions.ToArray();
        if (values.Length == 0) return null;
        var left = values.Min(region => region.Left);
        var top = values.Min(region => region.Top);
        var right = values.Max(region => region.Right);
        var bottom = values.Max(region => region.Bottom);
        return new PixelRegion(left, top, right - left, bottom - top);
    }

    private static int FindLinePosition(PixelRegion word, IReadOnlyList<OcrRecognitionLine> lines)
    {
        for (var index = 0; index < lines.Count; index++)
            if (Contains(lines[index].BoundingBox, word)) return index;
        return -1;
    }

    private static bool Contains(PixelRegion outer, PixelRegion inner) =>
        inner.Left >= outer.Left && inner.Top >= outer.Top
        && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;
}
