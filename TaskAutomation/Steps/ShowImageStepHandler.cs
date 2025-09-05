using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using OpenCvSharp.Extensions;
using Microsoft.Extensions.Logging;
using TaskAutomation.Events;

namespace TaskAutomation.Steps
{
    public class ShowImageStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;
            
            if (step is not ShowImageStep siStep)
            {
                var errorMessage = $"Invalid step type - expected ShowImageStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("ShowImageStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("ShowImageStepHandler: Processing show image step with window name '{WindowName}'", siStep.Settings.WindowName);

            try
            {
                if (executor.CurrentImage == null)
                {
                    var errorMessage = "No current image available to display";
                    logger.LogWarning("ShowImageStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                if (siStep.Settings.ShowRawImage)
                {
                    string windowName = $"{siStep.Settings.WindowName} - Raw Image";
                    executor.ImageDisplayService.DisplayImage(windowName, executor.CurrentImage, ImageDisplayType.Raw);
                    logger.LogDebug("ShowImageStepHandler: Requested display of raw image in window '{WindowName}'", windowName);
                }
                
                if (siStep.Settings.ShowProcessedImage)
                {
                    string windowName = $"{siStep.Settings.WindowName} - Processed Image";
                    if (executor.CurrentImageWithResult != null &&
                        !executor.CurrentImageWithResult.IsDisposed &&
                        executor.CurrentImageWithResult.Height >= 10 &&
                        executor.CurrentImageWithResult.Width >= 10)
                    {
                        using var processedBitmap = executor.CurrentImageWithResult.ToBitmap();
                        executor.ImageDisplayService.DisplayImage(windowName, processedBitmap, ImageDisplayType.Processed);
                        logger.LogDebug("ShowImageStepHandler: Requested display of processed image in window '{WindowName}'", windowName);
                    }
                    else
                    {
                        if (executor.CurrentImage != null)
                        {
                            executor.ImageDisplayService.DisplayImage(windowName, executor.CurrentImage, ImageDisplayType.Raw);
                            logger.LogDebug("ShowImageStepHandler: Requested display of raw image as processed image in window '{WindowName}' (processed image not available)", windowName);
                        }
                    }
                }

                logger.LogInformation("ShowImageStepHandler: Images displayed successfully");
                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("ShowImageStepHandler: Show image was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ShowImageStepHandler: Failed to display images: {ErrorMessage}", ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
