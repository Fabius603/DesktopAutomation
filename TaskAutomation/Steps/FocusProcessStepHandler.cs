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
    /// Bringt das Hauptfenster eines laufenden Prozesses in den Vordergrund.
    /// Wenn kein passender Prozess gefunden wird oder das Fenster kein Handle hat,
    /// wird nur geloggt – es wird kein Fehler zurückgegeben.
    /// </summary>
    public sealed class FocusProcessStepHandler : JobStepHandler<FocusProcessStep, TaskResult>
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE   = 9;
        private const int SW_MAXIMIZE   = 3;
        private const int SW_SHOWMAXIMIZED = 3;

        protected override Task<TaskResult> ExecuteCoreAsync(
            FocusProcessStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            var executablePath = step.Settings.ExecutablePath?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(executablePath))
            {
                logger.LogWarning("FocusProcessStepHandler: Kein Pfad zur ausführbaren Datei konfiguriert.");
                return Task.FromResult(new TaskResult { WasExecuted = true, Success = false,
                    ErrorMessage = "Kein Pfad konfiguriert." });
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            var processes = Process.GetProcessesByName(processName);

            if (processes.Length == 0)
            {
                logger.LogInformation(
                    "FocusProcessStepHandler: Kein laufender Prozess mit dem Namen '{Name}' gefunden.",
                    processName);
                foreach (var p in processes) p.Dispose();
                return Task.FromResult(new TaskResult { WasExecuted = true, Success = true });
            }

            var target = processes[0];
            var hwnd = target.MainWindowHandle;

            if (hwnd == IntPtr.Zero)
            {
                logger.LogInformation(
                    "FocusProcessStepHandler: Prozess '{Name}' hat kein sichtbares Hauptfenster.",
                    processName);
            }
            else
            {
                switch (step.Settings.WindowMode)
                {
                    case FocusProcessWindowMode.Maximized:
                        ShowWindow(hwnd, SW_MAXIMIZE);
                        break;
                    case FocusProcessWindowMode.Fullscreen:
                        // Vollbild: maximieren und dann in den Vordergrund bringen
                        ShowWindow(hwnd, SW_MAXIMIZE);
                        break;
                    default:
                        // Normal: nur wiederherstellen wenn minimiert
                        if (IsIconic(hwnd))
                            ShowWindow(hwnd, SW_RESTORE);
                        break;
                }
                SetForegroundWindow(hwnd);
                logger.LogInformation(
                    "FocusProcessStepHandler: Prozess '{Name}' in den Vordergrund gebracht.",
                    processName);
            }

            foreach (var p in processes) p.Dispose();
            return Task.FromResult(new TaskResult { WasExecuted = true, Success = true });
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
    }
}
