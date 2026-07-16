using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Prueft, ob ein Fenster des angegebenen Prozesses das aktive Vordergrundfenster ist.
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
            var processName = Path.GetFileNameWithoutExtension(step.Settings.ProcessName?.Trim() ?? string.Empty);
            var cacheMs = Math.Max(0, step.Settings.CacheMs);

            if (cacheMs > 0 &&
                ctx.ActiveWindowCache.TryGetValue(step.Id, out var cached) &&
                string.Equals(cached.ProcessName, processName, StringComparison.OrdinalIgnoreCase) &&
                (DateTime.Now - cached.Timestamp).TotalMilliseconds < cacheMs)
            {
                return Task.FromResult(new ActiveWindowResult { WasExecuted = true, IsActive = cached.IsActive });
            }

            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                ctx.Logger.LogInformation("ActiveWindowStepHandler: Kein aktives Fenster gefunden.");
                return Task.FromResult(CacheResult(ctx, step.Id, processName, false));
            }

            GetWindowThreadProcessId(hwnd, out uint pid);

            bool isActive = false;
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                isActive = proc.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                ctx.Logger.LogDebug(
                    ex,
                    "ActiveWindowStepHandler: Prozessname fuer PID {Pid} konnte nicht ermittelt werden.",
                    pid);
            }

            ctx.Logger.LogInformation(
                "ActiveWindowStepHandler: Prozess '{Process}' - Fenster aktiv: {IsActive}.",
                processName,
                isActive);

            return Task.FromResult(CacheResult(ctx, step.Id, processName, isActive));
        }

        protected override ActiveWindowResult CreateDefault() => ActiveWindowResult.Default;

        private static ActiveWindowResult CacheResult(
            IStepPipelineContext ctx,
            string stepId,
            string processName,
            bool isActive)
        {
            ctx.ActiveWindowCache[stepId] = new ActiveWindowCacheEntry(processName, isActive, DateTime.Now);
            return new ActiveWindowResult { WasExecuted = true, IsActive = isActive };
        }
    }
}
