using ImageCapture.ProcessDuplication;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public class ProcessDuplicationStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;
            
            if (step is not ProcessDuplicationStep pdStep)
            {
                var errorMessage = $"Invalid step type - expected ProcessDuplicationStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("ProcessDuplicationStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("ProcessDuplicationStepHandler: Processing process duplication for process '{ProcessName}'", pdStep.Settings.ProcessName);

            try
            {
                if (string.IsNullOrWhiteSpace(pdStep.Settings.ProcessName))
                {
                    var errorMessage = "No process name specified";
                    logger.LogWarning("ProcessDuplicationStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                executor.ProcessDuplicationResult?.Dispose(); // Vorherigen Frame freigeben
                executor.CurrentImage?.Dispose(); // Vorheriges Desktop-Bild freigeben

                if (executor.ProcessDuplicator == null)
                {
                    executor.ProcessDuplicator = new ProcessDuplicator(pdStep.Settings.ProcessName);
                    logger.LogDebug("ProcessDuplicationStepHandler: Created new ProcessDuplicator for process '{ProcessName}'", pdStep.Settings.ProcessName);
                }

                logger.LogInformation("ProcessDuplicationStepHandler: Capturing process '{ProcessName}'", pdStep.Settings.ProcessName);
                executor.ProcessDuplicationResult = executor.ProcessDuplicator.CaptureProcess();
                
                if (!executor.ProcessDuplicationResult.ProcessFound)
                {
                    logger.LogWarning("ProcessDuplicationStepHandler: Process '{ProcessName}' not found", pdStep.Settings.ProcessName);
                    return true; // Continue with next step
                }

                executor.CurrentImage = executor.ProcessDuplicationResult.ProcessImage.Clone() as Bitmap;
                executor.CurrentOffset = executor.ProcessDuplicationResult.WindowOffsetOnDesktop;
                
                logger.LogInformation("ProcessDuplicationStepHandler: Successfully captured process '{ProcessName}' at offset ({X}, {Y})", 
                    pdStep.Settings.ProcessName, executor.CurrentOffset.X, executor.CurrentOffset.Y);
                
                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("ProcessDuplicationStepHandler: Process duplication was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ProcessDuplicationStepHandler: Failed to capture process '{ProcessName}': {ErrorMessage}", pdStep.Settings.ProcessName, ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
