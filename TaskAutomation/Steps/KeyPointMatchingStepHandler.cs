using ImageDetection.Algorithms.KeyPointMatching;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class KeyPointMatchingStepHandler : JobStepHandler<KeyPointMatchingStep, DetectionResult>
    {
        protected override async Task<DetectionResult> ExecuteCoreAsync(
            KeyPointMatchingStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("KeyPointMatchingStepHandler: Matching '{TemplatePath}'", step.Settings.TemplatePath);

            if (string.IsNullOrWhiteSpace(step.Settings.TemplatePath))
                throw new InvalidOperationException("Kein Template-Pfad angegeben.");

            if (!File.Exists(step.Settings.TemplatePath))
                throw new FileNotFoundException($"Template-Datei nicht gefunden: '{step.Settings.TemplatePath}'");

            var capture = ctx.Results.GetById<CaptureResult>(step.Settings.SourceCaptureStepId);
            if (!capture.HasImage)
            {
                logger.LogInformation("KeyPointMatchingStepHandler: Kein Bild verfügbar, Step wird übersprungen.");
                return new DetectionResult { WasExecuted = true, Found = false };
            }

            // Lazy-create / re-use the matcher (one per job context)
            if (ctx.KeyPointMatcher == null)
                ctx.KeyPointMatcher = new KeyPointMatcher(
                    step.Settings.MinMatchCount,
                    step.Settings.LowesRatioThreshold);
            else
                ctx.KeyPointMatcher.Configure(
                    step.Settings.MinMatchCount,
                    step.Settings.LowesRatioThreshold);

            ctx.KeyPointMatcher.SetROI(step.Settings.ROI);
            if (step.Settings.EnableROI) ctx.KeyPointMatcher.EnableROI();
            else                         ctx.KeyPointMatcher.DisableROI();

            ctx.KeyPointMatcher.SetTemplate(step.Settings.TemplatePath);

            using var sourceMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(capture.Image!);
            var rawResult = ctx.KeyPointMatcher.Detect(sourceMat);

            if (!rawResult.Success)
            {
                logger.LogInformation("KeyPointMatchingStepHandler: Keine ausreichend guten Matches gefunden.");
                return new DetectionResult { WasExecuted = true, Found = false };
            }

            var globalPoint = ScreenHelper.ConvertResultToGlobalDesktopCoordinates(
                rawResult.CenterPoint,
                capture.Offset);

            logger.LogInformation(
                "KeyPointMatchingStepHandler: Match bei ({X},{Y}), Confidence {C:F3}",
                globalPoint.X, globalPoint.Y, rawResult.Confidence);

            System.Drawing.Rectangle? globalBoundingBox = null;
            if (rawResult.BoundingBox.HasValue)
            {
                var b = rawResult.BoundingBox.Value;
                globalBoundingBox = new System.Drawing.Rectangle(
                    b.X + capture.Offset.X, b.Y + capture.Offset.Y, b.Width, b.Height);
            }

            var allDetections = rawResult.AllResults
                .Select(r =>
                {
                    var c = new System.Drawing.Point(r.CenterPoint.X + capture.Offset.X, r.CenterPoint.Y + capture.Offset.Y);
                    System.Drawing.Rectangle? bb = r.BoundingBox.HasValue
                        ? new System.Drawing.Rectangle(r.BoundingBox.Value.X + capture.Offset.X, r.BoundingBox.Value.Y + capture.Offset.Y, r.BoundingBox.Value.Width, r.BoundingBox.Value.Height)
                        : null;
                    return (Center: c, BoundingBox: bb);
                })
                .ToList<(System.Drawing.Point Center, System.Drawing.Rectangle? BoundingBox)>();

            if (allDetections.Count == 0)
                allDetections.Add((Center: globalPoint, BoundingBox: globalBoundingBox));

            return new DetectionResult
            {
                WasExecuted    = true,
                Found          = true,
                Point          = globalPoint,
                BoundingBox    = globalBoundingBox,
                Confidence     = rawResult.Confidence,
                SourceCaptureIsFresh = capture.IsFresh,
                SourceCaptureTimestampUtc = capture.CaptureTimestampUtc,
                AllDetections  = allDetections
            };
        }

        protected override DetectionResult CreateDefault() => DetectionResult.Default;
    }
}
