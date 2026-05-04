using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
