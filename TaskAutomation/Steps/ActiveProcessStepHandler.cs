using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Prüft, ob ein Prozess mit dem konfigurierten Namen aktuell ausgeführt wird.
    /// Das Ergebnis (<see cref="TaskResult.Success"/>) ist <c>true</c>, wenn mindestens
    /// eine Instanz des Prozesses gefunden wurde, sonst <c>false</c>.
    /// </summary>
    public sealed class ActiveProcessStepHandler : JobStepHandler<ActiveProcessStep, ActiveProcessResult>
    {
        protected override Task<ActiveProcessResult> ExecuteCoreAsync(
            ActiveProcessStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var processName = ProcessWindowMatcher.NormalizeProcessName(step.Settings.ProcessName);

            if (string.IsNullOrEmpty(processName))
            {
                ctx.Logger.LogWarning("ActiveProcessStepHandler: Kein Prozessname konfiguriert.");
                return Task.FromResult(new ActiveProcessResult { WasExecuted = true, IsRunning = false });
            }

            var isRunning = ProcessWindowMatcher.FindMatchingProcessIds(processName, null).Count > 0;

            ctx.Logger.LogInformation(
                "ActiveProcessStepHandler: Prozess '{Name}' {Status}.",
                step.Settings.ProcessName, isRunning ? "läuft" : "nicht gefunden");

            return Task.FromResult(new ActiveProcessResult { WasExecuted = true, IsRunning = isRunning });
        }

        protected override ActiveProcessResult CreateDefault() => ActiveProcessResult.Default;
    }
}
