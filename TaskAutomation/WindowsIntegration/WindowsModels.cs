using System.Text.Json;

namespace TaskAutomation.WindowsIntegration;

public enum WindowsEventCategory
{
    Network, Audio, Session, Power, Display, Device, Bluetooth, FileSystem,
    Process, Window, Input, Clipboard, Printer, Storage, SystemSettings,
    Security, WindowsUpdate, SystemLifecycle
}

public enum WindowsCapabilityStatus
{
    Success, Unsupported, AccessDenied, Timeout, Failed
}

public enum WindowsConnectivity { Unknown, Disconnected, LocalNetwork, Internet }
public enum WindowsConnectionType { Unknown, Ethernet, WiFi, Mobile, Virtual }
public enum WindowsPowerSource { Unknown, Ac, Battery }
public enum WindowsSessionState { Unknown, Active, Locked, Disconnected }
public enum WindowsDeviceState { Unknown, Connected, Disconnected, Disabled, Error }
public enum WindowsOnOffState { Unknown, Off, On }

public sealed record WindowsSystemEvent(
    string EventType,
    WindowsEventCategory Category,
    DateTimeOffset Timestamp,
    string? ResourceId = null,
    IReadOnlyDictionary<string, string?>? Data = null);

public sealed class WindowsEventSubscription
{
    public string EventType { get; init; } = string.Empty;
    public Dictionary<string, string?> Filters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public TimeSpan Debounce { get; init; }
}

public sealed class WindowsStateQuery
{
    public string QueryType { get; init; } = string.Empty;
    public Dictionary<string, string?> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Stable union result for all Windows queries. Irrelevant properties retain their neutral value.
/// This deliberately gives the job condition system a statically discoverable contract.
/// </summary>
internal sealed record WindowsStateSnapshot
{
    public WindowsCapabilityStatus Status { get; init; } = WindowsCapabilityStatus.Success;
    public bool IsAvailable => Status == WindowsCapabilityStatus.Success;
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public bool Exists { get; init; }
    public bool IsActive { get; init; }
    public bool IsConnected { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsMuted { get; init; }
    public bool IsCharging { get; init; }
    public bool PendingRestart { get; init; }
    public long Count { get; init; }
    public long Value { get; init; }
    public double Percentage { get; init; }
    public double FreeSpaceGb { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public WindowsConnectivity Connectivity { get; init; } = WindowsConnectivity.Unknown;
    public WindowsConnectionType ConnectionType { get; init; } = WindowsConnectionType.Unknown;
    public WindowsPowerSource PowerSource { get; init; } = WindowsPowerSource.Unknown;
    public WindowsSessionState SessionState { get; init; } = WindowsSessionState.Unknown;
    public WindowsDeviceState DeviceState { get; init; } = WindowsDeviceState.Unknown;
    public WindowsOnOffState OnOffState { get; init; } = WindowsOnOffState.Unknown;
    public IReadOnlyList<string> Items { get; init; } = [];

    public string Fingerprint() => JsonSerializer.Serialize(this with { CapturedAt = default });
}

public sealed record WindowsCapabilityRequirements(
    bool RequiresElevation = false,
    string MinimumWindowsVersion = "Windows 10");

public enum WindowsParameterType
{
    Text, Integer, Boolean, FilePath, DirectoryPath, ProcessName, Drive, Enum, Duration
}

public sealed record WindowsParameterDescriptor(
    string Name,
    string DisplayName,
    WindowsParameterType Type,
    bool Required = false,
    string? DefaultValue = null,
    IReadOnlyList<string>? AllowedValues = null,
    string? Placeholder = null);

public sealed record WindowsCapabilityDescriptor(
    string Id,
    WindowsEventCategory Category,
    string DisplayName,
    bool SupportsEvents,
    bool SupportsStateQuery,
    string? RelatedQuery = null,
    WindowsCapabilityRequirements? Requirements = null,
    IReadOnlyList<WindowsParameterDescriptor>? Parameters = null,
    string? ResultTypeName = null);
