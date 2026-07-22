using System.Management;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Low-latency events exposed by .NET's Windows notification bridge.</summary>
public sealed class NativeWindowsEventSource : IWindowsEventSource
{
    public event Action<WindowsSystemEvent>? EventReceived;
    private bool _started;
    private Dictionary<string, DisplayState> _displayDevices = new(StringComparer.OrdinalIgnoreCase);
    private System.Windows.Forms.PowerLineStatus _powerLineStatus;
    private bool _charging;
    private float _batteryPercentage;
    private string _timeZoneId = string.Empty;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started) return Task.CompletedTask;
        _started = true;
        _displayDevices = CurrentDisplays();
        var power = System.Windows.Forms.SystemInformation.PowerStatus;
        _powerLineStatus = power.PowerLineStatus;
        _charging = power.BatteryChargeStatus.HasFlag(System.Windows.Forms.BatteryChargeStatus.Charging);
        _batteryPercentage = power.BatteryLifePercent;
        _timeZoneId = TimeZoneInfo.Local.Id;
        NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChanged;
        NetworkChange.NetworkAddressChanged += NetworkAddressChanged;
        SystemEvents.PowerModeChanged += PowerModeChanged;
        SystemEvents.SessionSwitch += SessionSwitch;
        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
        SystemEvents.TimeChanged += TimeChanged;
        SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
        SystemEvents.SessionEnding += SessionEnding;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_started) return Task.CompletedTask;
        _started = false;
        NetworkChange.NetworkAvailabilityChanged -= NetworkAvailabilityChanged;
        NetworkChange.NetworkAddressChanged -= NetworkAddressChanged;
        SystemEvents.PowerModeChanged -= PowerModeChanged;
        SystemEvents.SessionSwitch -= SessionSwitch;
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        SystemEvents.TimeChanged -= TimeChanged;
        SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;
        SystemEvents.SessionEnding -= SessionEnding;
        return Task.CompletedTask;
    }

    private void Emit(string type, WindowsEventCategory category, IReadOnlyDictionary<string, string?>? data = null) => EventReceived?.Invoke(new WindowsSystemEvent(type, category, DateTimeOffset.Now, Data: data));
    private void EmitBoth(string legacy, string concrete, WindowsEventCategory category, Dictionary<string, string?> data)
    { Emit(legacy, category, data); if (!string.Equals(legacy, concrete, StringComparison.OrdinalIgnoreCase)) Emit(concrete, category, data); }
    private void NetworkAvailabilityChanged(object? s, NetworkAvailabilityEventArgs e)
    {
        var data = new Dictionary<string, string?> { ["available"] = e.IsAvailable.ToString(), ["state"] = e.IsAvailable ? "connected" : "disconnected" };
        EmitBoth("network.availability.changed", e.IsAvailable ? "network.connected" : "network.disconnected", WindowsEventCategory.Network, data);
    }
    private void NetworkAddressChanged(object? s, EventArgs e) => Emit("network.address.changed", WindowsEventCategory.Network);
    private void PowerModeChanged(object s, PowerModeChangedEventArgs e)
    {
        var power = System.Windows.Forms.SystemInformation.PowerStatus;
        var data = new Dictionary<string, string?>
        {
            ["mode"] = e.Mode.ToString(), ["power_line"] = power.PowerLineStatus.ToString(),
            ["charging"] = power.BatteryChargeStatus.HasFlag(System.Windows.Forms.BatteryChargeStatus.Charging).ToString(),
            ["battery_percentage"] = Math.Round(power.BatteryLifePercent * 100d, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        Emit("power.mode.changed", WindowsEventCategory.Power, data);
        if (e.Mode == PowerModes.Suspend) Emit("power.suspended", WindowsEventCategory.Power, data);
        else if (e.Mode == PowerModes.Resume) Emit("power.resumed", WindowsEventCategory.Power, data);
        else
        {
            if (_powerLineStatus != power.PowerLineStatus)
                Emit(power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online ? "power.ac_connected" : "power.ac_disconnected", WindowsEventCategory.Power, data);
            var charging = power.BatteryChargeStatus.HasFlag(System.Windows.Forms.BatteryChargeStatus.Charging);
            if (_charging != charging) Emit(charging ? "power.charging_started" : "power.charging_stopped", WindowsEventCategory.Power, data);
            if (Math.Abs(_batteryPercentage - power.BatteryLifePercent) > 0.0001f) Emit("power.battery_level_changed", WindowsEventCategory.Power, data);
            Emit("power.status.changed", WindowsEventCategory.Power, data);
        }
        _powerLineStatus = power.PowerLineStatus;
        _charging = power.BatteryChargeStatus.HasFlag(System.Windows.Forms.BatteryChargeStatus.Charging);
        _batteryPercentage = power.BatteryLifePercent;
    }
    private void SessionSwitch(object s, SessionSwitchEventArgs e)
    {
        var concrete = e.Reason switch
        {
            SessionSwitchReason.SessionLock => "session.locked", SessionSwitchReason.SessionUnlock => "session.unlocked",
            SessionSwitchReason.SessionLogon => "session.logged_on", SessionSwitchReason.SessionLogoff => "session.logged_off",
            SessionSwitchReason.RemoteConnect => "session.remote_connected", SessionSwitchReason.RemoteDisconnect => "session.remote_disconnected",
            SessionSwitchReason.ConsoleConnect => "session.console_connected", SessionSwitchReason.ConsoleDisconnect => "session.console_disconnected",
            _ => "session.state.changed"
        };
        EmitBoth("session.state.changed", concrete, WindowsEventCategory.Session, new() { ["reason"] = e.Reason.ToString() });
    }
    private void DisplaySettingsChanged(object? s, EventArgs e)
    {
        var current = CurrentDisplays();
        var added = current.Keys.Except(_displayDevices.Keys, StringComparer.OrdinalIgnoreCase).ToArray();
        var removed = _displayDevices.Keys.Except(current.Keys, StringComparer.OrdinalIgnoreCase).ToArray();
        var common = current.Keys.Intersect(_displayDevices.Keys, StringComparer.OrdinalIgnoreCase).ToArray();
        var resolutionChanged = common.Any(id => current[id].Width != _displayDevices[id].Width || current[id].Height != _displayDevices[id].Height);
        var orientationChanged = common.Any(id => current[id].IsPortrait != _displayDevices[id].IsPortrait);
        var primaryChanged = common.Any(id => current[id].Primary != _displayDevices[id].Primary);
        var data = new Dictionary<string, string?> { ["added"] = string.Join(",", added), ["removed"] = string.Join(",", removed) };
        _displayDevices = current;
        Emit("display.settings.changed", WindowsEventCategory.Display, data);
        if (added.Length > 0) Emit("display.connected", WindowsEventCategory.Display, data);
        if (removed.Length > 0) Emit("display.disconnected", WindowsEventCategory.Display, data);
        if (orientationChanged) Emit("display.orientation_changed", WindowsEventCategory.Display, data);
        if (resolutionChanged) Emit("display.resolution_changed", WindowsEventCategory.Display, data);
        if (primaryChanged) Emit("display.primary_changed", WindowsEventCategory.Display, data);
        if (added.Length == 0 && removed.Length == 0 && !orientationChanged && !resolutionChanged && !primaryChanged)
            Emit("display.configuration.changed", WindowsEventCategory.Display, data);
    }
    private void TimeChanged(object? s, EventArgs e)
    {
        var currentZone = TimeZoneInfo.Local.Id;
        var concrete = currentZone != _timeZoneId ? "system.time.timezone_changed" : "system.time.clock_adjusted";
        EmitBoth("system.time.changed", concrete, WindowsEventCategory.SystemSettings,
            new() { ["local_time"] = DateTimeOffset.Now.ToString("O"), ["timezone"] = currentZone });
        _timeZoneId = currentZone;
    }
    private void UserPreferenceChanged(object s, UserPreferenceChangedEventArgs e)
    {
        var concrete = e.Category switch
        {
            UserPreferenceCategory.Locale => "system.settings.locale_changed", UserPreferenceCategory.Color => "system.settings.colors_changed",
            UserPreferenceCategory.Desktop => "system.settings.desktop_changed", UserPreferenceCategory.General => "system.settings.general_changed",
            UserPreferenceCategory.Icon => "system.settings.icons_changed", UserPreferenceCategory.Keyboard => "system.settings.keyboard_changed",
            UserPreferenceCategory.Menu => "system.settings.menu_changed", UserPreferenceCategory.Mouse => "system.settings.mouse_changed",
            UserPreferenceCategory.Power => "system.settings.power_changed", UserPreferenceCategory.Screensaver => "system.settings.screensaver_changed",
            UserPreferenceCategory.Window => "system.settings.window_changed", _ => "system.settings.changed"
        };
        EmitBoth("system.settings.changed", concrete, WindowsEventCategory.SystemSettings, new() { ["category"] = e.Category.ToString() });
    }
    private void SessionEnding(object s, SessionEndingEventArgs e)
    {
        var concrete = e.Reason == SessionEndReasons.Logoff ? "system.lifecycle.logoff" : "system.lifecycle.shutdown";
        EmitBoth("system.lifecycle.changed", concrete, WindowsEventCategory.SystemLifecycle, new() { ["reason"] = e.Reason.ToString() });
    }
    private static Dictionary<string, DisplayState> CurrentDisplays() => System.Windows.Forms.Screen.AllScreens.ToDictionary(x => x.DeviceName,
        x => new DisplayState(x.Bounds.Width, x.Bounds.Height, x.Primary), StringComparer.OrdinalIgnoreCase);
    private sealed record DisplayState(int Width, int Height, bool Primary) { public bool IsPortrait => Height > Width; }
}

public sealed class ProcessTraceWindowsEventSource : IWindowsEventSource
{
    private ManagementEventWatcher? _start;
    private ManagementEventWatcher? _stop;
    public event Action<WindowsSystemEvent>? EventReceived;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_start is not null) return Task.CompletedTask;
        _start = Watch("Win32_ProcessStartTrace", "process.started");
        _stop = Watch("Win32_ProcessStopTrace", "process.exited");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var watcher in new[] { _start, _stop }) { try { watcher?.Stop(); } catch { } watcher?.Dispose(); }
        _start = _stop = null;
        return Task.CompletedTask;
    }

    private ManagementEventWatcher Watch(string trace, string eventType)
    {
        var watcher = new ManagementEventWatcher(new WqlEventQuery($"SELECT * FROM {trace}"));
        watcher.EventArrived += (_, e) => EventReceived?.Invoke(new WindowsSystemEvent(eventType, WindowsEventCategory.Process,
            DateTimeOffset.Now, e.NewEvent.Properties["ProcessID"]?.Value?.ToString(), new Dictionary<string, string?>
            {
                ["name"] = e.NewEvent.Properties["ProcessName"]?.Value?.ToString(),
                ["process_id"] = e.NewEvent.Properties["ProcessID"]?.Value?.ToString()
            }));
        watcher.Start();
        return watcher;
    }
}
