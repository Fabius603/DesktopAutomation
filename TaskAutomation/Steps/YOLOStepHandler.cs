using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;
using ImageHelperMethods;
using ImageDetection;

namespace TaskAutomation.Steps
{
    public sealed class YOLOStepHandler : JobStepHandler<YOLODetectionStep, DetectionResult>
    {
        protected override async Task<DetectionResult> ExecuteCoreAsync(
            YOLODetectionStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("YOLOStepHandler: Detecting '{ClassName}' with model '{Model}'",
                step.Settings.ClassName, step.Settings.Model);

            if (string.IsNullOrWhiteSpace(step.Settings.Model))
                throw new InvalidOperationException("No YOLO model specified");

            if (string.IsNullOrWhiteSpace(step.Settings.ClassName))
                throw new InvalidOperationException("No class name specified for YOLO detection");

            if (ctx.YoloManager == null)
                throw new InvalidOperationException("YoloManager not available");

            var capture = ctx.Results.GetById<CaptureResult>(step.Settings.SourceCaptureStepId);
            if (!capture.HasImage)
            {
                logger.LogInformation("YOLOStepHandler: Kein Bild verfügbar, Step wird übersprungen");
                return new DetectionResult { WasExecuted = true, Found = false };
            }

            await ctx.YoloManager.EnsureModelAsync(step.Settings.Model, ct);

            System.Drawing.Rectangle? roi = null;
            if (step.Settings.EnableROI && step.Settings.ROI.Width > 0 && step.Settings.ROI.Height > 0)
                roi = new System.Drawing.Rectangle(
                    step.Settings.ROI.X, step.Settings.ROI.Y,
                    step.Settings.ROI.Width, step.Settings.ROI.Height);

            var rawResult = await ctx.YoloManager.DetectAsync(
                step.Settings.Model, step.Settings.ClassName, capture.Image!,
                step.Settings.ConfidenceThreshold, roi, ct);

            if (rawResult?.Success != true)
            {
                logger.LogInformation(
                    "YOLOStepHandler: No '{ClassName}' found above threshold {T}",
                    step.Settings.ClassName, step.Settings.ConfidenceThreshold);
                return new DetectionResult
                {
                    WasExecuted    = true,
                    Found          = false,
                    ProcessedImage = (Bitmap)capture.Image!.Clone()
                };
            }

            var globalPoint = ScreenHelper.ConvertResultToGlobalDesktopCoordinates(
                    rawResult.CenterPoint,
                    capture.Offset);

            logger.LogInformation(
                "YOLOStepHandler: Found at ({X},{Y}) confidence {C:F3}",
                globalPoint.X, globalPoint.Y, rawResult.Confidence);

            Bitmap processedImg;
            if (step.Settings.DrawResults)
            {
                var imageToProcess = (Bitmap)capture.Image!.Clone();
                processedImg = DrawResult.DrawDetectionResult(imageToProcess, rawResult);
                if (!ReferenceEquals(processedImg, imageToProcess))
                    imageToProcess.Dispose();
            }
            else
            {
                processedImg = (Bitmap)capture.Image!.Clone();
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
    }
}
