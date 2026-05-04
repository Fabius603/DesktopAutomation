using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
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
    public sealed class TemplateMatchingStepHandler : JobStepHandler<TemplateMatchingStep, DetectionResult>
    {
        protected override async Task<DetectionResult> ExecuteCoreAsync(
            TemplateMatchingStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("TemplateMatchingStepHandler: Matching '{TemplatePath}'", step.Settings.TemplatePath);

            if (string.IsNullOrWhiteSpace(step.Settings.TemplatePath))
                throw new InvalidOperationException("No template path specified");

            if (!File.Exists(step.Settings.TemplatePath))
                throw new FileNotFoundException($"Template file not found: '{step.Settings.TemplatePath}'");

            var capture = ctx.Results.GetById<CaptureResult>(step.Settings.SourceCaptureStepId);
            if (!capture.HasImage)
            {
                logger.LogInformation("TemplateMatchingStepHandler: Kein Bild verfügbar, Step wird übersprungen");
                return new DetectionResult { WasExecuted = true, Found = false };
            }

            if (ctx.TemplateMatcher == null)
                ctx.TemplateMatcher = new TemplateMatching(step.Settings.TemplateMatchMode);

            ctx.TemplateMatcher.SetROI(step.Settings.ROI);
            if (step.Settings.EnableROI) ctx.TemplateMatcher.EnableROI();
            else                         ctx.TemplateMatcher.DisableROI();
            ctx.TemplateMatcher.DisableMultiplePoints();
            ctx.TemplateMatcher.SetTemplate(step.Settings.TemplatePath);
            ctx.TemplateMatcher.SetThreshold(step.Settings.ConfidenceThreshold);

            // capture.Image direkt übergeben – Detect() liest es nur (kein Clone vor der Erkennung nötig).
            var rawResult = ctx.TemplateMatcher.Detect(capture.Image!);

            if (!rawResult.Success)
            {
                logger.LogInformation("TemplateMatchingStepHandler: No match found above threshold");
                return new DetectionResult
                {
                    WasExecuted    = true,
                    Found          = false,
                    ProcessedImage = null   // ShowImageStep / VideoCreationStep fallen auf CaptureResult.Image zurück
                };
            }

            var globalPoint = ScreenHelper.ConvertResultToGlobalDesktopCoordinates(
                    rawResult.CenterPoint,
                    capture.Offset);

            logger.LogInformation(
                "TemplateMatchingStepHandler: Found at ({X},{Y}) confidence {C:F3}",
                globalPoint.X, globalPoint.Y, rawResult.Confidence);

            Bitmap? processedImg = null;
            if (step.Settings.DrawResults)
            {
                // Nur für Annotation klonen – spart ~8 MB Memcopy wenn DrawResults=false
                var clone = (Bitmap)capture.Image!.Clone();
                processedImg = DrawResult.DrawDetectionResult(clone, rawResult);
                if (!ReferenceEquals(processedImg, clone))
                    clone.Dispose();
            }

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
                ProcessedImage = processedImg,
                AllDetections  = allDetections
            };
        }

        protected override DetectionResult CreateDefault() => DetectionResult.Default;
    }
}

