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

            logger.LogDebug("JobExecutionStepHandler: Processing job execution step");

            // Try to find by ID first, fallback to name for backward compatibility
            Job? targetJob = null;
            string? targetJobInfo = null;

            if (settings.JobId.HasValue)
            {
                targetJob = ctx.AllJobs.Values.FirstOrDefault(j => j.Id == settings.JobId.Value);
                targetJobInfo = $"ID '{settings.JobId}'";
                if (targetJob == null)
                {
                    var errorMessage = $"Target job with ID '{settings.JobId}' not found";
                    logger.LogError("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
            }
            else if (!string.IsNullOrWhiteSpace(settings.JobName))
            {
                if (!ctx.AllJobs.TryGetValue(settings.JobName, out targetJob))
                {
                    var errorMessage = $"Target job '{settings.JobName}' not found";
                    logger.LogError("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                targetJobInfo = $"name '{settings.JobName}'";
            }
            else
            {
                var errorMessage = "No job ID or name specified in JobExecutionStep";
                logger.LogWarning("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Validation: Check if trying to execute the same job (prevent infinite recursion)
            if (targetJob.Id == job.Id)
            {
                var errorMessage = $"Cannot execute the same job '{job.Name}' (ID: {job.Id}) that is currently running - preventing infinite recursion";
                logger.LogWarning("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Validation: Check if target job is repeating (not allowed)
            if (targetJob.Repeating)
            {
                var errorMessage = $"Cannot execute repeating job '{targetJob.Name}' from within another job";
                logger.LogWarning("JobExecutionStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            try
            {
                if (settings.WaitForCompletion)
                {
                    logger.LogInformation("JobExecutionStepHandler: Executing job '{TargetJobName}' (ID: {TargetJobId}) and waiting for completion", targetJob.Name, targetJob.Id);
                    // Execute job and wait for completion
                    await ctx.ExecuteJob(targetJob.Name, ct);
                    logger.LogInformation("JobExecutionStepHandler: Job '{TargetJobName}' completed successfully", targetJob.Name);
                }
                else
                {
                    logger.LogInformation("JobExecutionStepHandler: Starting job '{TargetJobName}' (ID: {TargetJobId}) in fire-and-forget mode", targetJob.Name, targetJob.Id);
                    // Fire and forget - start job without waiting
                    var jobName = targetJob.Name; // Capture for closure
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ctx.ExecuteJob(jobName, CancellationToken.None);
                            logger.LogDebug("JobExecutionStepHandler: Fire-and-forget job '{TargetJobName}' completed", jobName);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "JobExecutionStepHandler: Fire-and-forget job '{TargetJobName}' failed", jobName);
                        }
                    });
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("JobExecutionStepHandler: Job '{TargetJobName}' execution was cancelled", targetJob.Name);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JobExecutionStepHandler: Failed to execute job {TargetJobInfo}: {ErrorMessage}", targetJobInfo, ex.Message);
                throw; // Re-throw all other exceptions
            }
        }
    }
}
