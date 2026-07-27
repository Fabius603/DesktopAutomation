using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using TaskAutomation.Steps;

namespace TaskAutomation.WindowsIntegration;

public sealed class DefaultWindowsSettingProvider : IWindowsSettingProvider
{
    public async Task<WindowsSettingChangeResult> ChangeAsync(
        WindowsSettingChange change,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            return WindowsSettingChangeResult.Failed(
                change.SettingId, WindowsCapabilityStatus.Unsupported, "setting.windows_only",
                "Windows settings can only be changed on Windows.");

        try
        {
            return change.SettingId switch
            {
                "audio.master_volume" => ChangeMasterVolume(change),
                "audio.mute" => ChangeMute(change),
                "audio.default_output" => ChangeDefaultAudioDevice(change, render: true),
                "audio.default_input" => ChangeDefaultAudioDevice(change, render: false),
                "power.scheme" => await ChangePowerSchemeAsync(change, cancellationToken).ConfigureAwait(false),
                "power.display_timeout" => await ChangePowerTimeoutAsync(change, "monitor", cancellationToken).ConfigureAwait(false),
                "power.sleep_timeout" => await ChangePowerTimeoutAsync(change, "standby", cancellationToken).ConfigureAwait(false),
                "personalization.theme" => ChangeTheme(change),
                "personalization.wallpaper" => ChangeWallpaper(change),
                "display.mode" => await ChangeDisplayModeAsync(change, cancellationToken).ConfigureAwait(false),
                "display.primary" => ChangePrimaryDisplay(change),
                "network.wifi_connection" => await ChangeWifiAsync(change, cancellationToken).ConfigureAwait(false),
                "network.vpn_connection" => await ChangeVpnAsync(change, cancellationToken).ConfigureAwait(false),
                "notifications.focus_mode" => ChangeFocusMode(change),
                "printer.default" => ChangeDefaultPrinter(change),
                _ => WindowsSettingChangeResult.Failed(
                    change.SettingId, WindowsCapabilityStatus.Unsupported, "setting.unsupported",
                    "The selected Windows setting is not supported.")
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            return WindowsSettingChangeResult.Failed(
                change.SettingId, WindowsCapabilityStatus.AccessDenied, "setting.access_denied", ex.Message);
        }
        catch (Exception ex)
        {
            return WindowsSettingChangeResult.Failed(
                change.SettingId, WindowsCapabilityStatus.Failed, "setting.failed", ex.Message);
        }
    }

    private static WindowsSettingChangeResult ChangeMasterVolume(WindowsSettingChange change)
    {
        var requested = RequiredInt(change, "value", 0, 100);
        using var endpoint = AudioEndpoint.OpenDefault(render: true);
        Marshal.ThrowExceptionForHR(endpoint.Volume.GetMasterVolumeLevelScalar(out var previous));
        Marshal.ThrowExceptionForHR(endpoint.Volume.SetMasterVolumeLevelScalar(requested / 100f, IntPtr.Zero));
        return Success(change, Math.Round(previous * 100).ToString(), requested.ToString());
    }

    private static WindowsSettingChangeResult ChangeMute(WindowsSettingChange change)
    {
        var requested = Required(change, "state");
        using var endpoint = AudioEndpoint.OpenDefault(render: true);
        Marshal.ThrowExceptionForHR(endpoint.Volume.GetMute(out var previous));
        var applied = requested switch
        {
            "on" => true,
            "off" => false,
            "toggle" => !previous,
            _ => throw new ArgumentException("The mute state must be on, off, or toggle.")
        };
        Marshal.ThrowExceptionForHR(endpoint.Volume.SetMute(applied, IntPtr.Zero));
        return Success(change, previous.ToString(), applied.ToString());
    }

    private static WindowsSettingChangeResult ChangeDefaultAudioDevice(
        WindowsSettingChange change,
        bool render)
    {
        var requested = Required(change, "device_name");
        var deviceId = AudioEndpoint.ResolveDeviceId(requested, render);
        using var current = AudioEndpoint.OpenDefault(render);
        Marshal.ThrowExceptionForHR(current.Device.GetId(out var previousId));

        object policyObject = new PolicyConfigComObject();
        var policy = (IPolicyConfig)policyObject;
        try
        {
            foreach (var role in new[] { ERole.Console, ERole.Multimedia, ERole.Communications })
                Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(deviceId, role));
        }
        finally
        {
            if (Marshal.IsComObject(policy)) Marshal.ReleaseComObject(policy);
        }
        return Success(change, previousId, deviceId);
    }

    private static async Task<WindowsSettingChangeResult> ChangePowerSchemeAsync(
        WindowsSettingChange change,
        CancellationToken cancellationToken)
    {
        var scheme = Required(change, "scheme");
        var alias = scheme switch
        {
            "balanced" => "SCHEME_BALANCED",
            "high_performance" => "SCHEME_MIN",
            "power_saver" => "SCHEME_MAX",
            _ => throw new ArgumentException("Unknown power scheme.")
        };
        var previous = await RunAsync("powercfg.exe", ["/getactivescheme"], cancellationToken).ConfigureAwait(false);
        await RunAsync("powercfg.exe", ["/setactive", alias], cancellationToken).ConfigureAwait(false);
        return Success(change, previous.Trim(), scheme);
    }

    private static async Task<WindowsSettingChangeResult> ChangePowerTimeoutAsync(
        WindowsSettingChange change,
        string setting,
        CancellationToken cancellationToken)
    {
        var minutes = RequiredInt(change, "minutes", 0, 35791394);
        var source = Required(change, "power_source");
        if (source is not ("both" or "ac" or "dc"))
            throw new ArgumentException("The power source must be both, ac, or dc.");

        if (source is "both" or "ac")
            await RunAsync("powercfg.exe", ["/change", $"{setting}-timeout-ac", minutes.ToString()], cancellationToken)
                .ConfigureAwait(false);
        if (source is "both" or "dc")
            await RunAsync("powercfg.exe", ["/change", $"{setting}-timeout-dc", minutes.ToString()], cancellationToken)
                .ConfigureAwait(false);
        return Success(change, string.Empty, $"{minutes} ({source})");
    }

    private static WindowsSettingChangeResult ChangeTheme(WindowsSettingChange change)
    {
        var theme = Required(change, "theme");
        var value = theme switch
        {
            "light" => 1,
            "dark" => 0,
            _ => throw new ArgumentException("The theme must be light or dark.")
        };
        using var key = Registry.CurrentUser.CreateSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: true)
            ?? throw new InvalidOperationException("The Windows theme settings could not be opened.");
        var previousApps = Convert.ToInt32(key.GetValue("AppsUseLightTheme", 1));
        var previousSystem = Convert.ToInt32(key.GetValue("SystemUsesLightTheme", 1));
        key.SetValue("AppsUseLightTheme", value, RegistryValueKind.DWord);
        key.SetValue("SystemUsesLightTheme", value, RegistryValueKind.DWord);
        NativeMethods.BroadcastSettingChange("ImmersiveColorSet");
        var previous = previousApps == 1 && previousSystem == 1 ? "light" :
            previousApps == 0 && previousSystem == 0 ? "dark" : "mixed";
        return Success(change, previous, theme);
    }

    private static WindowsSettingChangeResult ChangeWallpaper(WindowsSettingChange change)
    {
        var path = Path.GetFullPath(Required(change, "path"));
        if (!File.Exists(path)) throw new FileNotFoundException("The selected wallpaper does not exist.", path);
        var previous = NativeMethods.GetWallpaper();
        if (!NativeMethods.SystemParametersInfo(
                NativeMethods.SpiSetDeskWallpaper, 0, path,
                NativeMethods.SpifUpdateIniFile | NativeMethods.SpifSendChange))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return Success(change, previous, path);
    }

    private static async Task<WindowsSettingChangeResult> ChangeDisplayModeAsync(
        WindowsSettingChange change,
        CancellationToken cancellationToken)
    {
        var mode = Required(change, "mode");
        var argument = mode switch
        {
            "internal" => "/internal",
            "duplicate" => "/clone",
            "extend" => "/extend",
            "external" => "/external",
            _ => throw new ArgumentException("Unknown display mode.")
        };
        await RunAsync(Path.Combine(Environment.SystemDirectory, "DisplaySwitch.exe"), [argument], cancellationToken)
            .ConfigureAwait(false);
        return Success(change, string.Empty, mode);
    }

    private static WindowsSettingChangeResult ChangePrimaryDisplay(WindowsSettingChange change)
    {
        var requested = Required(change, "display_name");
        var deviceName = NativeMethods.ResolveDisplayName(requested);
        var mode = NativeMethods.GetCurrentDisplayMode(deviceName);
        var previous = NativeMethods.GetPrimaryDisplayName();
        mode.dmPositionX = 0;
        mode.dmPositionY = 0;
        mode.dmFields = NativeMethods.DmPosition;
        var result = NativeMethods.ChangeDisplaySettingsEx(
            deviceName, ref mode, IntPtr.Zero,
            NativeMethods.CdsSetPrimary | NativeMethods.CdsUpdateRegistry,
            IntPtr.Zero);
        if (result != 0) throw new Win32Exception($"Windows rejected the primary display change ({result}).");
        return Success(change, previous, deviceName);
    }

    private static async Task<WindowsSettingChangeResult> ChangeWifiAsync(
        WindowsSettingChange change,
        CancellationToken cancellationToken)
    {
        var action = Required(change, "action");
        if (action == "disconnect")
        {
            await RunAsync("netsh.exe", ["wlan", "disconnect"], cancellationToken).ConfigureAwait(false);
            return Success(change, string.Empty, "disconnected");
        }
        if (action != "connect") throw new ArgumentException("The WLAN action must be connect or disconnect.");
        if (!change.Parameters.TryGetValue("profile", out var profile) || string.IsNullOrWhiteSpace(profile))
            throw new ArgumentException("A WLAN profile is required when connecting.");
        await RunAsync("netsh.exe", ["wlan", "connect", $"name={profile}"], cancellationToken).ConfigureAwait(false);
        return Success(change, string.Empty, profile);
    }

    private static async Task<WindowsSettingChangeResult> ChangeVpnAsync(
        WindowsSettingChange change,
        CancellationToken cancellationToken)
    {
        var action = Required(change, "action");
        var name = Required(change, "connection_name");
        var arguments = action switch
        {
            "connect" => new[] { name },
            "disconnect" => new[] { name, "/disconnect" },
            _ => throw new ArgumentException("The VPN action must be connect or disconnect.")
        };
        await RunAsync("rasdial.exe", arguments, cancellationToken).ConfigureAwait(false);
        return Success(change, string.Empty, $"{action}: {name}");
    }

    private static WindowsSettingChangeResult ChangeFocusMode(WindowsSettingChange change)
    {
        var mode = Required(change, "mode");
        var enabled = mode switch
        {
            "off" => 1,
            "on" => 0,
            _ => throw new ArgumentException("The focus mode must be on or off.")
        };
        using var key = Registry.CurrentUser.CreateSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings", writable: true)
            ?? throw new InvalidOperationException("The Windows notification settings could not be opened.");
        var previous = Convert.ToInt32(key.GetValue("NOC_GLOBAL_SETTING_TOASTS_ENABLED", 1)) == 0 ? "on" : "off";
        key.SetValue("NOC_GLOBAL_SETTING_TOASTS_ENABLED", enabled, RegistryValueKind.DWord);
        NativeMethods.BroadcastSettingChange("Windows.Notification");
        return Success(change, previous, mode);
    }

    private static WindowsSettingChangeResult ChangeDefaultPrinter(WindowsSettingChange change)
    {
        var printer = Required(change, "printer_name");
        var previous = NativeMethods.GetDefaultPrinterName();
        if (!NativeMethods.SetDefaultPrinter(printer))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return Success(change, previous, printer);
    }

    private static WindowsSettingChangeResult Success(
        WindowsSettingChange change,
        string previous,
        string applied) =>
        new()
        {
            WasExecuted = true,
            Success = true,
            Status = WindowsCapabilityStatus.Success,
            SettingId = change.SettingId,
            PreviousValue = previous,
            AppliedValue = applied
        };

    private static string Required(WindowsSettingChange change, string name) =>
        change.Parameters.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException($"The required parameter '{name}' is missing.");

    private static int RequiredInt(WindowsSettingChange change, string name, int minimum, int maximum)
    {
        var raw = Required(change, name);
        if (!int.TryParse(raw, out var value) || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(name, $"The value must be between {minimum} and {maximum}.");
        return value;
    }

    private static async Task<string> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = info };
        if (!process.Start()) throw new InvalidOperationException($"{fileName} could not be started.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error) ? $"{fileName} returned exit code {process.ExitCode}." : error.Trim());
        return output;
    }

    private sealed class AudioEndpoint : IDisposable
    {
        private readonly CoreAudioWindowsEventSource.IMMDeviceEnumerator _enumerator;
        private readonly object _volumeObject;
        public CoreAudioWindowsEventSource.IMMDevice Device { get; }
        public CoreAudioWindowsEventSource.IAudioEndpointVolume Volume { get; }

        private AudioEndpoint(
            CoreAudioWindowsEventSource.IMMDeviceEnumerator enumerator,
            CoreAudioWindowsEventSource.IMMDevice device,
            object volumeObject)
        {
            _enumerator = enumerator;
            Device = device;
            _volumeObject = volumeObject;
            Volume = (CoreAudioWindowsEventSource.IAudioEndpointVolume)volumeObject;
        }

        public static AudioEndpoint OpenDefault(bool render)
        {
            var enumerator = (CoreAudioWindowsEventSource.IMMDeviceEnumerator)
                new CoreAudioWindowsEventSource.MMDeviceEnumeratorComObject();
            Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(
                render ? CoreAudioWindowsEventSource.EDataFlow.Render : CoreAudioWindowsEventSource.EDataFlow.Capture,
                CoreAudioWindowsEventSource.ERole.Multimedia, out var device));
            var iid = typeof(CoreAudioWindowsEventSource.IAudioEndpointVolume).GUID;
            Marshal.ThrowExceptionForHR(device.Activate(ref iid, 23, IntPtr.Zero, out var volumeObject));
            return new AudioEndpoint(enumerator, device, volumeObject);
        }

        public static string ResolveDeviceId(string nameOrId, bool render)
        {
            if (nameOrId.StartsWith("{0.0.", StringComparison.OrdinalIgnoreCase)) return nameOrId;
            var direction = render ? "Render" : "Capture";
            using var root = Registry.LocalMachine.OpenSubKey(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\{direction}");
            foreach (var subKeyName in root?.GetSubKeyNames() ?? [])
            {
                using var properties = root!.OpenSubKey($@"{subKeyName}\Properties");
                var matches = properties?.GetValueNames()
                    .Select(valueName => properties.GetValue(valueName))
                    .OfType<string>()
                    .Any(value => value.Contains(nameOrId, StringComparison.CurrentCultureIgnoreCase)) == true;
                if (matches)
                    return $"{{0.0.{(render ? 0 : 1)}.00000000}}.{subKeyName}";
            }
            throw new InvalidOperationException($"No active audio device matching '{nameOrId}' was found.");
        }

        public void Dispose()
        {
            if (Marshal.IsComObject(_volumeObject)) Marshal.ReleaseComObject(_volumeObject);
            if (Marshal.IsComObject(Device)) Marshal.ReleaseComObject(Device);
            if (Marshal.IsComObject(_enumerator)) Marshal.ReleaseComObject(_enumerator);
        }
    }

    private enum ERole { Console, Multimedia, Communications }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private sealed class PolicyConfigComObject { }

    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        int GetMixFormat(string deviceId, IntPtr format);
        int GetDeviceFormat(string deviceId, int defaultFormat, IntPtr format);
        int ResetDeviceFormat(string deviceId);
        int SetDeviceFormat(string deviceId, IntPtr endpointFormat, IntPtr mixFormat);
        int GetProcessingPeriod(string deviceId, int defaultPeriod, IntPtr defaultValue, IntPtr minimumValue);
        int SetProcessingPeriod(string deviceId, IntPtr period);
        int GetShareMode(string deviceId, IntPtr mode);
        int SetShareMode(string deviceId, IntPtr mode);
        int GetPropertyValue(string deviceId, IntPtr key, IntPtr value);
        int SetPropertyValue(string deviceId, IntPtr key, IntPtr value);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ERole role);
        int SetEndpointVisibility(string deviceId, int visible);
    }

    private static class NativeMethods
    {
        public const uint SpiSetDeskWallpaper = 0x0014;
        private const uint SpiGetDeskWallpaper = 0x0073;
        public const uint SpifUpdateIniFile = 0x01;
        public const uint SpifSendChange = 0x02;
        public const int CdsUpdateRegistry = 0x00000001;
        public const int CdsSetPrimary = 0x00000010;
        public const int DmPosition = 0x00000020;
        private const int DisplayDeviceAttachedToDesktop = 0x00000001;
        private const int DisplayDevicePrimaryDevice = 0x00000004;
        private const int EnumCurrentSettings = -1;
        private const uint WmSettingChange = 0x001A;
        private static readonly IntPtr HwndBroadcast = new(0xffff);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(
            uint action, uint parameter, string value, uint update);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(
            uint action, uint parameter, StringBuilder value, uint update);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr window, uint message, IntPtr word, string value,
            uint flags, uint timeout, out IntPtr result);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(
            string? device, uint index, ref DisplayDevice displayDevice, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettings(
            string deviceName, int modeNumber, ref DevMode mode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ChangeDisplaySettingsEx(
            string? deviceName, ref DevMode mode, IntPtr window, int flags, IntPtr parameter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDefaultPrinter(string printerName);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetDefaultPrinter(StringBuilder name, ref int size);

        public static string GetWallpaper()
        {
            var value = new StringBuilder(32768);
            return SystemParametersInfo(SpiGetDeskWallpaper, (uint)value.Capacity, value, 0)
                ? value.ToString()
                : string.Empty;
        }

        public static void BroadcastSettingChange(string section) =>
            SendMessageTimeout(HwndBroadcast, WmSettingChange, IntPtr.Zero, section, 2, 1000, out _);

        public static string ResolveDisplayName(string requested)
        {
            for (uint index = 0; ; index++)
            {
                var device = DisplayDevice.Create();
                if (!EnumDisplayDevices(null, index, ref device, 0)) break;
                if ((device.StateFlags & DisplayDeviceAttachedToDesktop) == 0) continue;
                if (device.DeviceName.Equals(requested, StringComparison.OrdinalIgnoreCase)
                    || device.DeviceString.Contains(requested, StringComparison.CurrentCultureIgnoreCase))
                    return device.DeviceName;
            }
            throw new InvalidOperationException($"No attached display matching '{requested}' was found.");
        }

        public static string GetPrimaryDisplayName()
        {
            for (uint index = 0; ; index++)
            {
                var device = DisplayDevice.Create();
                if (!EnumDisplayDevices(null, index, ref device, 0)) break;
                if ((device.StateFlags & DisplayDevicePrimaryDevice) != 0) return device.DeviceName;
            }
            return string.Empty;
        }

        public static DevMode GetCurrentDisplayMode(string deviceName)
        {
            var mode = DevMode.Create();
            if (!EnumDisplaySettings(deviceName, EnumCurrentSettings, ref mode))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return mode;
        }

        public static string GetDefaultPrinterName()
        {
            var size = 0;
            GetDefaultPrinter(new StringBuilder(), ref size);
            if (size <= 0) return string.Empty;
            var name = new StringBuilder(size);
            return GetDefaultPrinter(name, ref size) ? name.ToString() : string.Empty;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DisplayDevice
        {
            public int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;

            public static DisplayDevice Create() => new()
            {
                Size = Marshal.SizeOf<DisplayDevice>(),
                DeviceName = string.Empty,
                DeviceString = string.Empty,
                DeviceId = string.Empty,
                DeviceKey = string.Empty
            };
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DevMode
        {
            private const int CchDeviceName = 32;
            private const int CchFormName = 32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchDeviceName)] public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchFormName)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;

            public static DevMode Create() => new()
            {
                dmDeviceName = string.Empty,
                dmFormName = string.Empty,
                dmSize = (short)Marshal.SizeOf<DevMode>()
            };
        }
    }
}
