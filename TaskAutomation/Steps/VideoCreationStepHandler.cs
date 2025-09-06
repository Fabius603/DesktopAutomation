using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using OpenCvSharp.Extensions;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public class VideoCreationStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;

            if (step is not VideoCreationStep vcStep)
            {
                var errorMessage = $"Invalid step type - expected VideoCreationStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("VideoCreationStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("VideoCreationStepHandler: Processing video creation step");

            try
            {
                ct.ThrowIfCancellationRequested();

                if (executor.VideoRecorder == null)
                {
                    var errorMessage = "VideoRecorder is not initialized";
                    logger.LogWarning("VideoCreationStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                if (!string.IsNullOrEmpty(vcStep.Settings.SavePath))
                {
                    executor.VideoRecorder.OutputDirectory = vcStep.Settings.SavePath;
                    logger.LogDebug("VideoCreationStepHandler: Set output directory to '{SavePath}'", vcStep.Settings.SavePath);
                }

                if (!string.IsNullOrEmpty(vcStep.Settings.FileName))
                {
                    executor.VideoRecorder.FileName = vcStep.Settings.FileName;
                    logger.LogDebug("VideoCreationStepHandler: Set filename to '{FileName}'", vcStep.Settings.FileName);
                }

                if (executor.CurrentImage == null || executor.CurrentImage.Width == 0 && executor.CurrentImage.Height == 0)
                {
                    var errorMessage = "No valid image available for video recording";
                    logger.LogWarning("VideoCreationStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                if (vcStep.Settings.UseRawImage)
                {
                    ct.ThrowIfCancellationRequested();
                    executor.VideoRecorder.AddFrame((Bitmap)executor.CurrentImage?.Clone());
                    logger.LogDebug("VideoCreationStepHandler: Added raw image frame to video");
                }
                else if (vcStep.Settings.UseProcessedImage)
                {
                    if (executor.CurrentImageWithResult != null && executor.CurrentImageWithResult.Width > 0 && executor.CurrentImageWithResult.Height > 0)
                    {
                        ct.ThrowIfCancellationRequested();
                        executor.VideoRecorder.AddFrame((Bitmap)executor.CurrentImageWithResult.Clone());
                        logger.LogDebug("VideoCreationStepHandler: Added processed image frame to video");
                    }
                    else
                    {
                        ct.ThrowIfCancellationRequested();
                        executor.VideoRecorder.AddFrame((Bitmap)executor.CurrentImage?.Clone());
                        logger.LogDebug("VideoCreationStepHandler: Added raw image frame to video (processed image not available)");
                    }
                }

                logger.LogInformation("VideoCreationStepHandler: Video frame added successfully");
                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("VideoCreationStepHandler: Video creation was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "VideoCreationStepHandler: Failed to add video frame: {ErrorMessage}", ex.Message);
                throw; // Re-throw all other exceptions
            }
            
        }
    }
}
