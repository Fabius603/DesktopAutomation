using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace TaskAutomation.Steps;

/// <summary>Gemeinsame Namens- und Fenstertitel-Suche für Prozess-Steps.</summary>
internal static class ProcessWindowMatcher
{
    internal sealed record ProcessWindowMatch(IntPtr Handle, uint OwnerProcessId, string Title);
    private sealed record ProcessTreeEntry(int ParentProcessId, string ProcessName);
    private sealed record WindowAssociation(ProcessWindowMatch Window, IReadOnlyList<int> TargetProcessIds);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    private static readonly object ConsoleAttachmentLock = new();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    public static string NormalizeProcessName(string? processName) =>
        Path.GetFileNameWithoutExtension(processName?.Trim() ?? string.Empty);

    public static bool TitleMatches(string? title, string? titleContains) =>
        string.IsNullOrWhiteSpace(titleContains)
        || (title ?? string.Empty).Contains(titleContains.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool IsForegroundWindow(IntPtr handle) =>
        handle != IntPtr.Zero && GetForegroundWindow() == handle;

    public static bool ForegroundWindowMatches(string? processName, string? titleContains = null)
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;
        GetWindowThreadProcessId(foregroundWindow, out var foregroundProcessId);
        if (foregroundProcessId <= int.MaxValue
            && ProcessIdMatchesName((int)foregroundProcessId, processName)
            && TitleMatches(ReadWindowTitle(foregroundWindow), titleContains))
            return true;

        foreach (var window in FindMatchingWindows(processName, titleContains))
            if (window.Handle == foregroundWindow)
                return true;
        return false;
    }

    public static bool ForegroundWindowMatches(int processId, string? titleContains = null)
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;
        return FindMatchingWindows(processId, titleContains).Any(window => window.Handle == foregroundWindow);
    }

    public static IReadOnlyList<ProcessWindowMatch> FindMatchingWindows(
        int processId, string? titleContains = null)
    {
        if (processId <= 0) return [];
        string processName;
        try
        {
            using var process = Process.GetProcessById(processId);
            processName = process.ProcessName;
        }
        catch { return []; }

        return FindWindowAssociations(processName, new HashSet<int> { processId })
            .Where(association => association.TargetProcessIds.Contains(processId)
                                  && TitleMatches(association.Window.Title, titleContains))
            .Select(association => association.Window)
            .DistinctBy(window => window.Handle)
            .ToArray();
    }

    public static IReadOnlyList<ProcessWindowMatch> FindMatchingWindows(
        string? processName, string? titleContains = null)
    {
        var result = new Dictionary<IntPtr, ProcessWindowMatch>();
        foreach (var association in FindWindowAssociations(processName))
            if (TitleMatches(association.Window.Title, titleContains))
                result[association.Window.Handle] = association.Window;
        return [.. result.Values];
    }

    public static IReadOnlyList<int> FindMatchingProcessIds(string? processName, string? titleContains)
    {
        var normalizedName = NormalizeProcessName(processName);
        if (string.IsNullOrWhiteSpace(normalizedName)) return [];

        var processes = Process.GetProcessesByName(normalizedName);
        try
        {
            var ids = new HashSet<int>();
            foreach (var process in processes)
                ids.Add(process.Id);

            if (string.IsNullOrWhiteSpace(titleContains))
                return [.. ids];

            var matchingIds = new HashSet<int>();
            foreach (var association in FindWindowAssociations(normalizedName, ids))
            {
                if (!TitleMatches(association.Window.Title, titleContains)) continue;
                foreach (var targetProcessId in association.TargetProcessIds)
                    matchingIds.Add(targetProcessId);
            }
            return [.. matchingIds];
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }

    private static IReadOnlyList<WindowAssociation> FindWindowAssociations(
        string? processName, IReadOnlySet<int>? knownTargetProcessIds = null)
    {
        var normalizedName = NormalizeProcessName(processName);
        if (string.IsNullOrWhiteSpace(normalizedName)) return [];

        HashSet<int> targetProcessIds;
        if (knownTargetProcessIds is null)
        {
            var processes = Process.GetProcessesByName(normalizedName);
            try
            {
                targetProcessIds = [.. processes.Select(process => process.Id)];
            }
            finally
            {
                foreach (var process in processes) process.Dispose();
            }
        }
        else
        {
            targetProcessIds = [.. knownTargetProcessIds];
        }
        if (targetProcessIds.Count == 0) return [];

        var processTree = CaptureProcessTree();
        var consoleWindows = new Dictionary<int, IntPtr>();
        foreach (var targetProcessId in targetProcessIds)
            consoleWindows[targetProcessId] = TryGetConsoleWindow(targetProcessId);

        var associations = new List<WindowAssociation>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            GetWindowThreadProcessId(hwnd, out var ownerProcessId);
            if (ownerProcessId > int.MaxValue) return true;
            var title = ReadWindowTitle(hwnd);
            var isKnownHostWindow = processTree.TryGetValue((int)ownerProcessId, out var owner)
                                    && IsKnownConsoleHost(owner.ProcessName);

            var matchingTargets = new List<int>();
            foreach (var targetProcessId in targetProcessIds)
            {
                if ((int)ownerProcessId == targetProcessId
                    || consoleWindows[targetProcessId] == hwnd
                    || IsHostedByConsoleProcess(targetProcessId, (int)ownerProcessId, processTree)
                    || (isKnownHostWindow && TitleSuggestsHostedProcess(title, normalizedName)))
                    matchingTargets.Add(targetProcessId);
            }
            if (matchingTargets.Count == 0) return true;

            associations.Add(new WindowAssociation(
                new ProcessWindowMatch(hwnd, ownerProcessId, title), matchingTargets));
            return true;
        }, IntPtr.Zero);
        return associations;
    }

    private static Dictionary<int, ProcessTreeEntry> CaptureProcessTree()
    {
        var result = new Dictionary<int, ProcessTreeEntry>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, ParentProcessId, Name FROM Win32_Process");
            using var processes = searcher.Get();
            foreach (ManagementObject process in processes)
            {
                var processId = checked((int)(uint)process["ProcessId"]);
                var parentProcessId = checked((int)(uint)process["ParentProcessId"]);
                result[processId] = new ProcessTreeEntry(
                    parentProcessId, NormalizeProcessName(Convert.ToString(process["Name"])));
            }
        }
        catch
        {
            // Direkte PID- und Konsolenfenster-Zuordnung funktionieren weiterhin.
        }
        return result;
    }

    private static bool IsHostedByConsoleProcess(
        int targetProcessId, int windowOwnerProcessId,
        IReadOnlyDictionary<int, ProcessTreeEntry> processTree)
    {
        if (!processTree.TryGetValue(windowOwnerProcessId, out var owner)
            || !IsKnownConsoleHost(owner.ProcessName))
            return false;

        return IsAncestor(windowOwnerProcessId, targetProcessId, processTree)
            || IsAncestor(targetProcessId, windowOwnerProcessId, processTree);
    }

    private static bool IsAncestor(
        int ancestorProcessId, int processId,
        IReadOnlyDictionary<int, ProcessTreeEntry> processTree)
    {
        var visited = new HashSet<int>();
        var current = processId;
        while (visited.Add(current) && processTree.TryGetValue(current, out var entry))
        {
            if (entry.ParentProcessId == ancestorProcessId) return true;
            if (entry.ParentProcessId <= 0 || entry.ParentProcessId == current) return false;
            current = entry.ParentProcessId;
        }
        return false;
    }

    private static bool IsKnownConsoleHost(string processName) =>
        processName.Equals("conhost", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("OpenConsole", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("WindowsTerminal", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("wt", StringComparison.OrdinalIgnoreCase);

    private static bool TitleSuggestsHostedProcess(string title, string normalizedProcessName)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(normalizedProcessName))
            return false;

        var executableName = normalizedProcessName + ".exe";
        return title.Equals(normalizedProcessName, StringComparison.OrdinalIgnoreCase)
            || title.Equals(executableName, StringComparison.OrdinalIgnoreCase)
            || title.Contains($@"\{executableName}", StringComparison.OrdinalIgnoreCase)
            || title.Contains($"/{executableName}", StringComparison.OrdinalIgnoreCase)
            || title.StartsWith(executableName + " ", StringComparison.OrdinalIgnoreCase)
            || title.EndsWith(" " + executableName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ProcessIdMatchesName(int processId, string? processName)
    {
        var expectedName = NormalizeProcessName(processName);
        if (string.IsNullOrWhiteSpace(expectedName)) return false;
        try
        {
            using var process = Process.GetProcessById(processId);
            return string.Equals(
                NormalizeProcessName(process.ProcessName), expectedName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException
                                   or InvalidOperationException
                                   or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static IntPtr TryGetConsoleWindow(int processId)
    {
        lock (ConsoleAttachmentLock)
        {
            if (!AttachConsole(checked((uint)processId))) return IntPtr.Zero;
            try
            {
                return GetConsoleWindow();
            }
            finally
            {
                FreeConsole();
            }
        }
    }

    private static string ReadWindowTitle(IntPtr hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0) return string.Empty;
        var text = new StringBuilder(length + 1);
        GetWindowText(hwnd, text, text.Capacity);
        return text.ToString();
    }
}
