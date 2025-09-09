using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
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
                // Try to find by ID first, fallback to name for backward compatibility
                TaskAutomation.Makros.Makro? makro = null;
                if (miStep.Settings.MakroId.HasValue)
                {
                    makro = executor.AllMakros.Values.FirstOrDefault(m => m.Id == miStep.Settings.MakroId.Value);
                    if (makro == null)
                    {
                        var errorMessage = $"Makro with ID '{miStep.Settings.MakroId}' not found";
                        logger.LogError("MakroExecutionStepHandler: {ErrorMessage}", errorMessage);
                        throw new InvalidOperationException(errorMessage);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(miStep.Settings.MakroName))
                {
                    if (!executor.AllMakros.TryGetValue(miStep.Settings.MakroName, out makro))
                    {
                        var errorMessage = $"Makro '{miStep.Settings.MakroName}' not found";
                        logger.LogError("MakroExecutionStepHandler: {ErrorMessage}", errorMessage);
                        throw new InvalidOperationException(errorMessage);
                    }
                }
                else
                {
                    var errorMessage = "No makro ID or name specified";
                    logger.LogWarning("MakroExecutionStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                logger.LogInformation("MakroExecutionStepHandler: Executing makro '{MakroName}' (ID: {MakroId})", makro.Name, makro.Id);
                await executor.MakroExecutor.ExecuteMakro(makro, executor.DxgiResources, ct);
                logger.LogInformation("MakroExecutionStepHandler: Makro '{MakroName}' executed successfully", makro.Name);

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("MakroExecutionStepHandler: Makro execution was cancelled");
                return false; // Return false for cancellation, don't treat as error
            }
            catch (Exception ex)
            {
                var makroInfo = miStep.Settings.MakroId.HasValue ? $"ID '{miStep.Settings.MakroId}'" : $"name '{miStep.Settings.MakroName}'";
                logger.LogError(ex, "MakroExecutionStepHandler: Failed to execute makro {MakroInfo}: {ErrorMessage}", makroInfo, ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
