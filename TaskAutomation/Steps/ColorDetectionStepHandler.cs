using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageDetection.Algorithms.ColorDetection;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public sealed class ColorDetectionStepHandler : JobStepHandler<ColorDetectionStep, ColorDetectionResult>
    {
        protected override async Task<ColorDetectionResult> ExecuteCoreAsync(
            ColorDetectionStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            await Task.Yield();

            var logger = ctx.Logger;
            var input = ResultBindingResolver.ResolveCapture(ctx.Results, step.Settings.ImageSource);
            var capture = input.Capture;
            if (input.Image is null)
            {
                logger.LogInformation("ColorDetectionStepHandler: Kein Bild verfuegbar, Step wird uebersprungen.");
                return new ColorDetectionResult { WasExecuted = true, Found = false };
            }

            var detector = ctx.ColorDetector ??= new ColorDetector();
            var dynamicRoi = DynamicRoiResolver.Resolve(
                step.Settings.DynamicRoiSource,
                capture,
                ctx,
                step.Settings.EnableROI ? step.Settings.ROI : null);
            var rawResult = detector.Detect(
                input.Image,
                CreateOptions(step.Settings, dynamicRoi),
                ct);

            if (!rawResult.Success)
            {
                logger.LogInformation(
                    "ColorDetectionStepHandler: Kein Treffer ueber Threshold {Threshold:F2} gefunden.",
                    step.Settings.ConfidenceThreshold);
                return new ColorDetectionResult { WasExecuted = true, Found = false, AppliedRoi = dynamicRoi?.ToString(), UsedDynamicRoi = dynamicRoi.HasValue };
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
                    return new DetectionItem { Center = center, BoundingBox = box, Confidence = rawResult.Confidence };
                })
                .ToList();

            if (allDetections.Count == 0)
                allDetections.Add(new DetectionItem { Center = globalPoint, BoundingBox = globalBoundingBox, Confidence = rawResult.Confidence });

            logger.LogInformation(
                "ColorDetectionStepHandler: Treffer bei ({X},{Y}), Confidence {Confidence:F3}.",
                globalPoint.X, globalPoint.Y, rawResult.Confidence);

            return new ColorDetectionResult
            {
                WasExecuted = true,
                Found = true,
                Point = globalPoint,
                BoundingBox = globalBoundingBox,
                Confidence = rawResult.Confidence,
                SourceCaptureIsFresh = capture.IsFresh,
                SourceCaptureTimestampUtc = capture.CaptureTimestampUtc,
                AllDetections = allDetections
                ,AppliedRoi = dynamicRoi?.ToString(), UsedDynamicRoi = dynamicRoi.HasValue
            };
        }

        protected override ColorDetectionResult CreateDefault() => ColorDetectionResult.Default;

        private static ColorDetectionOptions CreateOptions(ColorDetectionSettings settings, Rect? dynamicRoi)
            => new()
            {
                ColorHex = settings.ColorHex,
                ConfidenceThreshold = settings.ConfidenceThreshold,
                MinSize = settings.MinSize,
                MaxSize = settings.MaxSize,
                MinWidth = settings.MinWidth,
                MinHeight = settings.MinHeight,
                DownscaleFactor = settings.DownscaleFactor,
                EnableROI = dynamicRoi.HasValue || settings.EnableROI,
                ROI = new Rect(
                    (dynamicRoi ?? settings.ROI).X,
                    (dynamicRoi ?? settings.ROI).Y,
                    (dynamicRoi ?? settings.ROI).Width,
                    (dynamicRoi ?? settings.ROI).Height)
            };
    }
}
