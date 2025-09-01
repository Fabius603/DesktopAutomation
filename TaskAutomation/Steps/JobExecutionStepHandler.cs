using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public class JobExecutionStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job job, IJobExecutor ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            
            if (step is not JobExecutionStep jobExecutionStep)
            {
                var errorMessage = $"Invalid step type - expected JobExecutionStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var settings = jobExecutionStep.Settings;
            var targetJobName = settings.JobName;

            logger.LogDebug("JobExecutionStepHandler: Processing job execution step for target job '{TargetJobName}'", targetJobName);

            // Validation: Check if target job name is empty
            if (string.IsNullOrWhiteSpace(targetJobName))
            {
                var errorMessage = "No job name specified in JobExecutionStep";
                logger.LogWarning("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Validation: Check if trying to execute the same job (prevent infinite recursion)
            if (string.Equals(targetJobName, job.Name, StringComparison.OrdinalIgnoreCase))
            {
                var errorMessage = $"Cannot execute the same job '{job.Name}' that is currently running - preventing infinite recursion";
                logger.LogWarning("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Validation: Check if target job exists
            if (!ctx.AllJobs.TryGetValue(targetJobName, out var targetJob))
            {
                var errorMessage = $"Target job '{targetJobName}' not found";
                logger.LogError("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Validation: Check if target job is repeating (not allowed)
            if (targetJob.Repeating)
            {
                var errorMessage = $"Cannot execute repeating job '{targetJobName}' from within another job";
                logger.LogWarning("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            try
            {
                if (settings.WaitForCompletion)
                {
                    logger.LogInformation("JobExecutionStepHandler: Executing job '{TargetJobName}' and waiting for completion", targetJobName);
                    // Execute job and wait for completion
                    await ctx.ExecuteJob(targetJobName, ct);
                    logger.LogInformation("JobExecutionStepHandler: Job '{TargetJobName}' completed successfully", targetJobName);
                }
                else
                {
                    logger.LogInformation("JobExecutionStepHandler: Starting job '{TargetJobName}' in fire-and-forget mode", targetJobName);
                    // Fire and forget - start job without waiting
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ctx.ExecuteJob(targetJobName, CancellationToken.None);
                            logger.LogDebug("JobExecutionStepHandler: Fire-and-forget job '{TargetJobName}' completed", targetJobName);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "JobExecutionStepHandler: Fire-and-forget job '{TargetJobName}' failed", targetJobName);
                        }
                    });
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("JobExecutionStepHandler: Job '{TargetJobName}' execution was cancelled", targetJobName);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JobExecutionStepHandler: Failed to execute job '{TargetJobName}': {ErrorMessage}", targetJobName, ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
