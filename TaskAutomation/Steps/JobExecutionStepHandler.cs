using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class JobExecutionStepHandler : JobStepHandler<JobExecutionStep, TaskResult>
    {
        protected override async Task<TaskResult> ExecuteCoreAsync(
            JobExecutionStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger   = ctx.Logger;
            var settings = step.Settings;

            // Job auflösen (ID hat Vorrang vor Name)
            Job? targetJob;
            if (settings.JobId.HasValue)
            {
                targetJob = ctx.AllJobs.Values.FirstOrDefault(j => j.Id == settings.JobId.Value);
                if (targetJob == null)
                    throw new InvalidOperationException($"Target job with ID '{settings.JobId}' not found");
            }
            else if (!string.IsNullOrWhiteSpace(settings.JobName))
            {
                targetJob = ctx.AllJobs.Values.FirstOrDefault(j =>
                    string.Equals(j.Name, settings.JobName, StringComparison.OrdinalIgnoreCase));
                if (targetJob == null)
                    throw new InvalidOperationException($"Target job '{settings.JobName}' not found");
            }
            else
            {
                throw new InvalidOperationException("No job ID or name specified in JobExecutionStep");
            }

            if (targetJob.Id == ctx.CurrentJob.Id)
                throw new InvalidOperationException(
                    $"Cannot execute the same job '{ctx.CurrentJob.Name}' – preventing infinite recursion");

            if (targetJob.Repeating)
                throw new InvalidOperationException(
                    $"Cannot execute repeating job '{targetJob.Name}' from within another job");

            if (settings.WaitForCompletion)
            {
                logger.LogInformation("JobExecutionStepHandler: Executing '{Name}' and waiting for completion", targetJob.Name);
                await ctx.ExecuteJob(targetJob.Id, ct);
                logger.LogInformation("JobExecutionStepHandler: '{Name}' completed", targetJob.Name);
            }
            else
            {
                logger.LogInformation("JobExecutionStepHandler: Starting '{Name}' fire-and-forget", targetJob.Name);
                var id = targetJob.Id;
                _ = Task.Run(async () =>
                {
                    try   { await ctx.ExecuteJob(id, CancellationToken.None); }
                    catch (Exception ex) { logger.LogError(ex, "JobExecutionStepHandler: Fire-and-forget job '{Name}' failed", targetJob.Name); }
                });
            }

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
    }
}
