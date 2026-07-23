using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Steps;

/// <summary>Technical status shared by every Windows point-in-time query.</summary>
public abstract record WindowsStateQueryResult : StepResultBase
{
    [ResultProperty("status")]
    public WindowsCapabilityStatus Status { get; init; } = WindowsCapabilityStatus.Success;

    [ResultProperty("captured_at")]
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    [ResultProperty("error_code")]
    public string ErrorCode { get; init; } = string.Empty;

    [ResultProperty("error_message")]
    public string ErrorMessage { get; init; } = string.Empty;
}

public sealed record UnsupportedWindowsQueryResult : WindowsStateQueryResult;

public sealed record NetworkConnectivityQueryResult : WindowsStateQueryResult
{
    [ResultProperty("is_connected")]
    public bool IsConnected { get; init; }
    [ResultProperty("connectivity")]
    public WindowsConnectivity Connectivity { get; init; }
    [ResultProperty("connection_type")]
    public WindowsConnectionType ConnectionType { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("count")]
    public long Count { get; init; }
    [ResultProperty("items")]
    public IReadOnlyList<string> Items { get; init; } = [];
}

public sealed record AudioDevicesQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("count")]
    public long Count { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("device_state")]
    public WindowsDeviceState DeviceState { get; init; }
    [ResultProperty("items")]
    public IReadOnlyList<string> Items { get; init; } = [];
}

public sealed record AudioVolumeQueryResult : WindowsStateQueryResult
{
    [ResultProperty("device_exists")]
    public bool Exists { get; init; }
    [ResultProperty("is_muted")]
    public bool IsMuted { get; init; }
    [ResultProperty("volume_percentage")]
    public double Percentage { get; init; }
    [ResultProperty("device_id")]
    public string Id { get; init; } = string.Empty;
    [ResultProperty("on_off_state")]
    public WindowsOnOffState OnOffState { get; init; }
}

public sealed record SessionStateQueryResult : WindowsStateQueryResult
{
    [ResultProperty("is_active")]
    public bool IsActive { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("id")]
    public string Id { get; init; } = string.Empty;
    [ResultProperty("session_state")]
    public WindowsSessionState SessionState { get; init; }
}

public sealed record PowerStatusQueryResult : WindowsStateQueryResult
{
    [ResultProperty("is_connected")]
    public bool IsConnected { get; init; }
    [ResultProperty("is_charging")]
    public bool IsCharging { get; init; }
    [ResultProperty("percentage")]
    public double Percentage { get; init; }
    [ResultProperty("power_source")]
    public WindowsPowerSource PowerSource { get; init; }
}

public sealed record MonitorCollectionQueryResult : WindowsStateQueryResult
{
    [ResultProperty("is_connected")]
    public bool IsConnected { get; init; }
    [ResultProperty("count")]
    public long Count { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("items")]
    public IReadOnlyList<string> Items { get; init; } = [];
}

public sealed record DeviceCollectionQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("is_connected")]
    public bool IsConnected { get; init; }
    [ResultProperty("count")]
    public long Count { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("device_state")]
    public WindowsDeviceState DeviceState { get; init; }
    [ResultProperty("items")]
    public IReadOnlyList<string> Items { get; init; } = [];
}

public sealed record FileSystemPathQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("path")]
    public string Path { get; init; } = string.Empty;
    [ResultProperty("count")]
    public long Count { get; init; }
    [ResultProperty("value")]
    public long Value { get; init; }
}

public sealed record ProcessRunningQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("count")]
    public long Count { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("id")]
    public string Id { get; init; } = string.Empty;
}

public sealed record ForegroundWindowQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("text")]
    public string Text { get; init; } = string.Empty;
    [ResultProperty("id")]
    public string Id { get; init; } = string.Empty;
}

public sealed record InputIdleQueryResult : WindowsStateQueryResult
{
    [ResultProperty("value")]
    public long IdleMilliseconds { get; init; }
    [ResultProperty("percentage")]
    public double IdleSeconds { get; init; }
}

public sealed record ClipboardContentQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("text")]
    public string Text { get; init; } = string.Empty;
}

public sealed record PrinterStatusQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("count")]
    public long Count { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("device_state")]
    public WindowsDeviceState DeviceState { get; init; }
    [ResultProperty("items")]
    public IReadOnlyList<string> Items { get; init; } = [];
}

public sealed record StorageDrivesQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("is_connected")]
    public bool IsConnected { get; init; }
    [ResultProperty("count")]
    public long Count { get; init; }
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("free_space_gb")]
    public double FreeSpaceGb { get; init; }
    [ResultProperty("items")]
    public IReadOnlyList<string> Items { get; init; } = [];
}

public sealed record SystemSettingsQueryResult : WindowsStateQueryResult
{
    [ResultProperty("name")]
    public string Name { get; init; } = string.Empty;
    [ResultProperty("text")]
    public string Text { get; init; } = string.Empty;
    [ResultProperty("is_enabled")]
    public bool IsEnabled { get; init; }
}

public sealed record SecurityStatusQueryResult : WindowsStateQueryResult
{
    [ResultProperty("exists")]
    public bool Exists { get; init; }
    [ResultProperty("is_enabled")]
    public bool IsEnabled { get; init; }
    [ResultProperty("on_off_state")]
    public WindowsOnOffState OnOffState { get; init; }
}

public sealed record WindowsUpdateStatusQueryResult : WindowsStateQueryResult
{
    [ResultProperty("pending_restart")]
    public bool PendingRestart { get; init; }
}

public sealed record SystemLifecycleQueryResult : WindowsStateQueryResult
{
    [ResultProperty("value")]
    public long UptimeMilliseconds { get; init; }
    [ResultProperty("percentage")]
    public double UptimeSeconds { get; init; }
    [ResultProperty("text")]
    public string OperatingSystemVersion { get; init; } = string.Empty;
}

/// <summary>
/// Backend-owned registry for configuration-dependent Windows query results.
/// Both metadata discovery and runtime creation use this single mapping.
/// </summary>
public static class WindowsQueryResultRegistry
{
    private static readonly IReadOnlyDictionary<string, Type> ResultTypes =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["network.connectivity"] = typeof(NetworkConnectivityQueryResult),
            ["audio.devices"] = typeof(AudioDevicesQueryResult),
            ["audio.volume"] = typeof(AudioVolumeQueryResult),
            ["session.state"] = typeof(SessionStateQueryResult),
            ["power.status"] = typeof(PowerStatusQueryResult),
            ["display.monitors"] = typeof(MonitorCollectionQueryResult),
            ["device.hardware"] = typeof(DeviceCollectionQueryResult),
            ["device.usb"] = typeof(DeviceCollectionQueryResult),
            ["bluetooth.devices"] = typeof(DeviceCollectionQueryResult),
            ["filesystem.path"] = typeof(FileSystemPathQueryResult),
            ["process.running"] = typeof(ProcessRunningQueryResult),
            ["window.foreground"] = typeof(ForegroundWindowQueryResult),
            ["input.idle"] = typeof(InputIdleQueryResult),
            ["clipboard.content"] = typeof(ClipboardContentQueryResult),
            ["printer.status"] = typeof(PrinterStatusQueryResult),
            ["storage.drives"] = typeof(StorageDrivesQueryResult),
            ["system.settings"] = typeof(SystemSettingsQueryResult),
            ["security.status"] = typeof(SecurityStatusQueryResult),
            ["windows_update.status"] = typeof(WindowsUpdateStatusQueryResult),
            ["system.lifecycle"] = typeof(SystemLifecycleQueryResult)
        };

    public static ResultTypeDescriptor? GetContract(string queryType) =>
        ResultTypes.TryGetValue(queryType, out var resultType)
            ? StepResultMetadata.GetResultType(resultType.Name)
            : null;

    public static string? GetResultTypeName(string queryType) =>
        ResultTypes.TryGetValue(queryType, out var resultType) ? resultType.Name : null;

    internal static StepResultBase Create(string queryType, WindowsStateSnapshot value)
    {
        var common = new Common(value);
        return queryType.ToLowerInvariant() switch
        {
            "network.connectivity" => common.Apply(new NetworkConnectivityQueryResult
            {
                IsConnected = value.IsConnected, Connectivity = value.Connectivity,
                ConnectionType = value.ConnectionType, Name = value.Name, Count = value.Count, Items = value.Items
            }),
            "audio.devices" => common.Apply(new AudioDevicesQueryResult
            {
                Exists = value.Exists, Count = value.Count, Name = value.Name,
                DeviceState = value.DeviceState, Items = value.Items
            }),
            "audio.volume" => common.Apply(new AudioVolumeQueryResult
            {
                Exists = value.Exists, IsMuted = value.IsMuted, Percentage = value.Percentage,
                OnOffState = value.OnOffState, Id = value.Id
            }),
            "session.state" => common.Apply(new SessionStateQueryResult
            {
                IsActive = value.IsActive, Name = value.Name, Id = value.Id, SessionState = value.SessionState
            }),
            "power.status" => common.Apply(new PowerStatusQueryResult
            {
                IsConnected = value.IsConnected, IsCharging = value.IsCharging,
                Percentage = value.Percentage, PowerSource = value.PowerSource
            }),
            "display.monitors" => common.Apply(new MonitorCollectionQueryResult
            {
                IsConnected = value.IsConnected, Count = value.Count, Name = value.Name, Items = value.Items
            }),
            "device.hardware" or "device.usb" or "bluetooth.devices" =>
                common.Apply(new DeviceCollectionQueryResult
                {
                    Exists = value.Exists, IsConnected = value.IsConnected, Count = value.Count,
                    Name = value.Name, DeviceState = value.DeviceState, Items = value.Items
                }),
            "filesystem.path" => common.Apply(new FileSystemPathQueryResult
            {
                Exists = value.Exists, Path = value.Path,
                Count = value.Count, Value = value.Value
            }),
            "process.running" => common.Apply(new ProcessRunningQueryResult
            {
                Exists = value.Exists, Count = value.Count,
                Name = value.Name, Id = value.Id
            }),
            "window.foreground" => common.Apply(new ForegroundWindowQueryResult
            {
                Exists = value.Exists, Name = value.Name,
                Text = value.Text, Id = value.Id
            }),
            "input.idle" => common.Apply(new InputIdleQueryResult
                { IdleMilliseconds = value.Value, IdleSeconds = value.Percentage }),
            "clipboard.content" => common.Apply(new ClipboardContentQueryResult
                { Exists = value.Exists, Name = value.Name, Text = value.Text }),
            "printer.status" => common.Apply(new PrinterStatusQueryResult
            {
                Exists = value.Exists, Count = value.Count, Name = value.Name,
                DeviceState = value.DeviceState, Items = value.Items
            }),
            "storage.drives" => common.Apply(new StorageDrivesQueryResult
            {
                Exists = value.Exists, IsConnected = value.IsConnected, Count = value.Count,
                Name = value.Name, FreeSpaceGb = value.FreeSpaceGb, Items = value.Items
            }),
            "system.settings" => common.Apply(new SystemSettingsQueryResult
                { Name = value.Name, Text = value.Text, IsEnabled = value.IsEnabled }),
            "security.status" => common.Apply(new SecurityStatusQueryResult
                { Exists = value.Exists, IsEnabled = value.IsEnabled, OnOffState = value.OnOffState }),
            "windows_update.status" => common.Apply(new WindowsUpdateStatusQueryResult
                { PendingRestart = value.PendingRestart }),
            "system.lifecycle" => common.Apply(new SystemLifecycleQueryResult
                {
                    UptimeMilliseconds = value.Value,
                    UptimeSeconds = value.Percentage,
                    OperatingSystemVersion = value.Text
                }),
            _ => throw new InvalidOperationException($"No result contract is registered for Windows query '{queryType}'.")
        };
    }

    private sealed record Common(WindowsStateSnapshot Value)
    {
        public T Apply<T>(T result) where T : WindowsStateQueryResult =>
            (T)(result with
            {
                Status = Value.Status,
                CapturedAt = Value.CapturedAt,
                ErrorCode = Value.ErrorCode,
                ErrorMessage = Value.ErrorMessage
            });
    }
}
