using System.Runtime.InteropServices;
using EDataFlow = TaskAutomation.WindowsIntegration.CoreAudioWindowsEventSource.EDataFlow;
using ERole = TaskAutomation.WindowsIntegration.CoreAudioWindowsEventSource.ERole;
using IAudioEndpointVolume = TaskAutomation.WindowsIntegration.CoreAudioWindowsEventSource.IAudioEndpointVolume;
using IMMDevice = TaskAutomation.WindowsIntegration.CoreAudioWindowsEventSource.IMMDevice;
using IMMDeviceEnumerator = TaskAutomation.WindowsIntegration.CoreAudioWindowsEventSource.IMMDeviceEnumerator;
using MMDeviceEnumeratorComObject = TaskAutomation.WindowsIntegration.CoreAudioWindowsEventSource.MMDeviceEnumeratorComObject;

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

}
