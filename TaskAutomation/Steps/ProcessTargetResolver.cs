using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

internal static class ProcessTargetResolver
{
    public static IReadOnlyList<int> ResolveProcessIds(ProcessTargetSettings target, IJobResultStore results)
    {
        if (target.ProcessSource.IsConfigured)
        {
            var source = ResolveReference(target, results);
            return IsCurrent(source) ? [source!.ProcessId] : [];
        }

        var processName = !string.IsNullOrWhiteSpace(target.ProcessName)
            ? target.ProcessName
            : Path.GetFileNameWithoutExtension(target.ExecutablePath);
        var ids = ProcessWindowMatcher.FindMatchingProcessIds(processName, target.WindowTitleContains);
        if (string.IsNullOrWhiteSpace(target.ExecutablePath)) return ids;
        return ids.Where(id => ExecutablePathMatches(id, target.ExecutablePath)).ToArray();
    }

    public static RuntimeProcessReference? ResolveReference(
        ProcessTargetSettings target, IJobResultStore results)
    {
        var binding = target.ProcessSource;
        if (!binding.IsConfigured) return null;
        var reference = ResultBindingResolver.Resolve<RuntimeProcessReference>(results, binding).FirstOrDefault;
        return IsCurrent(reference) ? reference : null;
    }

    public static RuntimeProcessReference? CreateReference(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return CreateReference(process);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
                                   or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    public static RuntimeProcessReference CreateReference(Process process, string? knownPath = null)
    {
        var processId = process.Id;
        string path = knownPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            try { path = process.MainModule?.FileName ?? string.Empty; }
            catch { }
        }
        long windowHandle = 0;
        try
        {
            process.Refresh();
            windowHandle = process.MainWindowHandle.ToInt64();
        }
        catch { }
        if (windowHandle == 0)
            windowHandle = ProcessWindowMatcher.FindMatchingWindows(processId).FirstOrDefault()?.Handle.ToInt64() ?? 0;

        DateTime startTimeUtc;
        try { startTimeUtc = process.StartTime.ToUniversalTime(); }
        catch { startTimeUtc = DateTime.UtcNow; }
        string processName;
        try { processName = process.ProcessName; }
        catch { processName = Path.GetFileNameWithoutExtension(path); }

        return new RuntimeProcessReference
        {
            ProcessId = processId,
            StartTimeUtc = startTimeUtc,
            ProcessName = processName,
            ExecutablePath = path,
            WindowHandle = windowHandle
        };
    }

    public static bool IsCurrent(RuntimeProcessReference? reference)
    {
        if (reference is null || reference.ProcessId <= 0) return false;
        try
        {
            using var process = Process.GetProcessById(reference.ProcessId);
            return !process.HasExited
                && Math.Abs((process.StartTime.ToUniversalTime() - reference.StartTimeUtc).TotalSeconds) < 1;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
                                   or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static bool ExecutablePathMatches(int processId, string configuredPath)
    {
        try
        {
            if (!ExecutablePathResolver.TryResolve(configuredPath, out var expected)) return false;
            using var process = Process.GetProcessById(processId);
            var actual = process.MainModule?.FileName;
            return actual != null && string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected),
                StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

}
