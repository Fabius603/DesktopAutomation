using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Prüft, ob ein Fenster des angegebenen Prozesses das aktive Vordergrundfenster ist.
    /// </summary>
    public sealed class ActiveWindowStepHandler : JobStepHandler<ActiveWindowStep, ActiveWindowResult>
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        protected override Task<ActiveWindowResult> ExecuteCoreAsync(
            ActiveWindowStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var processName = step.Settings.ProcessName?.Trim() ?? string.Empty;

            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                ctx.Logger.LogInformation("ActiveWindowStepHandler: Kein aktives Fenster gefunden.");
                return Task.FromResult(new ActiveWindowResult { WasExecuted = true, IsActive = false });
            }

            GetWindowThreadProcessId(hwnd, out uint pid);

            bool isActive = false;
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                isActive = proc.ProcessName.Equals(
                    processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? processName[..^4]
                        : processName,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                ctx.Logger.LogDebug(ex,
                    "ActiveWindowStepHandler: Prozessname für PID {Pid} konnte nicht ermittelt werden.", pid);
            }

            ctx.Logger.LogInformation(
                "ActiveWindowStepHandler: Prozess '{Process}' – Fenster aktiv: {IsActive}.",
                processName, isActive);

            return Task.FromResult(new ActiveWindowResult { WasExecuted = true, IsActive = isActive });
        }

        protected override ActiveWindowResult CreateDefault() => ActiveWindowResult.Default;
    }
}
