using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;
using ImageHelperMethods;
using ImageDetection;
using OpenCvSharp.Extensions;
using OpenCvSharp;

namespace TaskAutomation.Steps
{
    public class YOLOStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;
            
            if (step is not YOLODetectionStep yoloStep)
            {
                var errorMessage = $"Invalid step type - expected YOLODetectionStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("YOLOStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("YOLOStepHandler: Processing YOLO detection with model '{Model}' for class '{ClassName}'", 
                yoloStep.Settings.Model, yoloStep.Settings.ClassName);

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(yoloStep.Settings.Model))
                {
                    var errorMessage = "No YOLO model specified";
                    logger.LogWarning("YOLOStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                if (string.IsNullOrWhiteSpace(yoloStep.Settings.ClassName))
                {
                    var errorMessage = "No class name specified for YOLO detection";
                    logger.LogWarning("YOLOStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                if (executor.CurrentImage == null)
                {
                    var errorMessage = "No current image available for YOLO detection";
                    logger.LogWarning("YOLOStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                if (executor.YoloManager == null)
                {
                    var errorMessage = "YoloManager not available in executor";
                    logger.LogError("YOLOStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                // Ensure model is available/downloaded
                await executor.YoloManager.EnsureModelAsync(yoloStep.Settings.Model, ct);
                logger.LogDebug("YOLOStepHandler: Model '{Model}' ensured/loaded", yoloStep.Settings.Model);

                // Prepare ROI if enabled
                System.Drawing.Rectangle? roi = null;
                if (yoloStep.Settings.EnableROI && yoloStep.Settings.ROI.Width > 0 && yoloStep.Settings.ROI.Height > 0)
                {
                    roi = new System.Drawing.Rectangle(
                        yoloStep.Settings.ROI.X,
                        yoloStep.Settings.ROI.Y,
                        yoloStep.Settings.ROI.Width,
                        yoloStep.Settings.ROI.Height);
                    logger.LogDebug("YOLOStepHandler: ROI enabled with bounds {ROI}", roi);
                }

                logger.LogInformation("YOLOStepHandler: Starting YOLO detection with model '{Model}', class '{ClassName}', threshold {Threshold}", 
                    yoloStep.Settings.Model, yoloStep.Settings.ClassName, yoloStep.Settings.ConfidenceThreshold);

                // Perform YOLO detection
                executor.DetectionResult = await executor.YoloManager.DetectAsync(
                    yoloStep.Settings.Model,
                    yoloStep.Settings.ClassName,
                    executor.CurrentImage,
                    yoloStep.Settings.ConfidenceThreshold,
                    roi,
                    ct);

                if (executor.DetectionResult?.Success == true)
                {
                    // Convert to global desktop coordinates
                    executor.LatestCalculatedPoint = ClassConverter.ToCv(
                        ScreenHelper.ConvertResultToGlobalDesktopCoordinates(
                            executor.DetectionResult.CenterPoint,
                            ClassConverter.ToDrawing(executor.CurrentOffset)
                        )
                    );

                    logger.LogInformation("YOLOStepHandler: YOLO detection successful at point ({X}, {Y}) with confidence {Confidence:F3}", 
                        executor.LatestCalculatedPoint.Value.X, executor.LatestCalculatedPoint.Value.Y, 
                        executor.DetectionResult.Confidence);

                    // Draw results if enabled
                    if (yoloStep.Settings.DrawResults)
                    {
                        executor.ImageToProcess = executor.CurrentImage.ToMat();
                        executor.CurrentImageWithResult = DrawResult.DrawDetectionResult(
                            executor.ImageToProcess,
                            executor.DetectionResult);
                        logger.LogDebug("YOLOStepHandler: Results drawn on image");
                    }
                }
                else
                {
                    // If YOLO detection failed, reset the point
                    executor.LatestCalculatedPoint = null;
                    logger.LogInformation("YOLOStepHandler: YOLO detection failed - no object '{ClassName}' found above threshold {Threshold}", 
                        yoloStep.Settings.ClassName, yoloStep.Settings.ConfidenceThreshold);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("YOLOStepHandler: YOLO detection was cancelled");
                return false; // Return false for cancellation, don't treat as error
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "YOLOStepHandler: Failed to execute YOLO detection: {ErrorMessage}", ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
