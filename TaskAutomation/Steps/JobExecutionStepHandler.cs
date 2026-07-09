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

            if (settings.WaitForCompletion)
            {
                logger.LogInformation(
                    "JobExecutionStepHandler: Executing '{Name}' and waiting for completion", targetJob.Name);

                if (ctx.StartJobViaDispatcherAsync != null)
                {
                    // Über den Dispatcher: sichtbar in RunningJobInstances, abbruchfähig
                    await ctx.StartJobViaDispatcherAsync(targetJob.Id, ct).ConfigureAwait(false);
                }
                else
                {
                    // Fallback (z.B. in Tests)
                    await ctx.ExecuteJob(targetJob.Id, ct).ConfigureAwait(false);
                }

                logger.LogInformation("JobExecutionStepHandler: '{Name}' completed", targetJob.Name);
            }
            else
            {
                // Verknüpfter Token: wenn der Eltern-Job abgebrochen wird, wird auch dieser gestoppt.
                logger.LogInformation(
                    "JobExecutionStepHandler: Starting '{Name}' fire-and-forget (linked to parent)", targetJob.Name);
                var id = targetJob.Id;

                if (ctx.StartJobViaDispatcher != null)
                {
                    // Über den Dispatcher starten → in RunningJobInstances sichtbar und abbruchfähig.
                    // Instanz-ID merken: JobExecutor.ExecuteJobAsync bereinigt sie beim Abbruch des Eltern-Jobs.
                    var instanceId = ctx.StartJobViaDispatcher(id);
                    if (instanceId != Guid.Empty)
                        ctx.ChildJobInstanceIds.Add(instanceId);
                    logger.LogInformation(
                        "JobExecutionStepHandler: '{Name}' started as dispatcher instance {InstanceId}",
                        targetJob.Name, instanceId);
                }
                else
                {
                    // Fallback (z.B. in Tests): direkter Aufruf mit verknüpftem CancellationToken
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    var executeJob = ctx.ExecuteJob;
                    var targetJobName = targetJob.Name;
                    _ = Task.Run(async () =>
                    {
                        try   { await executeJob(id, linkedCts.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException) { /* expected when parent stops */ }
                        catch (Exception ex)
                        {
                            logger.LogError(ex,
                                "JobExecutionStepHandler: Fire-and-forget job '{Name}' failed", targetJobName);
                        }
                        finally { linkedCts.Dispose(); }
                    });
                }
            }

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
    }
}
