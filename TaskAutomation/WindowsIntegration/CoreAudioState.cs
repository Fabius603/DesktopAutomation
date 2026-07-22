using System.Runtime.InteropServices;

namespace TaskAutomation.WindowsIntegration;

internal static class CoreAudioState
{
    public static WindowsStateSnapshot Query()
    {
        IMMDeviceEnumerator? enumerator = null; IMMDevice? device = null; object? endpointObject = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out device));
            var iid = typeof(IAudioEndpointVolume).GUID;
            Marshal.ThrowExceptionForHR(device.Activate(ref iid, 23, IntPtr.Zero, out endpointObject));
            var endpoint = (IAudioEndpointVolume)endpointObject;
            Marshal.ThrowExceptionForHR(endpoint.GetMasterVolumeLevelScalar(out var volume));
            Marshal.ThrowExceptionForHR(endpoint.GetMute(out var muted));
            Marshal.ThrowExceptionForHR(device.GetId(out var id));
            return new WindowsStateSnapshot
            {
                Status = WindowsCapabilityStatus.Success, CapturedAt = DateTime.UtcNow,
                Exists = true, IsActive = true, IsEnabled = true, IsMuted = muted,
                Percentage = Math.Round(volume * 100d, 2), Id = id, OnOffState = WindowsOnOffState.On
            };
        }
        finally
        {
            if (endpointObject is not null && Marshal.IsComObject(endpointObject)) Marshal.ReleaseComObject(endpointObject);
            if (device is not null && Marshal.IsComObject(device)) Marshal.ReleaseComObject(device);
            if (enumerator is not null && Marshal.IsComObject(enumerator)) Marshal.ReleaseComObject(enumerator);
        }
    }

    private enum EDataFlow { Render, Capture, All }
    private enum ERole { Console, Multimedia, Communications }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint classContext, IntPtr activationParameters,
            [MarshalAs(UnmanagedType.IUnknown)] out object instance);
        [PreserveSig] int OpenPropertyStore(uint access, out IntPtr properties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetState(out uint state);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int GetChannelCount(out uint count);
        [PreserveSig] int SetMasterVolumeLevel(float levelDb, IntPtr context);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, IntPtr context);
        [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, IntPtr context);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, IntPtr context);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, IntPtr context);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
}
