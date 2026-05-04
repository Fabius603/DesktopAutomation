using System.Diagnostics;
using System.Linq;
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
            var processName = step.Settings.ProcessName?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(processName))
            {
                ctx.Logger.LogWarning("ActiveProcessStepHandler: Kein Prozessname konfiguriert.");
                return Task.FromResult(new ActiveProcessResult { WasExecuted = true, IsRunning = false });
            }

            // Remove .exe extension for GetProcessesByName compatibility
            var nameWithoutExt = processName.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase)
                ? processName[..^4]
                : processName;

            var processes = Process.GetProcessesByName(nameWithoutExt);
            var isRunning = processes.Length > 0;

            foreach (var p in processes) p.Dispose();

            ctx.Logger.LogInformation(
                "ActiveProcessStepHandler: Prozess '{Name}' {Status}.",
                processName, isRunning ? "läuft" : "nicht gefunden");

            return Task.FromResult(new ActiveProcessResult { WasExecuted = true, IsRunning = isRunning });
        }

        protected override ActiveProcessResult CreateDefault() => ActiveProcessResult.Default;
    }
}
