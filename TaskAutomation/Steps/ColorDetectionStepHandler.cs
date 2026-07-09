using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageDetection.Algorithms.ColorDetection;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public sealed class ColorDetectionStepHandler : JobStepHandler<ColorDetectionStep, DetectionResult>
    {
        protected override async Task<DetectionResult> ExecuteCoreAsync(
            ColorDetectionStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            await Task.Yield();

            var logger = ctx.Logger;
            var capture = ctx.Results.GetById<CaptureResult>(step.Settings.SourceCaptureStepId);
            if (!capture.HasImage)
            {
                logger.LogInformation("ColorDetectionStepHandler: Kein Bild verfuegbar, Step wird uebersprungen.");
                return new DetectionResult { WasExecuted = true, Found = false };
            }

            var detector = ctx.ColorDetector ??= new ColorDetector();
            var rawResult = detector.Detect(
                capture.Image!,
                CreateOptions(step.Settings),
                ct);

            if (!rawResult.Success)
            {
                logger.LogInformation(
                    "ColorDetectionStepHandler: Kein Treffer ueber Threshold {Threshold:F2} gefunden.",
                    step.Settings.ConfidenceThreshold);
                return new DetectionResult { WasExecuted = true, Found = false };
            }

            var globalPoint = new System.Drawing.Point(
                rawResult.CenterPoint.X + capture.Offset.X,
                rawResult.CenterPoint.Y + capture.Offset.Y);

            System.Drawing.Rectangle? globalBoundingBox = null;
            if (rawResult.BoundingBox.HasValue)
            {
                var b = rawResult.BoundingBox.Value;
                globalBoundingBox = new System.Drawing.Rectangle(
                    b.X + capture.Offset.X,
                    b.Y + capture.Offset.Y,
                    b.Width,
                    b.Height);
            }

            var allDetections = rawResult.AllResults
                .Select(r =>
                {
                    var center = new System.Drawing.Point(
                        r.CenterPoint.X + capture.Offset.X,
                        r.CenterPoint.Y + capture.Offset.Y);
                    System.Drawing.Rectangle? box = r.BoundingBox.HasValue
                        ? new System.Drawing.Rectangle(
                            r.BoundingBox.Value.X + capture.Offset.X,
                            r.BoundingBox.Value.Y + capture.Offset.Y,
                            r.BoundingBox.Value.Width,
                            r.BoundingBox.Value.Height)
                        : null;
                    return (Center: center, BoundingBox: box);
                })
                .ToList<(System.Drawing.Point Center, System.Drawing.Rectangle? BoundingBox)>();

            if (allDetections.Count == 0)
                allDetections.Add((globalPoint, globalBoundingBox));

            logger.LogInformation(
                "ColorDetectionStepHandler: Treffer bei ({X},{Y}), Confidence {Confidence:F3}.",
                globalPoint.X, globalPoint.Y, rawResult.Confidence);

            return new DetectionResult
            {
                WasExecuted = true,
                Found = true,
                Point = globalPoint,
                BoundingBox = globalBoundingBox,
                Confidence = rawResult.Confidence,
                SourceCaptureIsFresh = capture.IsFresh,
                SourceCaptureTimestampUtc = capture.CaptureTimestampUtc,
                AllDetections = allDetections
            };
        }

        protected override DetectionResult CreateDefault() => DetectionResult.Default;

        private static ColorDetectionOptions CreateOptions(ColorDetectionSettings settings)
            => new()
            {
                ColorHex = settings.ColorHex,
                ConfidenceThreshold = settings.ConfidenceThreshold,
                MinSize = settings.MinSize,
                MaxSize = settings.MaxSize,
                MinWidth = settings.MinWidth,
                MinHeight = settings.MinHeight,
                DownscaleFactor = settings.DownscaleFactor,
                EnableROI = settings.EnableROI,
                ROI = new Rect(
                    settings.ROI.X,
                    settings.ROI.Y,
                    settings.ROI.Width,
                    settings.ROI.Height)
            };
    }
}
