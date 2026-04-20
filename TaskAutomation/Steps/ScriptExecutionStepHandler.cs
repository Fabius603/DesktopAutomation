using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class ScriptExecutionStepHandler : JobStepHandler<ScriptExecutionStep, TaskResult>
    {
        protected override async Task<TaskResult> ExecuteCoreAsync(
            ScriptExecutionStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("ScriptExecutionStepHandler: Script '{Path}'", step.Settings.ScriptPath);

            if (string.IsNullOrWhiteSpace(step.Settings.ScriptPath))
                throw new InvalidOperationException("No script path specified");

            if (!File.Exists(step.Settings.ScriptPath))
                throw new FileNotFoundException($"Script file not found: '{step.Settings.ScriptPath}'");

            if (step.Settings.FireAndForget)
            {
                logger.LogInformation("ScriptExecutionStepHandler: Starting '{Path}' fire-and-forget", step.Settings.ScriptPath);
                _ = Task.Run(async () =>
                {
                    try { await ctx.ScriptExecutor.ExecuteScriptFile(step.Settings.ScriptPath, CancellationToken.None); }
                    catch (Exception ex) { logger.LogError(ex, "ScriptExecutionStepHandler: Fire-and-forget script failed"); }
                });
            }
            else
            {
                logger.LogInformation("ScriptExecutionStepHandler: Executing '{Path}'", step.Settings.ScriptPath);
                await ctx.ScriptExecutor.ExecuteScriptFile(step.Settings.ScriptPath, ct);
            }

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
    }
}
