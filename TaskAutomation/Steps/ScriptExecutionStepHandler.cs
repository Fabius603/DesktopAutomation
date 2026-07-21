using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class ScriptExecutionStepHandler : JobStepHandler<ScriptExecutionStep, ScriptExecutionResult>
    {
        protected override async Task<ScriptExecutionResult> ExecuteCoreAsync(
            ScriptExecutionStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("ScriptExecutionStepHandler: Script '{Path}'", step.Settings.ScriptPath);

            if (string.IsNullOrWhiteSpace(step.Settings.ScriptPath))
                throw new InvalidOperationException("No script path specified");

            if (!File.Exists(step.Settings.ScriptPath))
                throw new FileNotFoundException($"Script file not found: '{step.Settings.ScriptPath}'");

            if (!step.Settings.WaitForExit)
            {
                var scriptPath = step.Settings.ScriptPath;
                var arguments = step.Settings.Arguments;
                var scriptExecutor = ctx.ScriptExecutor;

                logger.LogInformation("ScriptExecutionStepHandler: Starting '{Path}' fire-and-forget", scriptPath);
                _ = Task.Run(async () =>
                {
                    try { await scriptExecutor.ExecuteScriptFile(scriptPath, arguments, CancellationToken.None); }
                    catch (Exception ex) { logger.LogError(ex, "ScriptExecutionStepHandler: Fire-and-forget script failed"); }
                });
            }
            else
            {
                logger.LogInformation("ScriptExecutionStepHandler: Executing '{Path}'", step.Settings.ScriptPath);
                await ctx.ScriptExecutor.ExecuteScriptFile(
                    step.Settings.ScriptPath, step.Settings.Arguments, ct);
            }

            return new ScriptExecutionResult { WasExecuted = true, Success = true };
        }

        protected override ScriptExecutionResult CreateDefault() => ScriptExecutionResult.Default;
    }
}
