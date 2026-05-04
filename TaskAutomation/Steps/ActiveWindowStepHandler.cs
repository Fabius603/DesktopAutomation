using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Ermittelt das aktuell aktive (Vordergrund-)Fenster und liefert dessen Titel
    /// sowie den zugehörigen Prozessnamen als <see cref="ActiveWindowResult"/>.
    /// </summary>
    public sealed class ActiveWindowStepHandler : JobStepHandler<ActiveWindowStep, ActiveWindowResult>
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        protected override Task<ActiveWindowResult> ExecuteCoreAsync(
            ActiveWindowStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var hwnd = GetForegroundWindow();

            if (hwnd == IntPtr.Zero)
            {
                ctx.Logger.LogInformation("ActiveWindowStepHandler: Kein aktives Fenster gefunden.");
                return Task.FromResult(new ActiveWindowResult { WasExecuted = true });
            }

            // Get window title
            var titleBuilder = new StringBuilder(512);
            GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity);
            var windowTitle = titleBuilder.ToString();

            // Get process name from PID
            GetWindowThreadProcessId(hwnd, out uint pid);
            string processName;
            try
            {
                using var process = Process.GetProcessById((int)pid);
                processName = process.ProcessName;
            }
            catch (Exception ex)
            {
                ctx.Logger.LogDebug(ex, "ActiveWindowStepHandler: Prozessname für PID {Pid} konnte nicht ermittelt werden.", pid);
                processName = string.Empty;
            }

            ctx.Logger.LogInformation(
                "ActiveWindowStepHandler: Aktives Fenster: '{Title}' (Prozess: '{Process}').",
                windowTitle, processName);

            return Task.FromResult(new ActiveWindowResult
            {
                WasExecuted = true,
                WindowTitle = windowTitle,
                ProcessName = processName
            });
        }

        protected override ActiveWindowResult CreateDefault() => ActiveWindowResult.Default;
    }
}
