using ImageCapture.DesktopDuplication;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using ImageCapture.DesktopDuplication.RecordingIndicator;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;


namespace TaskAutomation.Steps
{
    public class DesktopDuplicationStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;

            if (step is not DesktopDuplicationStep ddStep)
            {
                var errorMessage = $"Invalid step type - expected DesktopDuplicationStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("DesktopDuplicationStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("DesktopDuplicationStepHandler: Processing desktop duplication for monitor index {MonitorIndex}", ddStep.Settings.DesktopIdx);

            try
            {
                // Dispose previous resources to prevent memory leaks
                executor.CurrentImage?.Dispose();
                executor.CurrentImage = null;
                executor.CurrentDesktopFrame?.Dispose();
                executor.CurrentDesktopFrame = null;

                if (executor.DesktopDuplicator == null)
                {
                    logger.LogDebug("DesktopDuplicationStepHandler: Creating new DesktopDuplicator for monitor index {MonitorIndex}", ddStep.Settings.DesktopIdx);
                    executor.DesktopDuplicator = new DesktopDuplicator(ddStep.Settings.DesktopIdx);
                    
                    // Give the duplicator a moment to fully initialize
                    await Task.Delay(100, ct);
                    logger.LogDebug("DesktopDuplicationStepHandler: DesktopDuplicator created and initialized");
                }

                ct.ThrowIfCancellationRequested();

                logger.LogInformation("DesktopDuplicationStepHandler: Capturing desktop frame from monitor {MonitorIndex}", ddStep.Settings.DesktopIdx);
                
                // Try to get frame with retry logic for initialization issues
                DesktopFrame frame = null;
                int retryCount = 0;
                int maxRetries = 3;
                
                while (frame?.DesktopImage == null && retryCount < maxRetries)
                {
                    try
                    {
                        frame?.Dispose(); // Clean up previous attempt
                        frame = executor.DesktopDuplicator.GetLatestFrame();
                        
                        if (frame?.DesktopImage == null)
                        {
                            retryCount++;
                            if (retryCount < maxRetries)
                            {
                                logger.LogWarning("DesktopDuplicationStepHandler: No desktop image captured on attempt {Attempt}/{MaxRetries}, retrying...", retryCount, maxRetries);
                                await Task.Delay(50, ct);
                            }
                        }
                    }
                    catch (Exception ex) when (retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        logger.LogWarning(ex, "DesktopDuplicationStepHandler: Frame capture failed on attempt {Attempt}/{MaxRetries}, retrying...", retryCount, maxRetries);
                        await Task.Delay(100, ct);
                    }
                }
                
                executor.CurrentDesktopFrame = frame;

                // Clone the bitmap to avoid sharing references
                if (executor.CurrentDesktopFrame?.DesktopImage != null)
                {
                    executor.CurrentImage = new Bitmap(executor.CurrentDesktopFrame.DesktopImage);

                    // Set the current offset to the screen bounds for this desktop index
                    var screenBounds = ImageHelperMethods.ScreenHelper.GetDesktopBounds(ddStep.Settings.DesktopIdx);
                    executor.CurrentOffset = new OpenCvSharp.Point(screenBounds.Left, screenBounds.Top);

                    logger.LogInformation("DesktopDuplicationStepHandler: Successfully captured desktop frame ({Width}x{Height}) at offset ({X}, {Y})",
                        executor.CurrentImage.Width, executor.CurrentImage.Height, executor.CurrentOffset.X, executor.CurrentOffset.Y);
                }
                else
                {
                    var errorMessage = "No desktop image captured";
                    logger.LogWarning("DesktopDuplicationStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("DesktopDuplicationStepHandler: Desktop duplication was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DesktopDuplicationStepHandler: Failed to capture desktop: {ErrorMessage}", ex.Message);

                // Clean up on error
                executor.CurrentImage?.Dispose();
                executor.CurrentImage = null;
                executor.CurrentDesktopFrame?.Dispose();
                executor.CurrentDesktopFrame = null;

                throw; // Re-throw all other exceptions
            }
        }
    }
}
