using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Management;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Receives window-manager, clipboard and Plug-and-Play notifications on a message-only window.</summary>
public sealed class Win32MessageWindowsEventSource : IWindowsEventSource
{
    private Thread? _thread;
    private ApplicationContext? _context;
    private MessageWindow? _window;
    public event Action<WindowsSystemEvent>? EventReceived;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_thread is not null) return Task.CompletedTask;
        using var ready = new ManualResetEventSlim();
        Exception? failure = null;
        _thread = new Thread(() =>
        {
            try
            {
                _context = new ApplicationContext();
                _window = new MessageWindow(Emit);
                ready.Set();
                Application.Run(_context);
                _window.Dispose();
            }
            catch (Exception ex) { failure = ex; ready.Set(); }
        }) { IsBackground = true, Name = "Windows native event message loop" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        ready.Wait(cancellationToken);
        if (failure is not null) throw failure;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _context?.ExitThread();
        if (_thread is not null && !_thread.Join(TimeSpan.FromSeconds(2))) _thread.Interrupt();
        _thread = null; _context = null; _window = null;
        return Task.CompletedTask;
    }

    private void Emit(string type, WindowsEventCategory category, string? resourceId, Dictionary<string, string?> data)
    {
        EventReceived?.Invoke(new WindowsSystemEvent(type, category, DateTimeOffset.Now, resourceId, data));
    }

    private sealed class MessageWindow : NativeWindow, IDisposable
    {
        private const int WmClipboardUpdate = 0x031D;
        private const int WmDeviceChange = 0x0219;
        private const int DbtDeviceArrival = 0x8000;
        private const int DbtDeviceRemoveComplete = 0x8004;
        private const int DbtDevTypeVolume = 0x00000002;
        private const int DbtDevTypeDeviceInterface = 0x00000005;
        private const uint DeviceNotifyWindowHandle = 0;
        private const uint DeviceNotifyAllInterfaceClasses = 4;
        private readonly Action<string, WindowsEventCategory, string?, Dictionary<string, string?>> _emit;
        private readonly List<IntPtr> _deviceNotifications = [];
        private readonly List<IntPtr> _windowHooks = [];
        private readonly Dictionary<IntPtr, WindowState> _knownWindows = [];
        private WinEventDelegate? _winEventCallback;
        private BluetoothSnapshot _bluetooth = ReadBluetooth();

        public MessageWindow(Action<string, WindowsEventCategory, string?, Dictionary<string, string?>> emit)
        {
            _emit = emit;
            CreateHandle(new CreateParams { Caption = "DesktopAutomation.WindowsEvents" });
            if (!AddClipboardFormatListener(Handle)) throw new InvalidOperationException("Clipboard-Ereignisse konnten nicht registriert werden.");
            var filter = new DeviceBroadcastDeviceInterface
            {
                Size = Marshal.SizeOf<DeviceBroadcastDeviceInterface>(), DeviceType = DbtDevTypeDeviceInterface
            };
            var pointer = Marshal.AllocHGlobal(filter.Size);
            try
            {
                Marshal.StructureToPtr(filter, pointer, false);
                var registration = RegisterDeviceNotification(Handle, pointer, DeviceNotifyWindowHandle | DeviceNotifyAllInterfaceClasses);
                if (registration != IntPtr.Zero) _deviceNotifications.Add(registration);
            }
            finally { Marshal.FreeHGlobal(pointer); }

            _winEventCallback = OnWinEvent;
            AddWindowHook(0x0003, 0x0003); // foreground
            AddWindowHook(0x8000, 0x8003); // create/destroy/show/hide
            AddWindowHook(0x800B, 0x800B); // move/resize
            AddWindowHook(0x0016, 0x0018); // minimize start/end
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmClipboardUpdate)
                OnClipboardChanged();
            else if (message.Msg == WmDeviceChange)
            {
                if (message.WParam.ToInt32() is DbtDeviceArrival or DbtDeviceRemoveComplete) OnDeviceChanged(message.WParam.ToInt32(), message.LParam);
                else if (message.WParam.ToInt32() == 0x0007)
                    EmitBoth("device.hardware.changed", "device.hardware.updated", WindowsEventCategory.Device, null, new() { ["change"] = "updated", ["name"] = "", ["filter_value"] = "" });
                RefreshBluetooth();
            }
            base.WndProc(ref message);
        }

        private void OnClipboardChanged()
        {
            var concrete = "clipboard.content_changed";
            var format = "unknown";
            try
            {
                if (!Clipboard.ContainsData(DataFormats.Text) && !Clipboard.ContainsImage() && !Clipboard.ContainsFileDropList())
                { concrete = "clipboard.cleared"; format = "empty"; }
                else if (Clipboard.ContainsFileDropList()) { concrete = "clipboard.files_changed"; format = "files"; }
                else if (Clipboard.ContainsImage()) { concrete = "clipboard.image_changed"; format = "image"; }
                else if (Clipboard.ContainsText()) { concrete = "clipboard.text_changed"; format = "text"; }
            }
            catch (ExternalException) { format = "busy"; }
            var data = new Dictionary<string, string?> { ["change"] = concrete.Split('.').Last(), ["format"] = format };
            _emit("clipboard.changed", WindowsEventCategory.Clipboard, null, data);
            _emit("clipboard.content_changed", WindowsEventCategory.Clipboard, null, data);
            if (concrete != "clipboard.content_changed") _emit(concrete, WindowsEventCategory.Clipboard, null, data);
        }

        private void OnDeviceChanged(int notification, IntPtr pointer)
        {
            var action = notification == DbtDeviceArrival ? "connected" : "disconnected";
            var path = DevicePath(pointer);
            var data = new Dictionary<string, string?>
            {
                ["change"] = action, ["name"] = path, ["filter_value"] = path
            };
            EmitBoth("device.hardware.changed", $"device.hardware.{action}", WindowsEventCategory.Device, path, data);

            var upper = path.ToUpperInvariant();
            if (upper.Contains("USB")) EmitBoth("device.usb.changed", $"device.usb.{action}", WindowsEventCategory.Device, path, data);
            if (upper.Contains("BTH") || upper.Contains("BLUETOOTH"))
                EmitBoth("bluetooth.state.changed", $"bluetooth.device.{action}", WindowsEventCategory.Bluetooth, path, data);
            if (upper.Contains("SWD#MMDEVAPI") || upper.Contains("AUDIO"))
                EmitBoth("audio.device.changed", $"audio.device.{action}", WindowsEventCategory.Audio, path, data);
            if (pointer != IntPtr.Zero && Marshal.ReadInt32(pointer, 4) == DbtDevTypeVolume || upper.Contains("STORAGE") || upper.Contains("VOLUME"))
            {
                EmitBoth("storage.drive.changed", $"storage.drive.{(action == "connected" ? "mounted" : "unmounted")}", WindowsEventCategory.Storage, path, data);
                if (pointer != IntPtr.Zero && Marshal.ReadInt32(pointer, 4) == DbtDevTypeVolume && (Marshal.ReadInt16(pointer, 16) & 1) != 0)
                    _emit(action == "connected" ? "storage.media.inserted" : "storage.media.removed", WindowsEventCategory.Storage, path, data);
            }
        }

        private void RefreshBluetooth()
        {
            var current = ReadBluetooth();
            foreach (var id in current.DeviceIds.Except(_bluetooth.DeviceIds, StringComparer.OrdinalIgnoreCase))
                EmitBluetooth("bluetooth.device.paired", "paired", id);
            foreach (var id in _bluetooth.DeviceIds.Except(current.DeviceIds, StringComparer.OrdinalIgnoreCase))
                EmitBluetooth("bluetooth.device.unpaired", "unpaired", id);
            if (current.RadioEnabled != _bluetooth.RadioEnabled)
                EmitBluetooth(current.RadioEnabled ? "bluetooth.radio.enabled" : "bluetooth.radio.disabled", current.RadioEnabled ? "enabled" : "disabled", current.RadioName);
            _bluetooth = current;
        }

        private void EmitBluetooth(string concrete, string change, string? id)
        {
            var data = new Dictionary<string, string?> { ["change"] = change, ["name"] = id, ["filter_value"] = id };
            _emit("bluetooth.state.changed", WindowsEventCategory.Bluetooth, id, data);
            _emit(concrete, WindowsEventCategory.Bluetooth, id, data);
        }

        private static BluetoothSnapshot ReadBluetooth()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT DeviceID,Name,ConfigManagerErrorCode FROM Win32_PnPEntity WHERE PNPClass='Bluetooth'");
                using var results = searcher.Get();
                var items = results.Cast<ManagementObject>().Select(x => new
                {
                    Id = x["DeviceID"]?.ToString() ?? string.Empty, Name = x["Name"]?.ToString() ?? string.Empty,
                    Error = Convert.ToUInt32(x["ConfigManagerErrorCode"] ?? 0)
                }).ToArray();
                var radios = items.Where(x => x.Name.Contains("radio", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("adapter", StringComparison.OrdinalIgnoreCase)).ToArray();
                var deviceIds = items.Where(x => !radios.Contains(x) && x.Id.Length > 0).Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                return new BluetoothSnapshot(deviceIds, radios.Any(x => x.Error == 0), radios.FirstOrDefault()?.Name ?? string.Empty);
            }
            catch { return new BluetoothSnapshot([], false, string.Empty); }
        }

        private void OnWinEvent(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint thread, uint time)
        {
            if (window == IntPtr.Zero || objectId != 0) return;

            if (eventType == 0x8001) // EVENT_OBJECT_DESTROY
            {
                if (_knownWindows.Remove(window, out var closed))
                    EmitWindow("closed", window, closed);
                return;
            }

            if (!TryCaptureApplicationWindow(window, out var current)) return;
            var wasKnown = _knownWindows.TryGetValue(window, out var previous);

            // CREATE frequently arrives before title/visibility are initialized. If the first
            // usable notification is SHOW, treat that as the single opening transition.
            if (!wasKnown)
            {
                if (eventType == 0x8000 && !current.Visible) return;
                _knownWindows[window] = current;
                if (eventType is 0x8000 or 0x8002)
                {
                    EmitWindow("opened", window, current);
                    return;
                }
            }

            var change = eventType switch
            {
                0x0003 => "focused",
                0x8002 when previous is not null && !previous.Visible && current.Visible => "shown",
                0x8003 when previous is not null && previous.Visible && !current.Visible => "hidden",
                0x800B when previous is not null && previous.Bounds != current.Bounds => "moved_or_resized",
                0x0016 => "minimized",
                0x0017 => "restored",
                _ => string.Empty
            };
            _knownWindows[window] = current;
            if (change.Length > 0) EmitWindow(change, window, current);
        }

        private void EmitWindow(string change, IntPtr window, WindowState state)
        {
            var data = new Dictionary<string, string?>
            {
                ["change"] = change,
                ["name"] = state.ProcessName,
                ["title"] = state.Title,
                ["process_id"] = state.ProcessId.ToString()
            };
            EmitBoth("window.changed", $"window.{change}", WindowsEventCategory.Window, window.ToString(), data);
        }

        private static bool TryCaptureApplicationWindow(IntPtr window, out WindowState state)
        {
            state = default!;
            if (!IsWindow(window) || GetAncestor(window, 2) != window) return false; // GA_ROOT
            if ((GetWindowLong(window, -20) & 0x00000080) != 0) return false; // WS_EX_TOOLWINDOW

            var titleLength = GetWindowTextLength(window);
            if (titleLength <= 0) return false;
            var title = new System.Text.StringBuilder(titleLength + 1);
            GetWindowText(window, title, title.Capacity);
            if (title.Length == 0) return false;

            GetWindowThreadProcessId(window, out var processId);
            string processName = string.Empty;
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch { }
            if (processName.Length == 0) return false;

            GetWindowRect(window, out var bounds);
            state = new WindowState(processId, processName, title.ToString(), IsWindowVisible(window), bounds);
            return true;
        }

        private sealed record WindowState(
            uint ProcessId, string ProcessName, string Title, bool Visible, WindowBounds Bounds);

        [StructLayout(LayoutKind.Sequential)]
        private readonly record struct WindowBounds(int Left, int Top, int Right, int Bottom);

        private void EmitBoth(string legacy, string concrete, WindowsEventCategory category, string? resourceId, Dictionary<string, string?> data)
        {
            _emit(legacy, category, resourceId, data);
            _emit(concrete, category, resourceId, data);
        }

        private void AddWindowHook(uint min, uint max)
        {
            var hook = SetWinEventHook(min, max, IntPtr.Zero, _winEventCallback!, 0, 0, 0);
            if (hook != IntPtr.Zero) _windowHooks.Add(hook);
        }

        private static string DevicePath(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) return string.Empty;
            var type = Marshal.ReadInt32(pointer, 4);
            if (type == DbtDevTypeDeviceInterface) return Marshal.PtrToStringUni(IntPtr.Add(pointer, 28)) ?? string.Empty;
            if (type == DbtDevTypeVolume)
            {
                var mask = Marshal.ReadInt32(pointer, 12);
                return string.Join(",", Enumerable.Range(0, 26).Where(bit => (mask & (1 << bit)) != 0).Select(bit => $"{(char)('A' + bit)}:"));
            }
            return string.Empty;
        }

        public void Dispose()
        {
            RemoveClipboardFormatListener(Handle);
            foreach (var registration in _deviceNotifications) UnregisterDeviceNotification(registration);
            foreach (var hook in _windowHooks) UnhookWinEvent(hook);
            DestroyHandle();
        }

        private sealed record BluetoothSnapshot(HashSet<string> DeviceIds, bool RadioEnabled, string RadioName);

        private delegate void WinEventDelegate(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint thread, uint time);
        [StructLayout(LayoutKind.Sequential)] private struct DeviceBroadcastDeviceInterface { public int Size; public int DeviceType; public int Reserved; public Guid ClassGuid; }
        [DllImport("user32.dll")] private static extern bool AddClipboardFormatListener(IntPtr window);
        [DllImport("user32.dll")] private static extern bool RemoveClipboardFormatListener(IntPtr window);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr filter, uint flags);
        [DllImport("user32.dll")] private static extern bool UnregisterDeviceNotification(IntPtr handle);
        [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventDelegate callback, uint process, uint thread, uint flags);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window, out WindowBounds bounds);
        [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr window);
    }
}
