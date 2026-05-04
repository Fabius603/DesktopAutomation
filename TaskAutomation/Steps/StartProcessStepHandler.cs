using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Startet ein Programm oder eine ausführbare Datei.
    /// Wenn <see cref="StartProcessSettings.WaitForExit"/> aktiviert ist,
    /// wird auf die Beendigung des Prozesses gewartet (unter Berücksichtigung des
    /// Cancellation-Tokens). Das Ergebnis (<see cref="TaskResult.Success"/>) ist
    /// <c>true</c>, wenn der Prozess erfolgreich gestartet wurde.
    /// </summary>
    public sealed class StartProcessStepHandler : JobStepHandler<StartProcessStep, TaskResult>
    {
        protected override async Task<TaskResult> ExecuteCoreAsync(
            StartProcessStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var executablePath = step.Settings.ExecutablePath?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(executablePath))
            {
                ctx.Logger.LogWarning("StartProcessStepHandler: Kein Pfad zur ausführbaren Datei konfiguriert.");
                return new TaskResult { WasExecuted = true, Success = false,
                    ErrorMessage = "Kein Pfad zur ausführbaren Datei konfiguriert." };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName  = executablePath,
                Arguments = step.Settings.Arguments ?? string.Empty,
                UseShellExecute = true
            };

            ctx.Logger.LogInformation(
                "StartProcessStepHandler: Starte '{Path}' mit Argumenten '{Args}' (WaitForExit={Wait}).",
                executablePath, startInfo.Arguments, step.Settings.WaitForExit);

            Process? process;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ctx.Logger.LogError(ex, "StartProcessStepHandler: Fehler beim Starten des Prozesses '{Path}'.", executablePath);
                return new TaskResult { WasExecuted = true, Success = false, ErrorMessage = ex.Message };
            }

            if (process == null)
            {
                return new TaskResult { WasExecuted = true, Success = false,
                    ErrorMessage = "Prozess konnte nicht gestartet werden (Process.Start gab null zurück)." };
            }

            if (step.Settings.WaitForExit)
            {
                try
                {
                    await process.WaitForExitAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    process.Dispose();
                }
            }
            else
            {
                process.Dispose();
            }

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
    }
}
