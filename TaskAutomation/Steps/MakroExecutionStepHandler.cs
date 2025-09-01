using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public class MakroExecutionStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;
            
            if (step is not MakroExecutionStep miStep)
            {
                var errorMessage = $"Invalid step type - expected MakroExecutionStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("MakroExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("MakroExecutionStepHandler: Processing makro execution for makro '{MakroName}'", miStep.Settings.MakroName);

            try
            {
                if (string.IsNullOrWhiteSpace(miStep.Settings.MakroName))
                {
                    var errorMessage = "No makro name specified";
                    logger.LogWarning("MakroExecutionStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                if (!executor.AllMakros.TryGetValue(miStep.Settings.MakroName, out var makro))
                {
                    var errorMessage = $"Makro '{miStep.Settings.MakroName}' not found";
                    logger.LogError("MakroExecutionStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                logger.LogInformation("MakroExecutionStepHandler: Executing makro '{MakroName}'", miStep.Settings.MakroName);
                await executor.MakroExecutor.ExecuteMakro(makro, executor.DxgiResources, ct);
                logger.LogInformation("MakroExecutionStepHandler: Makro '{MakroName}' executed successfully", miStep.Settings.MakroName);

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("MakroExecutionStepHandler: Makro execution was cancelled");
                return false; // Return false for cancellation, don't treat as error
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MakroExecutionStepHandler: Failed to execute makro '{MakroName}': {ErrorMessage}", miStep.Settings.MakroName, ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
