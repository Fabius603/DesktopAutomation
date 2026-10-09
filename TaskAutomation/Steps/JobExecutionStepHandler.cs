using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class JobExecutionStepHandler : JobStepHandler<JobExecutionStep, JobExecutionResult>
    {
        protected override async Task<JobExecutionResult> ExecuteCoreAsync(
            JobExecutionStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
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

            var execute = ctx.StartJobViaDispatcherAsync ?? ctx.ExecuteJob;
            var completion = ctx.OwnedExecutions is { } owned
                ? await owned.StartAsync(token => execute(targetJob.Id, token), ct).ConfigureAwait(false)
                : settings.WaitForCompletion ? execute(targetJob.Id, ct)
                    : throw new InvalidOperationException("Parallel execution requires an owning job scope.");
            if (settings.WaitForCompletion) await completion.ConfigureAwait(false);
            return new JobExecutionResult { WasExecuted = true, Success = true };
        }

        protected override JobExecutionResult CreateDefault() => JobExecutionResult.Default;
    }
}
