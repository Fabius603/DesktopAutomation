using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Push subscriptions to Windows Update, Defender and Firewall operational logs.</summary>
public sealed class EventLogWindowsEventSource : IWindowsEventSource
{
    internal sealed record SourceDefinition(
        string LogName,
        WindowsEventCategory Category,
        Func<int, string> Classify,
        string Query = "*[System]");

    internal static IReadOnlyList<SourceDefinition> SourceDefinitions { get; } =
    [
        new("System", WindowsEventCategory.WindowsUpdate, OnWindowsUpdate,
            "*[System[Provider[@Name='Microsoft-Windows-WindowsUpdateClient']]]"),
        new("Microsoft-Windows-WindowsUpdateClient/Operational", WindowsEventCategory.WindowsUpdate, OnWindowsUpdate),
        new("Microsoft-Windows-Windows Defender/Operational", WindowsEventCategory.Security, OnDefenderSecurity),
        new("Microsoft-Windows-Windows Firewall With Advanced Security/Firewall", WindowsEventCategory.Security, OnFirewallSecurity)
    ];

    private readonly ILogger<EventLogWindowsEventSource> _log;
    private readonly List<EventLogWatcher> _watchers = [];
    public event Action<WindowsSystemEvent>? EventReceived;
    public EventLogWindowsEventSource(ILogger<EventLogWindowsEventSource> log) => _log = log;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_watchers.Count > 0) return Task.CompletedTask;
        foreach (var source in SourceDefinitions)
            Watch(source);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var watcher in _watchers) { watcher.Enabled = false; watcher.Dispose(); }
        _watchers.Clear();
        return Task.CompletedTask;
    }

    private void Watch(SourceDefinition source)
    {
        try
        {
            var watcher = new EventLogWatcher(new EventLogQuery(source.LogName, PathType.LogName, source.Query));
            watcher.EventRecordWritten += (_, e) =>
            {
                if (e.EventException is not null) { _log.LogWarning(e.EventException, "Ereignisprotokoll {Log} meldete einen Fehler.", source.LogName); return; }
                using var record = e.EventRecord;
                if (record is null) return;
                var concrete = source.Classify(record.Id);
                var data = new Dictionary<string, string?>
                {
                    ["change"] = concrete.Split('.').Last(), ["event_id"] = record.Id.ToString(),
                    ["provider"] = record.ProviderName, ["log"] = source.LogName
                };
                foreach (var eventType in GetEventTypes(source.Category, concrete))
                    EventReceived?.Invoke(new WindowsSystemEvent(eventType, source.Category, DateTimeOffset.Now, record.RecordId?.ToString(), data));
            };
            watcher.Enabled = true;
            _watchers.Add(watcher);
        }
        catch (Exception ex) { _log.LogWarning(ex, "Ereignisprotokoll {Log} konnte nicht abonniert werden.", source.LogName); }
    }

    internal static string OnWindowsUpdate(int id) => id switch
    {
        19 => "windows_update.installed", 20 => "windows_update.failed", 21 => "windows_update.restart_required",
        25 or 31 => "windows_update.failed", 41 => "windows_update.downloaded", 44 => "windows_update.download_started",
        _ => "windows_update.changed"
    };

    internal static string OnDefenderSecurity(int id) => id switch
    {
        1006 or 1116 => "security.threat.detected", 1007 or 1117 => "security.threat.action_taken",
        5004 or 5007 => "security.settings.changed", _ => "security.state.changed"
    };

    internal static string OnFirewallSecurity(int id) => id switch
    {
        2002 or 2003 or 2004 or 2005 or 2006 or 2008 or 2032 or 2033 => "security.settings.changed",
        _ => "security.state.changed"
    };

    internal static IReadOnlyList<string> GetEventTypes(WindowsEventCategory category, string concrete)
    {
        var legacy = category == WindowsEventCategory.WindowsUpdate ? "windows_update.changed" : "security.state.changed";
        return string.Equals(legacy, concrete, StringComparison.OrdinalIgnoreCase)
            ? [legacy]
            : [legacy, concrete];
    }
}
