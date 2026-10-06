using System.Management;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Owns process event registration and the non-administrator WMI fallback for both consumers.</summary>
internal static class ProcessEventWatcher
{
    public static ManagementEventWatcher Start(bool started, EventArrivedEventHandler handler, Action fallback)
        => StartWithFallback(started, query =>
        {
            var watcher = new ManagementEventWatcher(new WqlEventQuery(query));
            watcher.EventArrived += handler;
            try { watcher.Start(); return watcher; }
            catch { watcher.Dispose(); throw; }
        }, fallback);

    internal static T StartWithFallback<T>(bool started, Func<string, T> start, Action fallback)
    {
        try { return start($"SELECT * FROM Win32_Process{(started ? "Start" : "Stop")}Trace"); }
        catch (Exception error) when (error is UnauthorizedAccessException
            || error is ManagementException { ErrorCode: ManagementStatus.AccessDenied or ManagementStatus.InvalidClass })
        {
            var result = start($"SELECT * FROM __Instance{(started ? "Creation" : "Deletion")}Event WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'");
            fallback();
            return result;
        }
    }

    public static ManagementBaseObject Process(EventArrivedEventArgs args)
        => Property(args.NewEvent, "TargetInstance") is ManagementBaseObject process ? process : args.NewEvent;

    public static string? Name(ManagementBaseObject process)
        => Convert.ToString(Property(process, "ProcessName") ?? Property(process, "Name"));

    private static object? Property(ManagementBaseObject value, string name)
        => value.Properties.Cast<PropertyData>().FirstOrDefault(property => property.Name == name)?.Value;
}
