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

namespace TaskAutomation.Steps
{
    public class DesktopDuplicationStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutionContext executor, CancellationToken ct)
        {
            var ddStep = step as DesktopDuplicationStep;
            if (ddStep == null)
            {
                return false;
            }

            // Dispose previous resources to prevent memory leaks
            executor.CurrentImage?.Dispose();
            executor.CurrentImage = null;
            executor.CurrentDesktopFrame?.Dispose();
            executor.CurrentDesktopFrame = null;

            if (executor.DesktopDuplicator == null)
            {
                executor.DesktopDuplicator = new DesktopDuplicator(ddStep.Settings.DesktopIdx);
            }
            
            try
            {
                ct.ThrowIfCancellationRequested();
                executor.CurrentDesktopFrame = executor.DesktopDuplicator.GetLatestFrame();
                
                // Clone the bitmap to avoid sharing references
                if (executor.CurrentDesktopFrame?.DesktopImage != null)
                {
                    executor.CurrentImage = new Bitmap(executor.CurrentDesktopFrame.DesktopImage);
                    
                    // Set the current offset to the screen bounds for this desktop index
                    var screenBounds = ImageHelperMethods.ScreenHelper.GetDesktopBounds(ddStep.Settings.DesktopIdx);
                    executor.CurrentOffset = new OpenCvSharp.Point(screenBounds.Left, screenBounds.Top);
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"DesktopDuplication error: {ex.Message}");
                
                // Clean up on error
                executor.CurrentImage?.Dispose();
                executor.CurrentImage = null;
                executor.CurrentDesktopFrame?.Dispose();
                executor.CurrentDesktopFrame = null;
                
                return true; // Continue execution
            }

            return true;
        }
    }
}
