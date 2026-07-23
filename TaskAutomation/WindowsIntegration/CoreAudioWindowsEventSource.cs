using System.Runtime.InteropServices;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Core Audio callbacks for endpoint and master-volume changes.</summary>
public sealed class CoreAudioWindowsEventSource : IWindowsEventSource, IMMNotificationClient, IAudioEndpointVolumeCallback
{
    private IMMDeviceEnumerator? _enumerator;
    private IMMDevice? _device;
    private IAudioEndpointVolume? _volume;
    private bool? _lastMuted;
    private float? _lastVolume;
    public event Action<WindowsSystemEvent>? EventReceived;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_enumerator is not null) return Task.CompletedTask;
        _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        Marshal.ThrowExceptionForHR(_enumerator.RegisterEndpointNotificationCallback(this));
        AttachDefaultEndpoint();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_volume is not null) { try { _volume.UnregisterControlChangeNotify(this); } catch { } }
        if (_enumerator is not null) { try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch { } }
        Release(ref _volume); Release(ref _device); Release(ref _enumerator);
        return Task.CompletedTask;
    }

    private void AttachDefaultEndpoint()
    {
        if (_volume is not null) { try { _volume.UnregisterControlChangeNotify(this); } catch { } }
        Release(ref _volume); Release(ref _device);
        if (_enumerator is null || _enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out _device) < 0) return;
        var iid = typeof(IAudioEndpointVolume).GUID;
        if (_device.Activate(ref iid, 23, IntPtr.Zero, out var instance) < 0) return;
        _volume = (IAudioEndpointVolume)instance;
        Marshal.ThrowExceptionForHR(_volume.RegisterControlChangeNotify(this));
        if (_volume.GetMute(out var muted) >= 0) _lastMuted = muted;
        if (_volume.GetMasterVolumeLevelScalar(out var level) >= 0) _lastVolume = level;
    }

    public int OnNotify(IntPtr notifyData)
    {
        var data = Marshal.PtrToStructure<AudioVolumeNotificationData>(notifyData);
        var muteChanged = _lastMuted is not null && _lastMuted != data.Muted;
        var levelChanged = _lastVolume is null || Math.Abs(_lastVolume.Value - data.MasterVolume) > 0.0001f;
        var values = new Dictionary<string, string?>
        {
            ["muted"] = data.Muted.ToString(), ["percentage"] = Math.Round(data.MasterVolume * 100d, 2).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["change"] = muteChanged ? data.Muted ? "muted" : "unmuted" : "volume_changed"
        };
        EventReceived?.Invoke(new WindowsSystemEvent("audio.volume.changed", WindowsEventCategory.Audio, DateTimeOffset.Now, Data: values));
        if (muteChanged) EventReceived?.Invoke(new WindowsSystemEvent(data.Muted ? "audio.volume.muted" : "audio.volume.unmuted", WindowsEventCategory.Audio, DateTimeOffset.Now, Data: values));
        if (levelChanged) EventReceived?.Invoke(new WindowsSystemEvent("audio.volume.level_changed", WindowsEventCategory.Audio, DateTimeOffset.Now, Data: values));
        _lastMuted = data.Muted; _lastVolume = data.MasterVolume;
        return 0;
    }

    public int OnDeviceStateChanged(string deviceId, uint newState)
    {
        var values = DeviceData(deviceId, "state_changed", newState.ToString());
        EmitBoth("audio.device.changed", "audio.device.state_changed", WindowsEventCategory.Audio, deviceId, values);
        if (newState == 0x00000001)
            EventReceived?.Invoke(new WindowsSystemEvent("audio.device.connected", WindowsEventCategory.Audio, DateTimeOffset.Now, deviceId, values));
        else if (newState is 0x00000004 or 0x00000008)
            EventReceived?.Invoke(new WindowsSystemEvent("audio.device.disconnected", WindowsEventCategory.Audio, DateTimeOffset.Now, deviceId, values));
        return 0;
    }
    public int OnDeviceAdded(string deviceId) { EmitBoth("audio.device.changed", "audio.device.added", WindowsEventCategory.Audio, deviceId, DeviceData(deviceId, "added")); return 0; }
    public int OnDeviceRemoved(string deviceId) { EmitBoth("audio.device.changed", "audio.device.removed", WindowsEventCategory.Audio, deviceId, DeviceData(deviceId, "removed")); return 0; }
    public int OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? deviceId)
    {
        if (flow == EDataFlow.Render && role == ERole.Multimedia) AttachDefaultEndpoint();
        EmitBoth("audio.device.changed", "audio.device.default_changed", WindowsEventCategory.Audio, deviceId,
            DeviceData(deviceId, "default_changed", $"{flow}.{role}"));
        return 0;
    }
    public int OnPropertyValueChanged(string deviceId, PropertyKey key)
    {
        EmitBoth("audio.device.changed", "audio.device.property_changed", WindowsEventCategory.Audio, deviceId, DeviceData(deviceId, "property_changed"));
        return 0;
    }

    private static Dictionary<string, string?> DeviceData(string? id, string change, string? state = null) => new()
    {
        ["id"] = id, ["name"] = id, ["filter_value"] = id, ["change"] = change, ["state"] = state
    };
    private void EmitBoth(string legacy, string concrete, WindowsEventCategory category, string? id, Dictionary<string, string?> data)
    {
        EventReceived?.Invoke(new WindowsSystemEvent(legacy, category, DateTimeOffset.Now, id, data));
        EventReceived?.Invoke(new WindowsSystemEvent(concrete, category, DateTimeOffset.Now, id, data));
    }
    private static void Release<T>(ref T? value) where T : class
    {
        if (value is not null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
        value = null;
    }

    public enum EDataFlow { Render, Capture, All }
    public enum ERole { Console, Multimedia, Communications }
    [StructLayout(LayoutKind.Sequential)] public struct PropertyKey { public Guid FormatId; public uint PropertyId; }
    [StructLayout(LayoutKind.Sequential)] private struct AudioVolumeNotificationData
    {
        public Guid EventContext;
        [MarshalAs(UnmanagedType.Bool)] public bool Muted;
        public float MasterVolume;
        public uint Channels;
        public float FirstChannelVolume;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumeratorComObject { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(EDataFlow flow, uint mask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(EDataFlow flow, ERole role, out IMMDevice endpoint);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IMMNotificationClient client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint context, IntPtr parameters, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
        [PreserveSig] int OpenPropertyStore(uint access, out IntPtr properties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetState(out uint state);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IAudioEndpointVolumeCallback callback);
        [PreserveSig] int UnregisterControlChangeNotify(IAudioEndpointVolumeCallback callback);
        [PreserveSig] int GetChannelCount(out uint count);
        [PreserveSig] int SetMasterVolumeLevel(float level, IntPtr context);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, IntPtr context);
        [PreserveSig] int GetMasterVolumeLevel(out float level);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float level, IntPtr context);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, IntPtr context);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float level);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, IntPtr context);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
}

[ComVisible(true), Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMNotificationClient
{
    [PreserveSig] int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, uint newState);
    [PreserveSig] int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
    [PreserveSig] int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
    [PreserveSig] int OnDefaultDeviceChanged(CoreAudioWindowsEventSource.EDataFlow flow, CoreAudioWindowsEventSource.ERole role,
        [MarshalAs(UnmanagedType.LPWStr)] string? deviceId);
    [PreserveSig] int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, CoreAudioWindowsEventSource.PropertyKey key);
}

[ComVisible(true), Guid("657804FA-D6AD-4496-8A60-352752AF4F89"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioEndpointVolumeCallback
{
    [PreserveSig] int OnNotify(IntPtr notifyData);
}
