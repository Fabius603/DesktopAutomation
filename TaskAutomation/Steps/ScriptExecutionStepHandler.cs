using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
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
                throw new FileNotFoundException("Script file not found.", step.Settings.ScriptPath);

            var outputContext = LogAmbient.Current;
            var outputPhase = StepLogScope.CurrentPhase;
            var outputIteration = StepLogScope.CurrentIteration;
            Action<string, bool>? outputCallback = ctx.ExecutionLogSession == null
                ? null
                : (line, isError) => ctx.ExecutionLogService.Record(ctx.ExecutionLogSession,
                    new LogEvent
                    {
                        Code = LogCodes.StepOutput,
                        Context = outputContext,
                        Phase = outputPhase,
                        Iteration = outputIteration,
                        Level = isError ? ExecutionLogLevel.Warning : ExecutionLogLevel.Debug,
                        Message = isError ? "Script-Fehlerausgabe empfangen." : "Script-Ausgabe empfangen.",
                        ProblemId = isError ? outputContext.StepExecutionId : null,
                        Parameters = new()
                        {
                            ["Stream"] = isError ? "StandardError" : "StandardOutput",
                            ["CharacterCount"] = line.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["StepType"] = step.GetType().Name
                        }
                    });

            if (!step.Settings.WaitForExit)
            {
                var scriptPath = step.Settings.ScriptPath;
                var arguments = step.Settings.Arguments;
                var scriptExecutor = ctx.ScriptExecutor;

                logger.LogInformation("ScriptExecutionStepHandler: Starting '{Path}' fire-and-forget", scriptPath);
                async Task ExecuteBackground(CancellationToken token)
                {
                    var duration = System.Diagnostics.Stopwatch.StartNew();
                    void RecordBackground(string code, Exception? error = null)
                    {
                        if (ctx.ExecutionLogSession is not { } session) return;
                        ctx.ExecutionLogService.Record(session, new LogEvent
                        {
                            Code = code,
                            Context = outputContext,
                            Phase = outputPhase,
                            Iteration = outputIteration,
                            Level = error is null ? ExecutionLogLevel.Information : ExecutionLogLevel.Error,
                            Message = code,
                            DurationMs = duration.ElapsedMilliseconds,
                            DiagnosticCode = error is null ? null : LogDiagnostics.Code(error),
                            ProblemId = error is null ? null : LogDiagnostics.ProblemId(error, outputContext.StepExecutionId),
                            Details = error is null ? null : LogDiagnostics.ExceptionDetails(error),
                            Parameters = new() { ["CompletionScope"] = "BackgroundExecution", ["StepType"] = step.GetType().Name }
                        });
                    }
                    try
                    {
                        RecordBackground(LogCodes.StepBackgroundStarted);
                        await scriptExecutor.ExecuteScriptFile(
                            scriptPath, arguments, token, outputCallback);
                        RecordBackground(LogCodes.StepBackgroundCompleted);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    { RecordBackground(LogCodes.StepBackgroundCancelled); throw; }
                    catch (Exception ex)
                    {
                        RecordBackground(LogCodes.StepBackgroundFailed, ex);
                        logger.LogError(ex, "ScriptExecutionStepHandler: Parallel script failed");
                        throw;
                    }
                }
                if (ctx.OwnedExecutions is { } owned) await owned.StartAsync(ExecuteBackground, ct).ConfigureAwait(false);
                else throw new InvalidOperationException("Parallel execution requires an owning job scope.");
            }
            else
            {
                logger.LogInformation("ScriptExecutionStepHandler: Executing '{Path}'", step.Settings.ScriptPath);
                await ctx.ScriptExecutor.ExecuteScriptFile(
                    step.Settings.ScriptPath, step.Settings.Arguments, ct, outputCallback);
            }

            return new ScriptExecutionResult { WasExecuted = true, Success = true };
        }

        protected override ScriptExecutionResult CreateDefault() => ScriptExecutionResult.Default;
    }
}
