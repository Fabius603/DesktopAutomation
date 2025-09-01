using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public class ScriptExecutionStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor executor, CancellationToken ct)
        {
            var logger = executor.Logger;
            
            if (step is not ScriptExecutionStep scStep)
            {
                var errorMessage = $"Invalid step type - expected ScriptExecutionStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("ScriptExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("ScriptExecutionStepHandler: Processing script execution for '{ScriptPath}'", scStep.Settings.ScriptPath);

            try
            {
                if (string.IsNullOrWhiteSpace(scStep.Settings.ScriptPath))
                {
                    var errorMessage = "No script path specified";
                    logger.LogWarning("ScriptExecutionStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                var isFile = File.Exists(scStep.Settings.ScriptPath);
                if (!isFile)
                {
                    var errorMessage = $"Script file not found: '{scStep.Settings.ScriptPath}'";
                    logger.LogError("ScriptExecutionStepHandler: {ErrorMessage}", errorMessage);
                    throw new FileNotFoundException(errorMessage);
                }

                if (scStep.Settings.FireAndForget)
                {
                    logger.LogInformation("ScriptExecutionStepHandler: Starting script '{ScriptPath}' in fire-and-forget mode", scStep.Settings.ScriptPath);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await executor.ScriptExecutor.ExecuteScriptFile(scStep.Settings.ScriptPath, CancellationToken.None);
                            logger.LogDebug("ScriptExecutionStepHandler: Fire-and-forget script '{ScriptPath}' completed successfully", scStep.Settings.ScriptPath);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "ScriptExecutionStepHandler: Fire-and-forget script '{ScriptPath}' failed", scStep.Settings.ScriptPath);
                        }
                    });
                    return true;
                }
                else
                {
                    logger.LogInformation("ScriptExecutionStepHandler: Executing script '{ScriptPath}' and waiting for completion", scStep.Settings.ScriptPath);
                    await executor.ScriptExecutor.ExecuteScriptFile(scStep.Settings.ScriptPath, ct);
                    logger.LogInformation("ScriptExecutionStepHandler: Script '{ScriptPath}' executed successfully", scStep.Settings.ScriptPath);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("ScriptExecutionStepHandler: Script execution was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ScriptExecutionStepHandler: Failed to execute script '{ScriptPath}': {ErrorMessage}", scStep.Settings.ScriptPath, ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
