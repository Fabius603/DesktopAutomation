using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class ActiveProcessStepHandler : JobStepHandler<ActiveProcessStep, ActiveProcessResult>
{
    protected override Task<ActiveProcessResult> ExecuteCoreAsync(
        ActiveProcessStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var target = step.Settings.Target;
        if (!target.ProcessSource.IsConfigured
            && string.IsNullOrWhiteSpace(target.ProcessName)
            && string.IsNullOrWhiteSpace(target.ExecutablePath))
        {
            ctx.Logger.LogWarning("ActiveProcessStepHandler: Kein Prozessziel konfiguriert.");
            return Task.FromResult(new ActiveProcessResult { WasExecuted = true });
        }

        var processIds = ProcessTargetResolver.ResolveProcessIds(target, ctx.Results);
        var processReference = processIds.Count > 0
            ? ProcessTargetResolver.CreateReference(processIds[0])
            : null;
        ctx.Logger.LogInformation(
            "ActiveProcessStepHandler: Ziel '{Target}' - aktiv: {IsRunning}, Treffer: {MatchCount}.",
            TargetLabel(target), processIds.Count > 0, processIds.Count);
        return Task.FromResult(new ActiveProcessResult
        {
            WasExecuted = true,
            IsRunning = processIds.Count > 0,
            MatchCount = processIds.Count,
            Process = processReference
        });
    }

    protected override ActiveProcessResult CreateDefault() => ActiveProcessResult.Default;

    private static string TargetLabel(ProcessTargetSettings target) =>
        target.ProcessSource.IsConfigured
            ? $"Step {target.ProcessSource.SourceStepId}"
            : !string.IsNullOrWhiteSpace(target.ProcessName) ? target.ProcessName : target.ExecutablePath;
}
