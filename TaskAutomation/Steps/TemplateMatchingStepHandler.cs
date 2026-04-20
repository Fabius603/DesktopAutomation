using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
using System;
using System.Drawing;
using System.IO;
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

            var capture = GetCapture(ctx.Results);
            if (!capture.HasImage)
                throw new InvalidOperationException("No captured image available for template matching");

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

            return new DetectionResult
            {
                WasExecuted    = true,
                Found          = true,
                Point          = globalPoint,
                Confidence     = rawResult.Confidence,
                ProcessedImage = processedImg
            };
        }

        protected override DetectionResult CreateDefault() => DetectionResult.Default;

        internal static CaptureResult GetCapture(IJobResultStore results)
        {
            var r = results.Get<DesktopDuplicationStep, CaptureResult>();
            if (!r.WasExecuted) r = results.Get<ProcessDuplicationStep, CaptureResult>();
            return r;
        }
    }
}

