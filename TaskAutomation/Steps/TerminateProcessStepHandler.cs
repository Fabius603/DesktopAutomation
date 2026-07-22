using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class TerminateProcessStepHandler : JobStepHandler<TerminateProcessStep, TerminateProcessResult>
{
    protected override async Task<TerminateProcessResult> ExecuteCoreAsync(
        TerminateProcessStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        var target = step.Settings.Target;
        var sourceReference = target.ProcessSource.IsConfigured
            ? ResultBindingResolver.Resolve<RuntimeProcessReference>(ctx.Results, target.ProcessSource).FirstOrDefault
            : null;
        var processName = ProcessWindowMatcher.NormalizeProcessName(sourceReference?.ProcessName ?? target.ProcessName);
        var matchingIds = ProcessTargetResolver.ResolveProcessIds(target, ctx.Results);
        if (matchingIds.Count == 0)
        {
            var titleText = string.IsNullOrWhiteSpace(target.WindowTitleContains)
                ? string.Empty
                : $" mit einem Fenster, dessen Titel '{target.WindowTitleContains}' enthält";
            var message = $"Prozess '{processName}'{titleText} wurde nicht gefunden.";
            ctx.Logger.LogWarning("TerminateProcessStepHandler: {Error}", message);
            return new TerminateProcessResult { WasExecuted = true, Success = false, ErrorMessage = message };
        }

        var terminated = 0;
        var errors = new List<string>();
        foreach (var processId in matchingIds)
        {
            ct.ThrowIfCancellationRequested();
            if (processId == Environment.ProcessId)
            {
                errors.Add($"PID {processId} ist die DesktopAutomation-Anwendung und wurde nicht beendet.");
                continue;
            }

            try
            {
                using var process = Process.GetProcessById(processId);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(ct).ConfigureAwait(false);
                terminated++;
                ctx.Logger.LogInformation(
                    "TerminateProcessStepHandler: Prozess '{Process}' (PID {Pid}) beendet.", processName, processId);
            }
            catch (ArgumentException)
            {
                terminated++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add($"PID {processId}: {ex.Message}");
                ctx.Logger.LogWarning(ex,
                    "TerminateProcessStepHandler: Prozess '{Process}' (PID {Pid}) konnte nicht beendet werden.",
                    processName, processId);
            }
        }

        if (errors.Count == 0)
            return new TerminateProcessResult { WasExecuted = true, Success = true };

        var errorMessage = terminated > 0
            ? $"{terminated} Prozess(e) beendet; {errors.Count} Fehler: {string.Join("; ", errors)}"
            : string.Join("; ", errors);
        return new TerminateProcessResult { WasExecuted = true, Success = false, ErrorMessage = errorMessage };
    }

    protected override TerminateProcessResult CreateDefault() => TerminateProcessResult.Default;
}
