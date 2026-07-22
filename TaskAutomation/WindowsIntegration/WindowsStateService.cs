using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.WindowsIntegration;

public interface IWindowsStateProvider
{
    IReadOnlyCollection<string> SupportedQueries { get; }
    Task<WindowsStateSnapshot> QueryAsync(WindowsStateQuery query, CancellationToken cancellationToken);
}

public interface IWindowsSystemStateService
{
    Task<WindowsStateSnapshot> QueryAsync(WindowsStateQuery query, CancellationToken cancellationToken = default);
}

public sealed class WindowsSystemStateService : IWindowsSystemStateService
{
    private readonly IReadOnlyList<IWindowsStateProvider> _providers;

    public WindowsSystemStateService(IEnumerable<IWindowsStateProvider> providers) => _providers = providers.ToArray();

    public Task<WindowsStateSnapshot> QueryAsync(WindowsStateQuery query, CancellationToken cancellationToken = default)
    {
        var provider = _providers.FirstOrDefault(p => p.SupportedQueries.Contains(query.QueryType, StringComparer.OrdinalIgnoreCase));
        return provider is null
            ? Task.FromResult(Failure(WindowsCapabilityStatus.Unsupported, "UNSUPPORTED_QUERY", $"Unbekannte Windows-Abfrage: {query.QueryType}"))
            : provider.QueryAsync(query, cancellationToken);
    }

    internal static WindowsStateSnapshot Failure(WindowsCapabilityStatus status, string code, string message) => new()
    {
        Status = status, ErrorCode = code, ErrorMessage = message, CapturedAt = DateTime.UtcNow
    };
}

/// <summary>Built-in provider using public .NET/Win32/WMI surfaces and no UI dependency.</summary>
public sealed class DefaultWindowsStateProvider : IWindowsStateProvider
{
    private readonly ILogger<DefaultWindowsStateProvider> _log;
    public DefaultWindowsStateProvider(ILogger<DefaultWindowsStateProvider> log) => _log = log;

    public IReadOnlyCollection<string> SupportedQueries { get; } = new[]
    {
        "network.connectivity", "audio.devices", "audio.volume", "session.state", "power.status",
        "display.monitors", "device.hardware", "device.usb", "bluetooth.devices", "filesystem.path",
        "process.running", "window.foreground", "input.idle", "clipboard.content", "printer.status",
        "storage.drives", "system.settings", "security.status", "windows_update.status", "system.lifecycle"
    };

    public async Task<WindowsStateSnapshot> QueryAsync(WindowsStateQuery query, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => Query(query), cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex) { return Fail(WindowsCapabilityStatus.AccessDenied, "ACCESS_DENIED", ex); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return Fail(WindowsCapabilityStatus.Timeout, "TIMEOUT", null); }
        catch (COMException ex) when ((uint)ex.HResult == 0x80070005) { return Fail(WindowsCapabilityStatus.AccessDenied, "ACCESS_DENIED", ex); }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Windows-Zustandsabfrage {QueryType} fehlgeschlagen.", query.QueryType);
            return Fail(WindowsCapabilityStatus.Failed, "QUERY_FAILED", ex);
        }
    }

    private static WindowsStateSnapshot Query(WindowsStateQuery query) => query.QueryType.ToLowerInvariant() switch
    {
        "network.connectivity" => Network(),
        "audio.devices" => WmiDevices("Win32_SoundDevice", query, "Name"),
        "audio.volume" => CoreAudioState.Query(),
        "session.state" => Session(),
        "power.status" => Power(),
        "display.monitors" => Displays(),
        "device.hardware" => WmiDevices("Win32_PnPEntity", query, "Name"),
        "device.usb" => WmiDevices("Win32_USBControllerDevice", query, "Dependent"),
        "bluetooth.devices" => WmiDevices("Win32_PnPEntity", WithFilter(query, "PNPClass", "Bluetooth"), "Name"),
        "filesystem.path" => FileSystem(query),
        "process.running" => ProcessState(query),
        "window.foreground" => ForegroundWindow(),
        "input.idle" => InputIdle(),
        "clipboard.content" => Clipboard(),
        "printer.status" => WmiDevices("Win32_Printer", query, "Name"),
        "storage.drives" => Storage(query),
        "system.settings" => SystemSettings(),
        "security.status" => Security(),
        "windows_update.status" => WindowsUpdate(),
        "system.lifecycle" => Lifecycle(),
        _ => Unsupported($"Unbekannte Abfrage: {query.QueryType}")
    };

    private static WindowsStateSnapshot Network()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
        var available = NetworkInterface.GetIsNetworkAvailable();
        var primary = interfaces.FirstOrDefault();
        var type = primary?.NetworkInterfaceType switch
        {
            NetworkInterfaceType.Wireless80211 => WindowsConnectionType.WiFi,
            NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet => WindowsConnectionType.Ethernet,
            NetworkInterfaceType.Ppp or NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2 => WindowsConnectionType.Mobile,
            _ => WindowsConnectionType.Unknown
        };
        return Ok() with { IsConnected = available, IsActive = available, Count = interfaces.Length,
            Name = primary?.Name ?? "", Connectivity = available ? WindowsConnectivity.Internet : WindowsConnectivity.Disconnected,
            ConnectionType = type, Items = interfaces.Select(x => x.Name).ToArray() };
    }

    private static WindowsStateSnapshot Session() => Ok() with
    {
        IsActive = true, Name = Environment.UserName, Id = Process.GetCurrentProcess().SessionId.ToString(),
        SessionState = WindowsSessionState.Active
    };

    private static WindowsStateSnapshot Power()
    {
        var status = System.Windows.Forms.SystemInformation.PowerStatus;
        var percentage = status.BatteryLifePercent < 0 ? 0 : status.BatteryLifePercent * 100d;
        return Ok() with { Percentage = percentage, IsCharging = status.BatteryChargeStatus.HasFlag(System.Windows.Forms.BatteryChargeStatus.Charging),
            PowerSource = status.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online ? WindowsPowerSource.Ac : WindowsPowerSource.Battery,
            IsConnected = status.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online };
    }

    private static WindowsStateSnapshot Displays()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        return Ok() with { Count = screens.Length, IsConnected = screens.Length > 0, Name = screens.FirstOrDefault(x => x.Primary)?.DeviceName ?? "",
            Items = screens.Select(x => $"{x.DeviceName}|{x.Bounds.Width}x{x.Bounds.Height}|Primary={x.Primary}").ToArray() };
    }

    private static WindowsStateSnapshot FileSystem(WindowsStateQuery q)
    {
        var path = Param(q, "path");
        if (string.IsNullOrWhiteSpace(path)) return Invalid("Parameter 'path' fehlt.");
        var file = File.Exists(path); var directory = Directory.Exists(path);
        return Ok() with { Exists = file || directory, Path = path, IsActive = file || directory,
            Count = directory ? Directory.EnumerateFileSystemEntries(path).LongCount() : file ? 1 : 0,
            Value = file ? new FileInfo(path).Length : 0 };
    }

    private static WindowsStateSnapshot ProcessState(WindowsStateQuery q)
    {
        var name = Path.GetFileNameWithoutExtension(Param(q, "name"));
        if (string.IsNullOrWhiteSpace(name)) return Invalid("Parameter 'name' fehlt.");
        var processes = Process.GetProcessesByName(name);
        try { return Ok() with { Exists = processes.Length > 0, IsActive = processes.Length > 0, Count = processes.Length,
            Name = name, Id = processes.FirstOrDefault()?.Id.ToString() ?? "" }; }
        finally { foreach (var p in processes) p.Dispose(); }
    }

    private static WindowsStateSnapshot ForegroundWindow()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return Ok();
        var length = GetWindowTextLength(handle); var buffer = new System.Text.StringBuilder(length + 1);
        _ = GetWindowText(handle, buffer, buffer.Capacity); _ = GetWindowThreadProcessId(handle, out var pid);
        string name = ""; try { using var process = Process.GetProcessById((int)pid); name = process.ProcessName; } catch { }
        return Ok() with { Exists = true, IsActive = true, Name = name, Text = buffer.ToString(), Id = pid.ToString() };
    }

    private static WindowsStateSnapshot InputIdle()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info)) return Unsupported("Die letzte Eingabe konnte nicht ermittelt werden.");
        var idle = unchecked(Environment.TickCount64 - info.Time);
        return Ok() with { Value = Math.Max(0, idle), Percentage = Math.Max(0, idle) / 1000d };
    }

    private static WindowsStateSnapshot Clipboard()
    {
        // Clipboard access requires an STA thread. This worker is intentionally isolated from WPF.
        WindowsStateSnapshot? result = null;
        var thread = new Thread(() =>
        {
            try
            {
                var hasText = System.Windows.Forms.Clipboard.ContainsText();
                result = Ok() with { Exists = hasText || System.Windows.Forms.Clipboard.ContainsImage(),
                    Text = hasText ? System.Windows.Forms.Clipboard.GetText() : "",
                    Name = hasText ? "Text" : System.Windows.Forms.Clipboard.ContainsImage() ? "Image" : "Unknown" };
            }
            catch (ExternalException ex) { result = WindowsSystemStateService.Failure(WindowsCapabilityStatus.Failed, "CLIPBOARD_BUSY", ex.Message); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(2))) return WindowsSystemStateService.Failure(WindowsCapabilityStatus.Timeout, "TIMEOUT", "Zwischenablage antwortet nicht.");
        return result ?? Unsupported("Zwischenablage nicht verfügbar.");
    }

    private static WindowsStateSnapshot WmiDevices(string wmiClass, WindowsStateQuery q, string displayProperty)
    {
        using var searcher = new ManagementObjectSearcher($"SELECT * FROM {wmiClass}");
        using var results = searcher.Get();
        var filterProperty = Param(q, "filter_property"); var filterValue = Param(q, "filter_value");
        var items = results.Cast<ManagementObject>().Where(item => string.IsNullOrWhiteSpace(filterValue)
            || (!string.IsNullOrWhiteSpace(filterProperty) ? item[filterProperty]?.ToString() : item[displayProperty]?.ToString() ?? "")
                ?.Contains(filterValue, StringComparison.OrdinalIgnoreCase) == true)
            .Select(item => item[displayProperty]?.ToString() ?? "").Where(x => x.Length > 0).ToArray();
        return Ok() with { Count = items.Length, Exists = items.Length > 0, IsConnected = items.Length > 0,
            Name = items.FirstOrDefault() ?? "", Items = items, DeviceState = items.Length > 0 ? WindowsDeviceState.Connected : WindowsDeviceState.Disconnected };
    }

    private static WindowsStateSnapshot Storage(WindowsStateQuery q)
    {
        var requested = Param(q, "name");
        var drives = DriveInfo.GetDrives().Where(d => string.IsNullOrWhiteSpace(requested) || d.Name.StartsWith(requested, StringComparison.OrdinalIgnoreCase)).ToArray();
        var ready = drives.Where(d => d.IsReady).ToArray();
        return Ok() with { Count = ready.Length, Exists = drives.Length > 0, IsConnected = ready.Length > 0,
            Name = ready.FirstOrDefault()?.Name ?? "", FreeSpaceGb = ready.Sum(d => d.AvailableFreeSpace) / 1024d / 1024d / 1024d,
            Items = ready.Select(d => $"{d.Name}|{d.DriveType}|{d.AvailableFreeSpace}").ToArray() };
    }

    private static WindowsStateSnapshot SystemSettings() => Ok() with
    {
        Name = TimeZoneInfo.Local.Id, Text = System.Globalization.CultureInfo.CurrentCulture.Name,
        IsEnabled = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) as int? == 0
    };

    private static WindowsStateSnapshot Security()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\mpssvc");
        return Ok() with { Exists = key is not null, IsEnabled = key?.GetValue("Start") is int start && start != 4,
            OnOffState = key?.GetValue("Start") is int value && value != 4 ? WindowsOnOffState.On : WindowsOnOffState.Off };
    }

    private static WindowsStateSnapshot WindowsUpdate()
    {
        var pending = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired") is not null
                      || Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") is not null;
        return Ok() with { PendingRestart = pending, IsActive = pending };
    }

    private static WindowsStateSnapshot Lifecycle() => Ok() with
    {
        Value = Environment.TickCount64, Percentage = Environment.TickCount64 / 1000d,
        CapturedAt = DateTime.UtcNow, Text = Environment.OSVersion.VersionString
    };

    private static WindowsStateQuery WithFilter(WindowsStateQuery source, string property, string value) => new()
    {
        QueryType = source.QueryType,
        Parameters = new(source.Parameters, StringComparer.OrdinalIgnoreCase) { ["filter_property"] = property, ["filter_value"] = value }
    };
    private static string Param(WindowsStateQuery q, string name) => q.Parameters.TryGetValue(name, out var value) ? value ?? "" : "";
    private static WindowsStateSnapshot Ok() => new() { Status = WindowsCapabilityStatus.Success, CapturedAt = DateTime.UtcNow };
    private static WindowsStateSnapshot Unsupported(string message) => WindowsSystemStateService.Failure(WindowsCapabilityStatus.Unsupported, "UNSUPPORTED", message);
    private static WindowsStateSnapshot Invalid(string message) => WindowsSystemStateService.Failure(WindowsCapabilityStatus.Failed, "INVALID_PARAMETERS", message);
    private static WindowsStateSnapshot Fail(WindowsCapabilityStatus status, string code, Exception? ex) =>
        WindowsSystemStateService.Failure(status, code, ex?.Message ?? code);

    [StructLayout(LayoutKind.Sequential)] private struct LastInputInfo { public uint Size; public uint Time; }
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo info);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
