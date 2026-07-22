using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TaskAutomation.WindowsIntegration;

public sealed class FileSystemWindowsEventSource : IWindowsSubscriptionEventSource
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        "filesystem.changed", "filesystem.created", "filesystem.deleted", "filesystem.renamed"
    };

    public bool Supports(string eventType) => Supported.Contains(eventType);

    public IDisposable Subscribe(WindowsEventSubscription subscription, Action<WindowsSystemEvent> handler)
    {
        var configuredPath = Value(subscription, "path");
        if (string.IsNullOrWhiteSpace(configuredPath)) throw new ArgumentException("Für Dateisystemereignisse ist 'path' erforderlich.");
        var fullPath = Path.GetFullPath(configuredPath);
        var isDirectory = Directory.Exists(fullPath) || Path.EndsInDirectorySeparator(fullPath);
        var directory = isDirectory ? fullPath.TrimEnd(Path.DirectorySeparatorChar) : Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Der zu überwachende Ordner wurde nicht gefunden: {directory}");

        var watcher = new FileSystemWatcher(directory, isDirectory ? "*" : Path.GetFileName(fullPath))
        {
            IncludeSubdirectories = isDirectory && BoolValue(subscription, "include_subdirectories"),
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite |
                           NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.Security
        };
        void Emit(string concreteType, string change, string path, string? oldPath = null)
        {
            if (!string.Equals(subscription.EventType, "filesystem.changed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(subscription.EventType, concreteType, StringComparison.OrdinalIgnoreCase)) return;
            handler(new WindowsSystemEvent(subscription.EventType, WindowsEventCategory.FileSystem, DateTimeOffset.Now,
                path, new Dictionary<string, string?>
                {
                    ["path"] = path, ["old_path"] = oldPath, ["change"] = change,
                    ["name"] = Path.GetFileName(path), ["include_subdirectories"] = watcher.IncludeSubdirectories.ToString()
                }));
        }
        watcher.Created += (_, e) => Emit("filesystem.created", "created", e.FullPath);
        watcher.Changed += (_, e) => Emit("filesystem.changed", "changed", e.FullPath);
        watcher.Deleted += (_, e) => Emit("filesystem.deleted", "deleted", e.FullPath);
        watcher.Renamed += (_, e) => Emit("filesystem.renamed", "renamed", e.FullPath, e.OldFullPath);
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private static string Value(WindowsEventSubscription s, string key) =>
        s.Filters.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
    private static bool BoolValue(WindowsEventSubscription s, string key) =>
        bool.TryParse(Value(s, key), out var value) && value;
}

/// <summary>Uses global input hooks and one deadline timer per threshold; it never scans state periodically.</summary>
public sealed class InputIdleWindowsEventSource : IWindowsSubscriptionEventSource, IDisposable
{
    private sealed class Registration : IDisposable
    {
        private readonly InputIdleWindowsEventSource _owner;
        public Guid Id { get; } = Guid.NewGuid();
        public long ThresholdMs { get; }
        public Action<WindowsSystemEvent> Handler { get; }
        public Timer Timer { get; }
        public bool IsIdle;
        public Registration(InputIdleWindowsEventSource owner, long thresholdMs, Action<WindowsSystemEvent> handler)
        {
            _owner = owner; ThresholdMs = thresholdMs; Handler = handler;
            Timer = new Timer(_ => owner.ReachedThreshold(this), null, Timeout.Infinite, Timeout.Infinite);
        }
        public void Dispose() { Timer.Dispose(); _owner._registrations.TryRemove(Id, out _); }
    }

    private readonly ConcurrentDictionary<Guid, Registration> _registrations = new();
    private readonly object _gate = new();
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private HookProc? _keyboardProc;
    private HookProc? _mouseProc;
    private Exception? _hookFailure;

    public bool Supports(string eventType) => string.Equals(eventType, "input.idle.changed", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(eventType, "input.idle.entered", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(eventType, "input.idle.left", StringComparison.OrdinalIgnoreCase);

    public IDisposable Subscribe(WindowsEventSubscription subscription, Action<WindowsSystemEvent> handler)
    {
        var threshold = subscription.Filters.TryGetValue("threshold_ms", out var text) && long.TryParse(text, out var parsed)
            ? Math.Max(100, parsed) : 60_000;
        EnsureHooks();
        Action<WindowsSystemEvent> filtered = e =>
        {
            if (string.Equals(subscription.EventType, "input.idle.changed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(subscription.EventType, e.EventType, StringComparison.OrdinalIgnoreCase)) handler(e with { EventType = subscription.EventType });
        };
        var registration = new Registration(this, threshold, filtered);
        _registrations[registration.Id] = registration;
        var idle = CurrentIdleMilliseconds();
        if (idle >= threshold) ReachedThreshold(registration);
        else registration.Timer.Change(TimeSpan.FromMilliseconds(threshold - idle), Timeout.InfiniteTimeSpan);
        return registration;
    }

    private void ReachedThreshold(Registration registration)
    {
        if (!_registrations.ContainsKey(registration.Id) || registration.IsIdle) return;
        registration.IsIdle = true;
        registration.Handler(Event("input.idle.entered", "entered", registration.ThresholdMs));
    }

    private void OnInput()
    {
        foreach (var registration in _registrations.Values)
        {
            if (registration.IsIdle)
            {
                registration.IsIdle = false;
                registration.Handler(Event("input.idle.left", "left", registration.ThresholdMs));
            }
            registration.Timer.Change(TimeSpan.FromMilliseconds(registration.ThresholdMs), Timeout.InfiniteTimeSpan);
        }
    }

    private static WindowsSystemEvent Event(string type, string state, long threshold) => new(type, WindowsEventCategory.Input,
        DateTimeOffset.Now, Data: new Dictionary<string, string?> { ["state"] = state, ["threshold_ms"] = threshold.ToString() });

    private void EnsureHooks()
    {
        lock (_gate)
        {
            if (_thread is not null) return;
            using var ready = new ManualResetEventSlim();
            _thread = new Thread(() =>
            {
                _threadId = GetCurrentThreadId();
                _keyboardProc = HookCallback;
                _mouseProc = HookCallback;
                var module = GetModuleHandle(null);
                _keyboardHook = SetWindowsHookEx(13, _keyboardProc, module, 0);
                _mouseHook = SetWindowsHookEx(14, _mouseProc, module, 0);
                if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
                    _hookFailure = new InvalidOperationException($"Globale Eingabeereignisse konnten nicht registriert werden (Win32 {Marshal.GetLastWin32Error()}).");
                ready.Set();
                while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
                { TranslateMessage(ref message); DispatchMessage(ref message); }
                if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
                if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
            }) { IsBackground = true, Name = "Windows input event source" };
            _thread.Start();
            ready.Wait();
            if (_hookFailure is not null) throw _hookFailure;
        }
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0) OnInput();
        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    public void Dispose()
    {
        foreach (var registration in _registrations.Values) registration.Dispose();
        if (_threadId != 0) PostThreadMessage(_threadId, 0x0012, IntPtr.Zero, IntPtr.Zero);
        _thread = null;
    }

    private static long CurrentIdleMilliseconds()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info) ? Math.Max(0, unchecked(Environment.TickCount64 - info.Time)) : 0;
    }

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] private struct LastInputInfo { public uint Size; public uint Time; }
    [StructLayout(LayoutKind.Sequential)] private struct Message { public IntPtr HWnd; public uint Value; public UIntPtr WParam; public IntPtr LParam; public uint Time; public System.Drawing.Point Point; }
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern int GetMessage(out Message message, IntPtr window, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll")] private static extern bool PostThreadMessage(uint threadId, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? moduleName);
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo info);
}
