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
                logger.LogError("MakroExecutionStepHandler: Invalid step type - expected MakroExecutionStep, got {StepType}", step?.GetType().Name ?? "null");
                return false;
            }

            logger.LogDebug("MakroExecutionStepHandler: Processing makro execution for makro '{MakroName}'", miStep.Settings.MakroName);

            try
            {
                if (string.IsNullOrWhiteSpace(miStep.Settings.MakroName))
                {
                    logger.LogWarning("MakroExecutionStepHandler: No makro name specified");
                    return false;
                }

                if (!executor.AllMakros.TryGetValue(miStep.Settings.MakroName, out var makro))
                {
                    logger.LogError("MakroExecutionStepHandler: Makro '{MakroName}' not found", miStep.Settings.MakroName);
                    return false;
                }

                logger.LogInformation("MakroExecutionStepHandler: Executing makro '{MakroName}'", miStep.Settings.MakroName);
                await executor.MakroExecutor.ExecuteMakro(makro, executor.DxgiResources, ct);
                logger.LogInformation("MakroExecutionStepHandler: Makro '{MakroName}' executed successfully", miStep.Settings.MakroName);

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("MakroExecutionStepHandler: Makro execution was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MakroExecutionStepHandler: Failed to execute makro '{MakroName}': {ErrorMessage}", miStep.Settings.MakroName, ex.Message);
                return false;
            }
        }
    }
}
