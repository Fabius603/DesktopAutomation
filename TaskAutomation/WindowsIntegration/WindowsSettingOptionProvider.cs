using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TaskAutomation.WindowsIntegration;

public interface IWindowsSettingOptionProvider
{
    Task<IReadOnlyList<WindowsSettingOption>> GetOptionsAsync(
        WindowsDynamicOptionSource source,
        CancellationToken cancellationToken);
}

public static class WindowsSettingOptionList
{
    public static IReadOnlyList<WindowsSettingOption> PreserveCurrent(
        IEnumerable<WindowsSettingOption> discovered,
        string? currentValue,
        string unavailableDisplayName)
    {
        var result = discovered.ToList();
        if (!string.IsNullOrWhiteSpace(currentValue)
            && result.All(option =>
                !option.Value.Equals(currentValue, StringComparison.OrdinalIgnoreCase)))
            result.Insert(0, new WindowsSettingOption(currentValue, unavailableDisplayName));
        return result;
    }
}

internal static class AudioDeviceDisplayName
{
    public static string? Resolve(string? windowsFriendlyName, string? endpointName, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(windowsFriendlyName)) return windowsFriendlyName;
        if (!string.IsNullOrWhiteSpace(endpointName)
            && !string.IsNullOrWhiteSpace(deviceName)
            && !endpointName.Equals(deviceName, StringComparison.CurrentCultureIgnoreCase))
            return $"{endpointName} ({deviceName})";
        return endpointName ?? deviceName;
    }
}

internal static class AudioDeviceOptionFilter
{
    private static readonly string[] HandsFreeSuffixes =
    [
        " Hands-Free",
        " Hands-Free AG Audio",
        " Freisprechen"
    ];

    public static IReadOnlyList<WindowsSettingOption> RemoveRedundantHandsFreeOutputs(
        IReadOnlyList<WindowsSettingOption> options)
    {
        var names = options.Select(option => option.DisplayName).ToHashSet(
            StringComparer.CurrentCultureIgnoreCase);
        return options.Where(option =>
        {
            var baseName = HandsFreeSuffixes
                .Where(suffix => option.DisplayName.EndsWith(
                    suffix, StringComparison.CurrentCultureIgnoreCase))
                .Select(suffix => option.DisplayName[..^suffix.Length].TrimEnd())
                .FirstOrDefault();
            return baseName is null || !names.Contains(baseName);
        }).ToArray();
    }
}

public sealed class DefaultWindowsSettingOptionProvider : IWindowsSettingOptionProvider
{
    public Task<IReadOnlyList<WindowsSettingOption>> GetOptionsAsync(
        WindowsDynamicOptionSource source,
        CancellationToken cancellationToken) =>
        Task.Run<IReadOnlyList<WindowsSettingOption>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!OperatingSystem.IsWindows()) return [];
            return source switch
            {
                WindowsDynamicOptionSource.AudioRenderDevices => ReadAudioDevices(render: true),
                WindowsDynamicOptionSource.AudioCaptureDevices => ReadAudioDevices(render: false),
                WindowsDynamicOptionSource.WlanProfiles => ReadWlanProfiles(),
                WindowsDynamicOptionSource.Displays => ReadDisplays(),
                _ => []
            };
        }, cancellationToken);

    private static IReadOnlyList<WindowsSettingOption> ReadDisplays()
    {
        var result = new List<WindowsSettingOption>();
        for (uint index = 0; ; index++)
        {
            var adapter = DisplayDevice.Create();
            if (!EnumDisplayDevices(null, index, ref adapter, 0)) break;
            if ((adapter.StateFlags & DisplayDeviceAttachedToDesktop) == 0) continue;

            var monitor = DisplayDevice.Create();
            var hasMonitor = EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0);
            var friendlyName = hasMonitor && !string.IsNullOrWhiteSpace(monitor.DeviceString)
                ? monitor.DeviceString
                : adapter.DeviceString;
            var displayName = string.IsNullOrWhiteSpace(friendlyName)
                ? adapter.DeviceName
                : $"{friendlyName} ({adapter.DeviceName})";
            result.Add(new WindowsSettingOption(adapter.DeviceName, displayName));
        }

        return result
            .OrderByDescending(option => IsPrimaryDisplay(option.Value))
            .ThenBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsPrimaryDisplay(string deviceName)
    {
        for (uint index = 0; ; index++)
        {
            var device = DisplayDevice.Create();
            if (!EnumDisplayDevices(null, index, ref device, 0)) return false;
            if (device.DeviceName.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                return (device.StateFlags & DisplayDevicePrimaryDevice) != 0;
        }
    }

    private static IReadOnlyList<WindowsSettingOption> ReadAudioDevices(bool render)
    {
        var direction = render ? "Render" : "Capture";
        using var root = Registry.LocalMachine.OpenSubKey(
            $@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\{direction}");
        if (root is null) return [];

        var result = new List<WindowsSettingOption>();
        foreach (var endpointKeyName in root.GetSubKeyNames())
        {
            using var endpoint = root.OpenSubKey(endpointKeyName);
            if (Convert.ToInt32(endpoint?.GetValue("DeviceState", 0)) != 1) continue;
            using var properties = endpoint?.OpenSubKey("Properties");
            var displayName = ReadAudioDisplayName(properties);
            if (string.IsNullOrWhiteSpace(displayName)) displayName = endpointKeyName;
            var endpointId = $"{{0.0.{(render ? 0 : 1)}.00000000}}.{endpointKeyName}";
            result.Add(new WindowsSettingOption(endpointId, displayName));
        }
        var visibleOptions = render
            ? AudioDeviceOptionFilter.RemoveRedundantHandsFreeOutputs(result)
            : result;
        var defaultEndpointId = TryGetDefaultAudioEndpointId(render);
        return visibleOptions
            .OrderByDescending(option => option.Value.Equals(
                defaultEndpointId, StringComparison.OrdinalIgnoreCase))
            .ThenBy(option => option.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ReadAudioDisplayName(RegistryKey? properties)
    {
        if (properties is null) return null;
        string? ReadKnown(string suffix)
        {
            var name = properties.GetValueNames()
                .FirstOrDefault(valueName => valueName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            return name is null ? null : properties.GetValue(name) as string;
        }

        var windowsFriendlyName = ReadKnown(@"{b3f8fa53-0004-438e-9003-51a46e139bfc},26");
        var endpointName = ReadKnown(@"{a45c254e-df1c-4efd-8020-67d146a850e0},2");
        var deviceName = ReadKnown(@"{b3f8fa53-0004-438e-9003-51a46e139bfc},6");
        return AudioDeviceDisplayName.Resolve(windowsFriendlyName, endpointName, deviceName)
               ?? properties.GetValueNames()
                   .Select(name => properties.GetValue(name) as string)
                   .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? TryGetDefaultAudioEndpointId(bool render)
    {
        CoreAudioWindowsEventSource.IMMDeviceEnumerator? enumerator = null;
        CoreAudioWindowsEventSource.IMMDevice? device = null;
        try
        {
            enumerator = (CoreAudioWindowsEventSource.IMMDeviceEnumerator)
                new CoreAudioWindowsEventSource.MMDeviceEnumeratorComObject();
            var result = enumerator.GetDefaultAudioEndpoint(
                render
                    ? CoreAudioWindowsEventSource.EDataFlow.Render
                    : CoreAudioWindowsEventSource.EDataFlow.Capture,
                CoreAudioWindowsEventSource.ERole.Multimedia,
                out device);
            return result == 0 && device is not null && device.GetId(out var id) == 0 ? id : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (device is not null && Marshal.IsComObject(device)) Marshal.ReleaseComObject(device);
            if (enumerator is not null && Marshal.IsComObject(enumerator)) Marshal.ReleaseComObject(enumerator);
        }
    }

    private static IReadOnlyList<WindowsSettingOption> ReadWlanProfiles()
    {
        var error = WlanOpenHandle(2, IntPtr.Zero, out _, out var client);
        if (error != 0) throw new Win32Exception((int)error);
        try
        {
            error = WlanEnumInterfaces(client, IntPtr.Zero, out var interfacesPointer);
            if (error != 0) throw new Win32Exception((int)error);
            try
            {
                var count = Marshal.ReadInt32(interfacesPointer);
                var current = IntPtr.Add(interfacesPointer, 8);
                var interfaceSize = Marshal.SizeOf<WlanInterfaceInfo>();
                var profiles = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                for (var index = 0; index < count; index++)
                {
                    var info = Marshal.PtrToStructure<WlanInterfaceInfo>(current);
                    ReadProfilesForInterface(client, info.InterfaceGuid, profiles);
                    current = IntPtr.Add(current, interfaceSize);
                }
                return profiles
                    .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(name => new WindowsSettingOption(name, name))
                    .ToArray();
            }
            finally
            {
                WlanFreeMemory(interfacesPointer);
            }
        }
        finally
        {
            WlanCloseHandle(client, IntPtr.Zero);
        }
    }

    private static void ReadProfilesForInterface(IntPtr client, Guid interfaceId, ISet<string> target)
    {
        var error = WlanGetProfileList(client, ref interfaceId, IntPtr.Zero, out var profilesPointer);
        if (error != 0) return;
        try
        {
            var count = Marshal.ReadInt32(profilesPointer);
            var current = IntPtr.Add(profilesPointer, 8);
            var profileSize = Marshal.SizeOf<WlanProfileInfo>();
            for (var index = 0; index < count; index++)
            {
                var profile = Marshal.PtrToStructure<WlanProfileInfo>(current);
                if (!string.IsNullOrWhiteSpace(profile.ProfileName)) target.Add(profile.ProfileName);
                current = IntPtr.Add(current, profileSize);
            }
        }
        finally
        {
            WlanFreeMemory(profilesPointer);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanInterfaceInfo
    {
        public Guid InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Description;
        public int State;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanProfileInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ProfileName;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;

        public static DisplayDevice Create() => new() { Size = Marshal.SizeOf<DisplayDevice>() };
    }

    private const int DisplayDeviceAttachedToDesktop = 0x00000001;
    private const int DisplayDevicePrimaryDevice = 0x00000004;

    [DllImport("wlanapi.dll")]
    private static extern uint WlanOpenHandle(
        uint clientVersion, IntPtr reserved, out uint negotiatedVersion, out IntPtr clientHandle);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanCloseHandle(IntPtr clientHandle, IntPtr reserved);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanEnumInterfaces(
        IntPtr clientHandle, IntPtr reserved, out IntPtr interfaceList);

    [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)]
    private static extern uint WlanGetProfileList(
        IntPtr clientHandle, ref Guid interfaceGuid, IntPtr reserved, out IntPtr profileList);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr memory);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(
        string? device, uint index, ref DisplayDevice displayDevice, uint flags);
}
