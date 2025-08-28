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

namespace TaskAutomation.Steps
{
    public class ShowImageStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;
            
            if (step is not ShowImageStep siStep)
            {
                logger.LogError("ShowImageStepHandler: Invalid step type - expected ShowImageStep, got {StepType}", step?.GetType().Name ?? "null");
                return false;
            }

            logger.LogDebug("ShowImageStepHandler: Processing show image step with window name '{WindowName}'", siStep.Settings.WindowName);

            try
            {
                if (executor.CurrentImage == null)
                {
                    logger.LogWarning("ShowImageStepHandler: No current image available to display");
                    return false;
                }

                void ShowBitmapImage(Bitmap bitmap, string name)
                {
                    var mat = bitmap.ToMat();
                    ShowMatImage(mat, name);
                }

                void ShowMatImage(Mat mat, string name)
                {
                    Cv2.Resize(mat, mat, new OpenCvSharp.Size(), 0.5, 0.5);
                    Cv2.ImShow(name, mat);
                    Cv2.WaitKey(1);
                }

                if (siStep.Settings.ShowRawImage)
                {
                    string windowName = $"{siStep.Settings.WindowName} - Raw Image";
                    ShowBitmapImage(executor.CurrentImage, windowName);
                    logger.LogDebug("ShowImageStepHandler: Displayed raw image in window '{WindowName}'", windowName);
                }
                
                if (siStep.Settings.ShowProcessedImage)
                {
                    string windowName = $"{siStep.Settings.WindowName} - Processed Image";
                    if (executor.CurrentImageWithResult != null &&
                        !executor.CurrentImageWithResult.IsDisposed &&
                        executor.CurrentImageWithResult.Height >= 10 &&
                        executor.CurrentImageWithResult.Width >= 10)
                    {
                        ShowMatImage(executor.CurrentImageWithResult, windowName);
                        logger.LogDebug("ShowImageStepHandler: Displayed processed image in window '{WindowName}'", windowName);
                    }
                    else
                    {
                        if (executor.CurrentImage != null)
                        {
                            ShowBitmapImage(executor.CurrentImage, windowName);
                            logger.LogDebug("ShowImageStepHandler: Displayed raw image as processed image in window '{WindowName}' (processed image not available)", windowName);
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
                return false;
            }
        }
    }
}
