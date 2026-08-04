using ImageDetection.Algorithms.TemplateMatching;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Geometry;

namespace TaskAutomation.Steps
{
    public sealed class TemplateMatchingStepHandler : JobStepHandler<TemplateMatchingStep, TemplateMatchingResult>
    {
        protected override async Task<TemplateMatchingResult> ExecuteCoreAsync(
            TemplateMatchingStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("TemplateMatchingStepHandler: Matching '{TemplatePath}'", step.Settings.TemplatePath);

            if (string.IsNullOrWhiteSpace(step.Settings.TemplatePath))
                throw new InvalidOperationException("No template path specified");

            if (!File.Exists(step.Settings.TemplatePath))
                throw new FileNotFoundException($"Template file not found: '{step.Settings.TemplatePath}'");

            var input = ResultBindingResolver.ResolveCapture(ctx.Results, step.Settings.ImageSource);
            var capture = input.Capture;
            if (input.Image is null)
            {
                logger.LogInformation("TemplateMatchingStepHandler: Kein Bild verfuegbar, Step wird uebersprungen");
                return new TemplateMatchingResult { WasExecuted = true, Found = false };
            }

            if (ctx.TemplateMatcher == null)
                ctx.TemplateMatcher = new TemplateMatching(step.Settings.TemplateMatchMode);

            var dynamicRoi = DynamicRoiResolver.Resolve(
                step.Settings.DynamicRoiSource,
                capture,
                ctx,
                step.Settings.EnableROI ? step.Settings.ROI : null);
            ctx.TemplateMatcher.SetROI((dynamicRoi ?? step.Settings.ROI).ToOpenCvRect());
            if (dynamicRoi.HasValue || step.Settings.EnableROI) ctx.TemplateMatcher.EnableROI();
            else                         ctx.TemplateMatcher.DisableROI();
            ctx.TemplateMatcher.EnableMultiplePoints();
            ctx.TemplateMatcher.SetTemplate(step.Settings.TemplatePath);
            ctx.TemplateMatcher.SetThreshold(step.Settings.ConfidenceThreshold);

            var rawResult = ctx.TemplateMatcher.Detect(input.Image);

            if (!rawResult.Success)
            {
                logger.LogInformation("TemplateMatchingStepHandler: No match found above threshold");
                return new TemplateMatchingResult { WasExecuted = true, Found = false, AppliedRoi = dynamicRoi, UsedDynamicRoi = dynamicRoi.HasValue };
            }

            var globalPoint = new PixelPoint(
                rawResult.CenterPoint.X + capture.Offset.X,
                rawResult.CenterPoint.Y + capture.Offset.Y);

            logger.LogInformation(
                "TemplateMatchingStepHandler: Found at ({X},{Y}) confidence {C:F3}",
                globalPoint.X, globalPoint.Y, rawResult.Confidence);

            PixelRegion? globalBoundingBox = null;
            if (rawResult.BoundingBox.HasValue)
            {
                var b = rawResult.BoundingBox.Value;
                globalBoundingBox = new PixelRegion(
                    b.X + capture.Offset.X,
                    b.Y + capture.Offset.Y,
                    b.Width,
                    b.Height);
            }

            var allDetections = rawResult.AllResults
                .Select(r =>
                {
                    var c = new PixelPoint(
                        r.CenterPoint.X + capture.Offset.X,
                        r.CenterPoint.Y + capture.Offset.Y);
                    PixelRegion? bb = r.BoundingBox.HasValue
                        ? new PixelRegion(
                            r.BoundingBox.Value.X + capture.Offset.X,
                            r.BoundingBox.Value.Y + capture.Offset.Y,
                            r.BoundingBox.Value.Width,
                            r.BoundingBox.Value.Height)
                        : null;
                    return new DetectionItem { Center = c, BoundingBox = bb, Confidence = rawResult.Confidence };
                })
                .ToList();

            if (allDetections.Count == 0)
                allDetections.Add(new DetectionItem { Center = globalPoint, BoundingBox = globalBoundingBox, Confidence = rawResult.Confidence });

            return new TemplateMatchingResult
            {
                WasExecuted   = true,
                Found         = true,
                Point         = globalPoint,
                BoundingBox   = globalBoundingBox,
                Confidence    = rawResult.Confidence,
                SourceCaptureIsFresh = capture.IsFresh,
                SourceCaptureTimestampUtc = capture.CaptureTimestampUtc,
                AllDetections = allDetections
                ,AppliedRoi = dynamicRoi, UsedDynamicRoi = dynamicRoi.HasValue
            };
        }

        protected override TemplateMatchingResult CreateDefault() => TemplateMatchingResult.Default;
    }
}
