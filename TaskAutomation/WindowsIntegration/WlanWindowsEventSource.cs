using System.Runtime.InteropServices;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Native WLAN AutoConfig notifications for association, roaming, radio and signal changes.</summary>
public sealed class WlanWindowsEventSource : IWindowsEventSource
{
    private const uint ClientVersion = 2;
    private const uint SourceAcm = 0x00000008;
    private const uint SourceMsm = 0x00000010;
    private IntPtr _client;
    private NotificationCallback? _callback;
    public event Action<WindowsSystemEvent>? EventReceived;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_client != IntPtr.Zero) return Task.CompletedTask;
        var error = WlanOpenHandle(ClientVersion, IntPtr.Zero, out _, out _client);
        if (error != 0) throw new InvalidOperationException($"WLAN-API konnte nicht geöffnet werden (Win32 {error}).");
        _callback = OnNotification;
        error = WlanRegisterNotification(_client, SourceAcm | SourceMsm, false, _callback, IntPtr.Zero, IntPtr.Zero, out _);
        if (error != 0) { WlanCloseHandle(_client, IntPtr.Zero); _client = IntPtr.Zero; throw new InvalidOperationException($"WLAN-Ereignisse konnten nicht registriert werden (Win32 {error})."); }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client == IntPtr.Zero) return Task.CompletedTask;
        WlanRegisterNotification(_client, 0, false, null, IntPtr.Zero, IntPtr.Zero, out _);
        WlanCloseHandle(_client, IntPtr.Zero); _client = IntPtr.Zero; _callback = null;
        return Task.CompletedTask;
    }

    private void OnNotification(ref NotificationData notification, IntPtr context)
    {
        var type = notification.Source switch
        {
            SourceAcm => AcmType(notification.Code), SourceMsm => MsmType(notification.Code), _ => null
        };
        if (type is null) return;
        var connection = TryReadConnection(notification.Data, notification.DataSize);
        var data = new Dictionary<string, string?>
        {
            ["change"] = type.Split('.').Last(), ["interface_id"] = notification.InterfaceGuid.ToString(),
            ["ssid"] = connection?.Ssid, ["profile"] = connection?.Profile, ["reason_code"] = connection?.ReasonCode.ToString()
        };
        EventReceived?.Invoke(new WindowsSystemEvent("network.wifi.changed", WindowsEventCategory.Network, DateTimeOffset.Now, notification.InterfaceGuid.ToString(), data));
        EventReceived?.Invoke(new WindowsSystemEvent(type, WindowsEventCategory.Network, DateTimeOffset.Now, notification.InterfaceGuid.ToString(), data));
    }

    private static string? AcmType(uint code) => code switch
    {
        1 => "network.wifi.autoconfig_enabled", 2 => "network.wifi.autoconfig_disabled", 7 => "network.wifi.scan_completed",
        8 => "network.wifi.scan_failed", 9 => "network.wifi.connecting", 10 => "network.wifi.connected",
        11 => "network.wifi.connection_failed", 13 => "network.wifi.adapter_added", 14 => "network.wifi.adapter_removed",
        15 or 16 => "network.wifi.profile_changed", 18 => "network.wifi.network_unavailable", 19 => "network.wifi.network_available",
        20 => "network.wifi.disconnecting", 21 => "network.wifi.disconnected", _ => null
    };
    private static string? MsmType(uint code) => code switch
    {
        1 => "network.wifi.associating", 2 => "network.wifi.associated", 3 => "network.wifi.authenticating",
        4 => "network.wifi.connected", 5 => "network.wifi.roaming_started", 6 => "network.wifi.roaming_completed",
        7 => "network.wifi.radio_state_changed", 8 => "network.wifi.signal_quality_changed", 9 => "network.wifi.disconnecting",
        10 => "network.wifi.disconnected", _ => null
    };

    private static ConnectionData? TryReadConnection(IntPtr pointer, uint size)
    {
        if (pointer == IntPtr.Zero || size < 556) return null;
        try
        {
            var raw = Marshal.PtrToStructure<ConnectionNotificationData>(pointer);
            var length = Math.Min((int)raw.Ssid.Length, raw.Ssid.Value?.Length ?? 0);
            return new ConnectionData(raw.ProfileName ?? string.Empty,
                length > 0 ? System.Text.Encoding.UTF8.GetString(raw.Ssid.Value!, 0, length) : string.Empty, raw.ReasonCode);
        }
        catch { return null; }
    }

    private sealed record ConnectionData(string Profile, string Ssid, uint ReasonCode);
    private delegate void NotificationCallback(ref NotificationData data, IntPtr context);
    [StructLayout(LayoutKind.Sequential)] private struct NotificationData { public uint Source; public uint Code; public Guid InterfaceGuid; public uint DataSize; public IntPtr Data; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct ConnectionNotificationData
    {
        public int ConnectionMode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string ProfileName;
        public Dot11Ssid Ssid;
        public int BssType;
        [MarshalAs(UnmanagedType.Bool)] public bool SecurityEnabled;
        public uint ReasonCode;
        public uint Flags;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Dot11Ssid
    {
        public uint Length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Value;
    }
    [DllImport("wlanapi.dll")] private static extern uint WlanOpenHandle(uint version, IntPtr reserved, out uint negotiatedVersion, out IntPtr client);
    [DllImport("wlanapi.dll")] private static extern uint WlanCloseHandle(IntPtr client, IntPtr reserved);
    [DllImport("wlanapi.dll")] private static extern uint WlanRegisterNotification(IntPtr client, uint source, [MarshalAs(UnmanagedType.Bool)] bool ignoreDuplicate,
        NotificationCallback? callback, IntPtr context, IntPtr reserved, out uint previousSource);
}
