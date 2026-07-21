using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>Bringt das sichtbare Hauptfenster eines laufenden Prozesses in den Vordergrund.</summary>
    public sealed class FocusProcessStepHandler : JobStepHandler<FocusProcessStep, FocusProcessResult>
    {
        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetLastActivePopup(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(uint attach, uint attachTo, bool isAttach);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetActiveWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetFocus(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr hwnd, int index);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private sealed record WindowCandidate(
            IntPtr Handle, uint ProcessId, string Title, long Area, bool IsToolWindow, bool HasOwner);

        private const int SW_RESTORE = 9;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const uint GW_OWNER = 4;
        private const uint GA_ROOTOWNER = 3;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const byte VK_MENU = 0x12;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        protected override async Task<FocusProcessResult> ExecuteCoreAsync(
            FocusProcessStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var processTarget = step.Settings.Target;
            var configuredProgram = processTarget.ExecutablePath;
            var usesRuntimeReference = processTarget.ProcessSource.IsConfigured;
            var configuredPath = string.Empty;
            if (!usesRuntimeReference
                && !ExecutablePathResolver.TryResolve(configuredProgram, out configuredPath))
            {
                ctx.Logger.LogWarning("FocusProcessStepHandler: Programm '{Program}' wurde nicht gefunden.", configuredProgram);
                return Failure($"Programm '{configuredProgram}' wurde nicht gefunden.");
            }

            WindowCandidate? target = null;
            var processFound = false;
            for (var attempt = 0; attempt < 3 && target == null; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                if (usesRuntimeReference)
                {
                    var ids = ProcessTargetResolver.ResolveProcessIds(processTarget, ctx.Results).ToHashSet();
                    processFound = ids.Count > 0;
                    var sourceReference = ProcessTargetResolver.ResolveReference(processTarget, ctx.Results);
                    if (sourceReference is { WindowHandle: not 0 })
                    {
                        var exactWindow = ProcessWindowMatcher.FindMatchingWindows(
                                sourceReference.ProcessId, processTarget.WindowTitleContains)
                            .FirstOrDefault(window => window.Handle.ToInt64() == sourceReference.WindowHandle);
                        if (exactWindow is not null)
                            target = ToWindowCandidate(exactWindow);
                    }
                    target ??= EnumerateCandidateWindows(ids, ctx.Logger)
                        .Where(candidate => ProcessWindowMatcher.TitleMatches(
                            candidate.Title, processTarget.WindowTitleContains))
                        .OrderByDescending(candidate => candidate.Area)
                        .FirstOrDefault();
                    if (target == null && ids.Count == 1)
                    {
                        var processId = ids.First();
                        target = ProcessWindowMatcher.FindMatchingWindows(
                                processId, processTarget.WindowTitleContains)
                            .Select(ToWindowCandidate)
                            .Where(candidate => candidate != null)
                            .Cast<WindowCandidate>()
                            .OrderByDescending(candidate => candidate.Area)
                            .FirstOrDefault();
                    }
                }
                else
                {
                    target = FindBestWindow(
                        configuredPath, processTarget.WindowTitleContains, ctx.Logger, out processFound);
                }
                if (target == null)
                    await Task.Delay(75, ct).ConfigureAwait(false);
            }

            if (target == null)
            {
                var processName = usesRuntimeReference
                    ? $"Step {processTarget.ProcessSource.SourceStepId}"
                    : Path.GetFileNameWithoutExtension(configuredPath);
                var titleText = string.IsNullOrWhiteSpace(processTarget.WindowTitleContains)
                    ? string.Empty
                    : $" mit dem Titelteil '{processTarget.WindowTitleContains}'";
                var error = processFound
                    ? $"Für den Prozess '{processName}' wurde kein sichtbares Fenster{titleText} gefunden."
                    : $"Prozess '{processName}' läuft nicht.";
                ctx.Logger.LogWarning("FocusProcessStepHandler: {Error}", error);
                return Failure(error);
            }

            if (step.Settings.Action == FocusProcessAction.Minimize)
            {
                ShowWindow(target.Handle, SW_MINIMIZE);
                ctx.Logger.LogInformation(
                    "FocusProcessStepHandler: Fenster '{Title}' (PID {Pid}, HWND 0x{Handle:X}) minimiert.",
                    target.Title, target.ProcessId, target.Handle.ToInt64());
                return Success(target);
            }

            var requestedMode = step.Settings.WindowMode == FocusProcessWindowMode.Fullscreen
                ? FocusProcessWindowMode.Maximized
                : step.Settings.WindowMode;
            var command = requestedMode == FocusProcessWindowMode.Maximized ? SW_MAXIMIZE : SW_RESTORE;
            var showResult = ShowWindow(target.Handle, command);
            ctx.Logger.LogDebug(
                "FocusProcessStepHandler: ShowWindow({Command})={Result} für HWND 0x{Handle:X}.",
                command, showResult, target.Handle.ToInt64());

            var focusResult = await TryActivateWindowAsync(target.Handle, ctx.Logger, ct).ConfigureAwait(false);
            if (!focusResult.Success)
            {
                ctx.Logger.LogWarning(
                    "FocusProcessStepHandler: Fenster '{Title}' (PID {Pid}, HWND 0x{Handle:X}) nicht aktiviert: {Error}",
                    target.Title, target.ProcessId, target.Handle.ToInt64(), focusResult.Error);
                return Failure(focusResult.Error);
            }

            ctx.Logger.LogInformation(
                "FocusProcessStepHandler: Fenster '{Title}' (PID {Pid}, HWND 0x{Handle:X}) aktiviert.",
                target.Title, target.ProcessId, target.Handle.ToInt64());
            return Success(target);
        }

        protected override FocusProcessResult CreateDefault() => FocusProcessResult.Default;

        private static FocusProcessResult Success(WindowCandidate target)
        {
            var process = ProcessTargetResolver.CreateReference(checked((int)target.ProcessId));
            return new FocusProcessResult
            {
                WasExecuted = true,
                Success = true,
                Process = process is null ? null : process with { WindowHandle = target.Handle.ToInt64() }
            };
        }

        private static FocusProcessResult Failure(string message) => new()
        {
            WasExecuted = true,
            Success = false,
            ErrorMessage = message
        };

        private static WindowCandidate? ToWindowCandidate(ProcessWindowMatcher.ProcessWindowMatch window)
        {
            if (!GetWindowRect(window.Handle, out var rect)) return null;
            var width = Math.Max(0, rect.Right - rect.Left);
            var height = Math.Max(0, rect.Bottom - rect.Top);
            if (width == 0 || height == 0) return null;
            var exStyle = GetWindowLongPtr(window.Handle, GWL_EXSTYLE).ToInt64();
            return new WindowCandidate(
                window.Handle, window.OwnerProcessId, window.Title, (long)width * height,
                (exStyle & WS_EX_TOOLWINDOW) != 0,
                GetWindow(window.Handle, GW_OWNER) != IntPtr.Zero);
        }

        private static WindowCandidate? FindBestWindow(
            string configuredPath, string? windowTitleContains, ILogger logger, out bool processFound)
        {
            var processName = Path.GetFileNameWithoutExtension(configuredPath);
            var processes = Process.GetProcessesByName(processName);
            try
            {
                processFound = processes.Length > 0;
                if (!processFound) return null;

                var exactPathIds = processes
                    .Where(p => HasExecutablePath(p, configuredPath))
                    .Select(p => p.Id)
                    .ToHashSet();
                var rootIds = exactPathIds.Count > 0
                    ? exactPathIds
                    : processes.Select(p => p.Id).ToHashSet();
                var candidates = EnumerateCandidateWindows(rootIds, logger);
                if (candidates.Count == 0)
                    candidates = EnumerateHostedWindows(configuredPath, processName, logger);
                if (candidates.Count == 0)
                {
                    var processIds = IncludeDescendantProcesses(rootIds, logger);
                    candidates = EnumerateCandidateWindows(processIds, logger);
                }
                candidates = candidates
                    .Where(candidate => ProcessWindowMatcher.TitleMatches(candidate.Title, windowTitleContains))
                    .ToList();
                if (candidates.Count == 0)
                    candidates = EnumerateAssociatedWindows(processName, windowTitleContains);
                if (candidates.Count == 0) return null;

                var foreground = GetForegroundWindow();
                var foregroundRoot = foreground == IntPtr.Zero ? IntPtr.Zero : GetAncestor(foreground, GA_ROOTOWNER);
                return candidates
                    .OrderByDescending(c => c.Handle == foreground || GetAncestor(c.Handle, GA_ROOTOWNER) == foregroundRoot)
                    .ThenBy(c => c.IsToolWindow)
                    .ThenBy(c => c.HasOwner)
                    .ThenByDescending(c => c.Area)
                    .ThenByDescending(c => !string.IsNullOrWhiteSpace(c.Title))
                    .ThenBy(c => c.ProcessId)
                    .First();
            }
            finally
            {
                foreach (var process in processes) process.Dispose();
            }
        }

        private static bool HasExecutablePath(Process process, string configuredPath)
        {
            try
            {
                var actual = process.MainModule?.FileName;
                return actual != null && string.Equals(
                    Path.GetFullPath(actual), Path.GetFullPath(configuredPath), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static HashSet<int> IncludeDescendantProcesses(HashSet<int> rootIds, ILogger logger)
        {
            var result = new HashSet<int>(rootIds);
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, ParentProcessId FROM Win32_Process");
                var relations = searcher.Get().Cast<ManagementObject>()
                    .Select(item => (
                        ProcessId: Convert.ToInt32((uint)item["ProcessId"]),
                        ParentId: Convert.ToInt32((uint)item["ParentProcessId"])))
                    .ToList();

                var changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var relation in relations)
                    {
                        if (result.Contains(relation.ParentId) && result.Add(relation.ProcessId))
                            changed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "FocusProcessStepHandler: Kindprozesse konnten nicht ermittelt werden.");
            }
            return result;
        }

        private static List<WindowCandidate> EnumerateCandidateWindows(
            HashSet<int> processIds, ILogger logger)
        {
            var byHandle = new Dictionary<IntPtr, WindowCandidate>();
            var enumResult = EnumWindows((hwnd, _) =>
            {
                if (!IsWindowVisible(hwnd)) return true;
                GetWindowThreadProcessId(hwnd, out var pid);
                if (!processIds.Contains((int)pid)) return true;

                var root = GetAncestor(hwnd, GA_ROOTOWNER);
                if (root == IntPtr.Zero) root = hwnd;
                var popup = GetLastActivePopup(root);
                var effective = popup != IntPtr.Zero && IsWindowVisible(popup) ? popup : hwnd;
                GetWindowThreadProcessId(effective, out pid);
                if (!processIds.Contains((int)pid) || !IsWindowVisible(effective)) return true;

                GetWindowRect(effective, out var rect);
                var width = Math.Max(0, rect.Right - rect.Left);
                var height = Math.Max(0, rect.Bottom - rect.Top);
                var exStyle = GetWindowLongPtr(effective, GWL_EXSTYLE).ToInt64();
                byHandle[effective] = new WindowCandidate(
                    effective,
                    pid,
                    ReadWindowTitle(effective),
                    (long)width * height,
                    (exStyle & WS_EX_TOOLWINDOW) != 0,
                    GetWindow(effective, GW_OWNER) != IntPtr.Zero);
                return true;
            }, IntPtr.Zero);

            if (!enumResult)
                logger.LogWarning(
                    "FocusProcessStepHandler: EnumWindows fehlgeschlagen (Win32={Error}).",
                    Marshal.GetLastWin32Error());
            return byHandle.Values.Where(c => c.Area > 0).ToList();
        }

        private static List<WindowCandidate> EnumerateHostedWindows(
            string configuredPath, string processName, ILogger logger)
        {
            var candidates = new List<WindowCandidate>();
            var executableName = Path.GetFileName(configuredPath);
            var enumResult = EnumWindows((hwnd, _) =>
            {
                if (!IsWindowVisible(hwnd)) return true;
                var title = ReadWindowTitle(hwnd);
                if (string.IsNullOrWhiteSpace(title)
                    || !TitleMatchesProgram(title, configuredPath, executableName, processName))
                    return true;

                if (!GetWindowRect(hwnd, out var rect)) return true;
                var width = Math.Max(0, rect.Right - rect.Left);
                var height = Math.Max(0, rect.Bottom - rect.Top);
                if (width == 0 || height == 0) return true;

                GetWindowThreadProcessId(hwnd, out var pid);
                var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
                candidates.Add(new WindowCandidate(
                    hwnd, pid, title, (long)width * height,
                    (exStyle & WS_EX_TOOLWINDOW) != 0,
                    GetWindow(hwnd, GW_OWNER) != IntPtr.Zero));
                return true;
            }, IntPtr.Zero);

            if (!enumResult)
                logger.LogWarning(
                    "FocusProcessStepHandler: Fenstersuche nach Programmtitel fehlgeschlagen (Win32={Error}).",
                    Marshal.GetLastWin32Error());
            return candidates;
        }

        private static List<WindowCandidate> EnumerateAssociatedWindows(
            string processName, string? windowTitleContains)
        {
            var candidates = new List<WindowCandidate>();
            foreach (var window in ProcessWindowMatcher.FindMatchingWindows(processName, windowTitleContains))
            {
                if (!GetWindowRect(window.Handle, out var rect)) continue;
                var width = Math.Max(0, rect.Right - rect.Left);
                var height = Math.Max(0, rect.Bottom - rect.Top);
                if (width == 0 || height == 0) continue;

                var exStyle = GetWindowLongPtr(window.Handle, GWL_EXSTYLE).ToInt64();
                candidates.Add(new WindowCandidate(
                    window.Handle,
                    window.OwnerProcessId,
                    window.Title,
                    (long)width * height,
                    (exStyle & WS_EX_TOOLWINDOW) != 0,
                    GetWindow(window.Handle, GW_OWNER) != IntPtr.Zero));
            }
            return candidates;
        }

        private static bool TitleMatchesProgram(
            string title, string configuredPath, string executableName, string processName)
        {
            if (title.Equals(configuredPath, StringComparison.OrdinalIgnoreCase)
                || title.Equals(executableName, StringComparison.OrdinalIgnoreCase)
                || title.Equals(processName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Windows Terminal commonly uses the hosted console command as its tab/window title.
            return title.Contains(configuredPath, StringComparison.OrdinalIgnoreCase)
                   || title.Contains($@"\{executableName}", StringComparison.OrdinalIgnoreCase);
        }

        private static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
            => IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index) : new IntPtr(GetWindowLong32(hwnd, index));

        private static string ReadWindowTitle(IntPtr hwnd)
        {
            var length = GetWindowTextLength(hwnd);
            if (length <= 0) return string.Empty;
            var text = new StringBuilder(length + 1);
            GetWindowText(hwnd, text, text.Capacity);
            return text.ToString();
        }

        private static async Task<(bool Success, string Error)> TryActivateWindowAsync(
            IntPtr hwnd, ILogger logger, CancellationToken ct)
        {
            if (!IsWindow(hwnd)) return (false, "Das ausgewählte Fenster existiert nicht mehr.");
            hwnd = ResolveActivePopup(hwnd);
            if (IsTargetForeground(hwnd)) return (true, string.Empty);

            var foreground = GetForegroundWindow();
            var currentThread = GetCurrentThreadId();
            var foregroundThread = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out _);
            var targetThread = GetWindowThreadProcessId(hwnd, out _);
            var attachedForeground = false;
            var attachedTarget = false;

            try
            {
                attachedForeground = foregroundThread != 0 && foregroundThread != currentThread
                    && AttachThreadInput(currentThread, foregroundThread, true);
                attachedTarget = targetThread != 0 && targetThread != currentThread
                    && AttachThreadInput(currentThread, targetThread, true);

                var broughtToTop = BringWindowToTop(hwnd);
                var positionedAtTop = SetWindowPos(
                    hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                var foregroundSet = SetForegroundWindow(hwnd);
                SetActiveWindow(hwnd);
                SetFocus(hwnd);
                var activeSet = GetActiveWindow() == hwnd;
                var focusSet = GetFocus() == hwnd;
                logger.LogDebug(
                    "FocusProcessStepHandler: Aktivierung HWND 0x{Handle:X}: AttachForeground={AttachForeground}, AttachTarget={AttachTarget}, BringToTop={Bring}, SetWindowPos={Position}, SetForeground={Foreground}, SetActive={Active}, SetFocus={Focus}.",
                    hwnd.ToInt64(), attachedForeground, attachedTarget, broughtToTop, positionedAtTop,
                    foregroundSet, activeSet, focusSet);
            }
            finally
            {
                if (attachedTarget) AttachThreadInput(currentThread, targetThread, false);
                if (attachedForeground) AttachThreadInput(currentThread, foregroundThread, false);
            }

            if (await WaitForForegroundAsync(hwnd, ct).ConfigureAwait(false))
                return (true, string.Empty);

            // Windows erlaubt SetForegroundWindow nach einer Benutzereingabe aus dem aufrufenden Thread.
            // Ein leerer Alt-Tastendruck ist der etablierte, minimale Fallback ohne Text-/Mauseingabe.
            keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
            keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            var fallbackResult = SetForegroundWindow(hwnd);
            logger.LogDebug(
                "FocusProcessStepHandler: Alt-Fallback SetForegroundWindow={Result} für HWND 0x{Handle:X}.",
                fallbackResult, hwnd.ToInt64());

            if (await WaitForForegroundAsync(hwnd, ct).ConfigureAwait(false))
                return (true, string.Empty);

            return (false,
                "Windows hat die Aktivierung abgelehnt. Mögliche Ursache: Das Ziel läuft mit höheren Rechten oder auf einem anderen Desktop.");
        }

        private static IntPtr ResolveActivePopup(IntPtr hwnd)
        {
            var root = GetAncestor(hwnd, GA_ROOTOWNER);
            if (root == IntPtr.Zero) root = hwnd;
            var popup = GetLastActivePopup(root);
            return popup != IntPtr.Zero && IsWindowVisible(popup) ? popup : root;
        }

        private static async Task<bool> WaitForForegroundAsync(IntPtr hwnd, CancellationToken ct)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                if (IsTargetForeground(hwnd)) return true;
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
            return false;
        }

        private static bool IsTargetForeground(IntPtr hwnd)
        {
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            var targetRoot = GetAncestor(hwnd, GA_ROOTOWNER);
            var foregroundRoot = GetAncestor(foreground, GA_ROOTOWNER);
            return foreground == hwnd || (targetRoot != IntPtr.Zero && targetRoot == foregroundRoot);
        }
    }
}
