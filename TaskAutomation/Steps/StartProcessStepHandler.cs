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
    /// Startet ein Programm oder eine ausführbare Datei.
    /// Wenn <see cref="StartProcessSettings.WaitForExit"/> aktiviert ist,
    /// wird auf die Beendigung des Prozesses gewartet (unter Berücksichtigung des
    /// Cancellation-Tokens). Das Ergebnis (<see cref="TaskResult.Success"/>) ist
    /// <c>true</c>, wenn der Prozess erfolgreich gestartet wurde.
    /// </summary>
    public sealed class StartProcessStepHandler : JobStepHandler<StartProcessStep, TaskResult>
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;
        private const int SW_MAXIMIZE = 3;

        protected override async Task<TaskResult> ExecuteCoreAsync(
            StartProcessStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var configuredExecutable = step.Settings.ExecutablePath?.Trim() ?? string.Empty;

            if (!ExecutablePathResolver.TryResolve(configuredExecutable, out var executablePath))
            {
                ctx.Logger.LogWarning("StartProcessStepHandler: Programm '{Program}' wurde nicht gefunden.", configuredExecutable);
                return new TaskResult { WasExecuted = true, Success = false,
                    ErrorMessage = $"Programm '{configuredExecutable}' wurde nicht gefunden." };
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

            await PositionMainWindowAsync(process, step.Settings, ctx.Logger, ct)
                .ConfigureAwait(false);

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

        private static async Task PositionMainWindowAsync(
            Process process, StartProcessSettings settings, ILogger logger, CancellationToken ct)
        {
            IntPtr hwnd = IntPtr.Zero;
            try
            {
                // Console programs and launcher processes often never own a main window.
                // Keep this best-effort positioning bounded so the job can continue quickly.
                for (var attempt = 0; attempt < 20 && hwnd == IntPtr.Zero && !process.HasExited; attempt++)
                {
                    process.Refresh();
                    hwnd = process.MainWindowHandle;
                    if (hwnd == IntPtr.Zero)
                        await Task.Delay(50, ct).ConfigureAwait(false);
                }

                if (hwnd == IntPtr.Zero)
                {
                    logger.LogDebug("StartProcessStepHandler: Kein positionierbares Hauptfenster gefunden.");
                    return;
                }

                var screens = System.Windows.Forms.Screen.AllScreens;
                var screen = settings.MonitorIndex >= 0 && settings.MonitorIndex < screens.Length
                    ? screens[settings.MonitorIndex]
                    : System.Windows.Forms.Screen.PrimaryScreen;
                if (screen == null)
                {
                    logger.LogWarning("StartProcessStepHandler: Kein Monitor verfügbar.");
                    return;
                }

                if (settings.MonitorIndex < 0 || settings.MonitorIndex >= screens.Length)
                    logger.LogWarning(
                        "StartProcessStepHandler: Monitor {MonitorIndex} existiert nicht; verwende den primären Monitor.",
                        settings.MonitorIndex);

                if (settings.WindowMode == StartProcessWindowMode.Normal)
                    ShowWindow(hwnd, SW_RESTORE);

                if (!GetWindowRect(hwnd, out var windowRect))
                {
                    logger.LogWarning(
                        "StartProcessStepHandler: Fenstergröße konnte nicht ermittelt werden (Win32={Error}).",
                        Marshal.GetLastWin32Error());
                    return;
                }

                var area = screen.WorkingArea;
                var width = Math.Max(1, windowRect.Right - windowRect.Left);
                var height = Math.Max(1, windowRect.Bottom - windowRect.Top);
                var desiredX = settings.PlacementMode == StartProcessPlacementMode.Centered
                    ? area.Left + Math.Max(0, (area.Width - width) / 2)
                    : area.Left + settings.OffsetX;
                var desiredY = settings.PlacementMode == StartProcessPlacementMode.Centered
                    ? area.Top + Math.Max(0, (area.Height - height) / 2)
                    : area.Top + settings.OffsetY;
                var x = Math.Clamp(desiredX, area.Left, Math.Max(area.Left, area.Right - width));
                var y = Math.Clamp(desiredY, area.Top, Math.Max(area.Top, area.Bottom - height));

                if (!SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0,
                        SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW))
                {
                    logger.LogWarning(
                        "StartProcessStepHandler: Fenster konnte nicht auf ({X},{Y}) positioniert werden (Win32={Error}).",
                        x, y, Marshal.GetLastWin32Error());
                }

                if (settings.WindowMode == StartProcessWindowMode.Maximized)
                    ShowWindow(hwnd, SW_MAXIMIZE);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "StartProcessStepHandler: Fensterpositionierung fehlgeschlagen.");
            }
        }
    }
}
