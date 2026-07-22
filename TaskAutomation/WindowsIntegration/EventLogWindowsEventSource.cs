using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Push subscriptions to Windows Update, Defender and Firewall operational logs.</summary>
public sealed class EventLogWindowsEventSource : IWindowsEventSource
{
    private readonly ILogger<EventLogWindowsEventSource> _log;
    private readonly List<EventLogWatcher> _watchers = [];
    public event Action<WindowsSystemEvent>? EventReceived;
    public EventLogWindowsEventSource(ILogger<EventLogWindowsEventSource> log) => _log = log;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_watchers.Count > 0) return Task.CompletedTask;
        Watch("Microsoft-Windows-WindowsUpdateClient/Operational", WindowsEventCategory.WindowsUpdate, OnWindowsUpdate);
        Watch("Microsoft-Windows-Windows Defender/Operational", WindowsEventCategory.Security, OnSecurity);
        Watch("Microsoft-Windows-Windows Firewall With Advanced Security/Firewall", WindowsEventCategory.Security, OnSecurity);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var watcher in _watchers) { watcher.Enabled = false; watcher.Dispose(); }
        _watchers.Clear();
        return Task.CompletedTask;
    }

    private void Watch(string logName, WindowsEventCategory category, Func<int, string> classify)
    {
        try
        {
            var watcher = new EventLogWatcher(new EventLogQuery(logName, PathType.LogName, "*[System]"));
            watcher.EventRecordWritten += (_, e) =>
            {
                if (e.EventException is not null) { _log.LogWarning(e.EventException, "Ereignisprotokoll {Log} meldete einen Fehler.", logName); return; }
                using var record = e.EventRecord;
                if (record is null) return;
                var concrete = classify(record.Id);
                var legacy = category == WindowsEventCategory.WindowsUpdate ? "windows_update.changed" : "security.state.changed";
                var data = new Dictionary<string, string?>
                {
                    ["change"] = concrete.Split('.').Last(), ["event_id"] = record.Id.ToString(),
                    ["provider"] = record.ProviderName, ["log"] = logName
                };
                EventReceived?.Invoke(new WindowsSystemEvent(legacy, category, DateTimeOffset.Now, record.RecordId?.ToString(), data));
                EventReceived?.Invoke(new WindowsSystemEvent(concrete, category, DateTimeOffset.Now, record.RecordId?.ToString(), data));
            };
            watcher.Enabled = true;
            _watchers.Add(watcher);
        }
        catch (Exception ex) { _log.LogWarning(ex, "Ereignisprotokoll {Log} konnte nicht abonniert werden.", logName); }
    }

    private static string OnWindowsUpdate(int id) => id switch
    {
        19 => "windows_update.installed", 20 => "windows_update.failed", 21 => "windows_update.restart_required",
        25 or 26 => "windows_update.download_started", 31 or 43 => "windows_update.downloaded",
        _ => "windows_update.changed"
    };

    private static string OnSecurity(int id) => id switch
    {
        1006 or 1116 => "security.threat.detected", 1007 or 1117 => "security.threat.action_taken",
        2000 or 2001 or 5007 => "security.settings.changed", _ => "security.state.changed"
    };
}
