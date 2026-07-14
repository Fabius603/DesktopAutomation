using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace TaskAutomation.Automations;

public sealed class WindowAutomationTriggerProvider : IAutomationTriggerProvider
{
    private sealed record WindowInfo(string ProcessName, string Title);

    private const uint EventSystemForeground = 0x0003;
    private const uint EventObjectCreate = 0x8000;
    private const uint EventObjectDestroy = 0x8001;
    private const uint EventObjectNameChange = 0x800C;
    private const int ObjIdWindow = 0;
    private const uint WineventOutOfContext = 0x0000;
    private const uint WineventSkipOwnProcess = 0x0002;
    private const uint WmQuit = 0x0012;
    private const uint GaRoot = 2;

    private readonly ConcurrentDictionary<Guid, AutomationDefinition> _entries = new();
    private readonly ConcurrentDictionary<nint, WindowInfo> _windows = new();
    private readonly ILogger<WindowAutomationTriggerProvider> _log;
    private readonly object _lifecycleGate = new();
    private readonly WinEventDelegate _callback;
    private Thread? _hookThread;
    private uint _hookThreadId;
    private CancellationTokenSource? _cts;

    public WindowAutomationTriggerProvider(ILogger<WindowAutomationTriggerProvider> log)
    {
        _log = log;
        _callback = OnWinEvent;
    }

    public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.WindowEvent];
    public event Action<Guid>? Triggered;

    public async Task StartAsync(CancellationToken ct = default)
    {
        Task readyTask;
        lock (_lifecycleGate)
        {
            if (_hookThread != null) return;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _hookThread = new Thread(() => RunHookLoop(ready))
            {
                IsBackground = true,
                Name = "Automation window-event listener"
            };
            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();
            readyTask = ready.Task;
        }

        try
        {
            await readyTask.WaitAsync(ct).ConfigureAwait(false);
        }
        catch when (!ct.IsCancellationRequested)
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        Thread? thread;
        lock (_lifecycleGate)
        {
            thread = _hookThread;
            _cts?.Cancel();
            if (_hookThreadId != 0)
                PostThreadMessage(_hookThreadId, WmQuit, 0, 0);
        }

        if (thread != null && thread != Thread.CurrentThread)
            await Task.Run(() => thread.Join(TimeSpan.FromSeconds(5)), ct).ConfigureAwait(false);

        lock (_lifecycleGate)
        {
            _hookThread = null;
            _hookThreadId = 0;
            _cts?.Dispose();
            _cts = null;
            _entries.Clear();
            _windows.Clear();
        }
    }

    public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
    {
        _entries[automation.Id] = automation;
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
    {
        _entries.TryRemove(automationId, out _);
        return Task.CompletedTask;
    }

    public DateTimeOffset? GetNextRun(Guid automationId) => null;

    private void RunHookLoop(TaskCompletionSource ready)
    {
        var hooks = new List<nint>();
        try
        {
            _hookThreadId = GetCurrentThreadId();
            PeekMessage(out _, 0, 0, 0, 0);
            hooks.Add(InstallHook(EventSystemForeground));
            hooks.Add(InstallHook(EventObjectCreate));
            hooks.Add(InstallHook(EventObjectDestroy));
            hooks.Add(InstallHook(EventObjectNameChange));
            CaptureExistingWindows();
            ready.TrySetResult();
            _log.LogInformation("Windows-Fensterereignisse für Automationen registriert.");

            while (GetMessage(out _, 0, 0, 0) > 0) { }
        }
        catch (Exception ex)
        {
            ready.TrySetException(ex);
            _log.LogError(ex, "Windows-Fensterereignisse konnten nicht registriert werden.");
        }
        finally
        {
            foreach (var hook in hooks)
                if (hook != 0) UnhookWinEvent(hook);
        }
    }

    private nint InstallHook(uint eventId)
    {
        var hook = SetWinEventHook(
            eventId,
            eventId,
            0,
            _callback,
            0,
            0,
            WineventOutOfContext | WineventSkipOwnProcess);
        if (hook == 0)
            throw new InvalidOperationException($"WinEvent-Hook 0x{eventId:X} konnte nicht registriert werden.");
        return hook;
    }

    private void OnWinEvent(
        nint hook,
        uint eventId,
        nint hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime)
    {
        if (hwnd == 0 || _cts?.IsCancellationRequested != false) return;
        if (eventId != EventSystemForeground && (idObject != ObjIdWindow || idChild != 0)) return;
        if (GetAncestor(hwnd, GaRoot) != hwnd) return;

        try
        {
            switch (eventId)
            {
                case EventObjectCreate:
                    if (TryCaptureWindow(hwnd, out var opened) && _windows.TryAdd(hwnd, opened))
                        _ = HandleWindowOpenedAsync(hwnd, opened, _cts.Token);
                    break;
                case EventObjectDestroy:
                    if (_windows.TryRemove(hwnd, out var closed))
                        FireMatching(WindowAutomationEventKind.Closed, closed);
                    break;
                case EventSystemForeground:
                    if (TryCaptureWindow(hwnd, out var focused))
                    {
                        _windows[hwnd] = focused;
                        FireMatching(WindowAutomationEventKind.Focused, focused);
                    }
                    break;
                case EventObjectNameChange:
                    if (TryCaptureWindow(hwnd, out var renamed))
                    {
                        var changed = !_windows.TryGetValue(hwnd, out var previous)
                                      || !string.Equals(previous.Title, renamed.Title, StringComparison.Ordinal);
                        _windows[hwnd] = renamed;
                        if (changed) FireMatching(WindowAutomationEventKind.TitleChanged, renamed);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Ein Fensterereignis konnte nicht verarbeitet werden.");
        }
    }

    private async Task HandleWindowOpenedAsync(nint hwnd, WindowInfo window, CancellationToken ct)
    {
        var matching = _entries.ToArray()
            .Where(pair => pair.Value.Trigger is WindowEventAutomationTrigger trigger
                           && trigger.EventKind == WindowAutomationEventKind.Opened
                           && ProcessMatches(trigger, window))
            .ToArray();
        var fired = new HashSet<Guid>();

        try
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                foreach (var pair in matching)
                {
                    if (fired.Contains(pair.Key)) continue;
                    var trigger = (WindowEventAutomationTrigger)pair.Value.Trigger;
                    if (!WindowMatches(trigger, window)) continue;
                    fired.Add(pair.Key);
                    _ = FireAfterDelayAsync(pair.Key, pair.Value, trigger.DelayAfterEvent, ct);
                }

                if (fired.Count == matching.Length || !IsWindow(hwnd)) return;
                await Task.Delay(TimeSpan.FromMilliseconds(250), ct).ConfigureAwait(false);
                if (!TryCaptureWindow(hwnd, out window)) return;
                _windows[hwnd] = window;
            }
        }
        catch (OperationCanceledException) { }
    }

    private void FireMatching(WindowAutomationEventKind eventKind, WindowInfo window)
    {
        var ct = _cts?.Token;
        if (!ct.HasValue || ct.Value.IsCancellationRequested) return;
        foreach (var pair in _entries.ToArray())
        {
            if (pair.Value.Trigger is not WindowEventAutomationTrigger trigger
                || trigger.EventKind != eventKind
                || !WindowMatches(trigger, window))
                continue;
            _ = FireAfterDelayAsync(pair.Key, pair.Value, trigger.DelayAfterEvent, ct.Value);
        }
    }

    private async Task FireAfterDelayAsync(Guid id, AutomationDefinition expected, TimeSpan delay, CancellationToken ct)
    {
        try
        {
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct).ConfigureAwait(false);
            if (_entries.TryGetValue(id, out var current) && ReferenceEquals(current, expected))
                Triggered?.Invoke(id);
        }
        catch (OperationCanceledException) { }
    }

    private static bool WindowMatches(WindowEventAutomationTrigger trigger, WindowInfo window) =>
        ProcessMatches(trigger, window)
        && (string.IsNullOrWhiteSpace(trigger.WindowTitleContains)
            || window.Title.Contains(trigger.WindowTitleContains, StringComparison.OrdinalIgnoreCase));

    private static bool ProcessMatches(WindowEventAutomationTrigger trigger, WindowInfo window) =>
        string.IsNullOrWhiteSpace(trigger.ProcessName)
        || string.Equals(
            Path.GetFileNameWithoutExtension(trigger.ProcessName.Trim()),
            window.ProcessName,
            StringComparison.OrdinalIgnoreCase);

    private void CaptureExistingWindows()
    {
        EnumWindows((hwnd, _) =>
        {
            if (TryCaptureWindow(hwnd, out var window)) _windows[hwnd] = window;
            return true;
        }, 0);
    }

    private static bool TryCaptureWindow(nint hwnd, out WindowInfo window)
    {
        window = new WindowInfo(string.Empty, string.Empty);
        try
        {
            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0) return false;
            using var process = Process.GetProcessById(checked((int)processId));
            window = new WindowInfo(process.ProcessName, GetWindowTitle(hwnd));
            return true;
        }
        catch { return false; }
    }

    private static string GetWindowTitle(nint hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0) return string.Empty;
        var buffer = new char[length + 1];
        var copied = GetWindowText(hwnd, buffer, buffer.Length);
        return copied > 0 ? new string(buffer, 0, copied) : string.Empty;
    }

    private delegate void WinEventDelegate(nint hook, uint eventId, nint hwnd, int idObject, int idChild, uint eventThread, uint eventTime);
    private delegate bool EnumWindowsDelegate(nint hwnd, nint parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public nint Hwnd;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint module, WinEventDelegate callback, uint processId, uint threadId, uint flags);
    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage message, nint hwnd, uint min, uint max);
    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out NativeMessage message, nint hwnd, uint min, uint max, uint removeMessage);
    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint threadId, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsDelegate callback, nint parameter);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hwnd, [Out] char[] text, int maxCount);
    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hwnd);
    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint hwnd, uint flags);
    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hwnd);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}

public sealed class FileSystemAutomationTriggerProvider : IAutomationTriggerProvider
{
    private sealed class Registration : IDisposable
    {
        public Registration(AutomationDefinition definition, FileSystemWatcher watcher)
        {
            Definition = definition;
            Watcher = watcher;
        }

        public AutomationDefinition Definition { get; }
        public FileSystemWatcher Watcher { get; }
        public CancellationTokenSource Cancellation { get; } = new();
        public ConcurrentDictionary<string, long> EventVersions { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Dispose()
        {
            Cancellation.Cancel();
            Watcher.Dispose();
            Cancellation.Dispose();
        }
    }

    private readonly ConcurrentDictionary<Guid, Registration> _registrations = new();
    private readonly ILogger<FileSystemAutomationTriggerProvider> _log;
    private volatile bool _started;

    public FileSystemAutomationTriggerProvider(ILogger<FileSystemAutomationTriggerProvider> log) => _log = log;

    public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.FileSystemEvent];
    public event Action<Guid>? Triggered;

    public Task StartAsync(CancellationToken ct = default)
    {
        _started = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _started = false;
        foreach (var id in _registrations.Keys.ToArray())
            RemoveRegistration(id);
        return Task.CompletedTask;
    }

    public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
    {
        RemoveRegistration(automation.Id);
        if (!_started || automation.Trigger is not FileSystemAutomationTrigger trigger)
            return Task.CompletedTask;

        var directory = Environment.ExpandEnvironmentVariables(trigger.DirectoryPath.Trim());
        if (!Directory.Exists(directory))
        {
            _log.LogWarning("Automation '{Name}' überwacht einen nicht vorhandenen Ordner: {Directory}", automation.Name, directory);
            return Task.CompletedTask;
        }

        try
        {
            var watcher = new FileSystemWatcher(directory, string.IsNullOrWhiteSpace(trigger.Filter) ? "*.*" : trigger.Filter.Trim())
            {
                IncludeSubdirectories = trigger.IncludeSubdirectories,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                               | NotifyFilters.Size | NotifyFilters.CreationTime
            };
            var registration = new Registration(automation, watcher);
            watcher.Created += (_, e) => OnFileEvent(automation.Id, registration, FileSystemAutomationEventKind.Created, e.FullPath);
            watcher.Changed += (_, e) => OnFileEvent(automation.Id, registration, FileSystemAutomationEventKind.Changed, e.FullPath);
            watcher.Deleted += (_, e) => OnFileEvent(automation.Id, registration, FileSystemAutomationEventKind.Deleted, e.FullPath);
            watcher.Renamed += (_, e) => OnFileEvent(automation.Id, registration, FileSystemAutomationEventKind.Renamed, e.FullPath);
            watcher.Error += (_, e) =>
            {
                _log.LogError(e.GetException(), "Fehler bei der Ordnerüberwachung für Automation '{Name}'. Die Überwachung wird neu gestartet.", automation.Name);
                _ = RecoverRegistrationAsync(automation.Id, registration);
            };
            _registrations[automation.Id] = registration;
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ordnerüberwachung für Automation '{Name}' konnte nicht gestartet werden.", automation.Name);
        }

        return Task.CompletedTask;
    }

    public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
    {
        RemoveRegistration(automationId);
        return Task.CompletedTask;
    }

    public DateTimeOffset? GetNextRun(Guid automationId) => null;

    private void OnFileEvent(Guid id, Registration registration, FileSystemAutomationEventKind kind, string path)
    {
        if (registration.Definition.Trigger is not FileSystemAutomationTrigger trigger || trigger.EventKind != kind)
            return;
        var key = $"{kind}|{path}";
        var version = registration.EventVersions.AddOrUpdate(key, 1, (_, current) => current + 1);
        _ = FireDebouncedAsync(id, registration, trigger, path, key, version);
    }

    private async Task FireDebouncedAsync(
        Guid id,
        Registration registration,
        FileSystemAutomationTrigger trigger,
        string path,
        string key,
        long version)
    {
        try
        {
            var ct = registration.Cancellation.Token;
            await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
            if (!IsCurrent(registration, key, version)) return;

            if (trigger.WaitUntilReady
                && trigger.EventKind is FileSystemAutomationEventKind.Created or FileSystemAutomationEventKind.Changed
                && !await WaitUntilReadyAsync(path, ct).ConfigureAwait(false))
                return;

            if (IsCurrent(registration, key, version)
                && _registrations.TryGetValue(id, out var current)
                && ReferenceEquals(current, registration))
            {
                Triggered?.Invoke(id);
                RemoveVersionIfCurrent(registration, key, version);
            }
        }
        catch (OperationCanceledException) { }
    }

    private static bool IsCurrent(Registration registration, string key, long version) =>
        registration.EventVersions.TryGetValue(key, out var current) && current == version;

    private static void RemoveVersionIfCurrent(Registration registration, string key, long version) =>
        ((ICollection<KeyValuePair<string, long>>)registration.EventVersions)
        .Remove(new KeyValuePair<string, long>(key, version));

    private async Task RecoverRegistrationAsync(Guid id, Registration failedRegistration)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), failedRegistration.Cancellation.Token).ConfigureAwait(false);
            if (_started
                && _registrations.TryGetValue(id, out var current)
                && ReferenceEquals(current, failedRegistration))
                await RegisterAsync(failedRegistration.Definition, failedRegistration.Cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    private static async Task<bool> WaitUntilReadyAsync(string path, CancellationToken ct)
    {
        if (Directory.Exists(path)) return true;
        long? previousLength = null;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) return false;
                var length = info.Length;
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (previousLength == length) return true;
                previousLength = length;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            await Task.Delay(TimeSpan.FromMilliseconds(250), ct).ConfigureAwait(false);
        }
        return false;
    }

    private void RemoveRegistration(Guid id)
    {
        if (_registrations.TryRemove(id, out var registration))
            registration.Dispose();
    }
}

public sealed class SystemAutomationTriggerProvider : IAutomationTriggerProvider
{
    private readonly ConcurrentDictionary<Guid, AutomationDefinition> _entries = new();
    private bool _started;

    public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.SystemEvent];
    public event Action<Guid>? Triggered;

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_started) return Task.CompletedTask;
        _started = true;
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionEnding += OnSessionEnding;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        if (!_started) return Task.CompletedTask;
        _started = false;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionEnding -= OnSessionEnding;
        _entries.Clear();
        return Task.CompletedTask;
    }

    public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
    {
        _entries[automation.Id] = automation;
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
    {
        _entries.TryRemove(automationId, out _);
        return Task.CompletedTask;
    }

    public DateTimeOffset? GetNextRun(Guid automationId) => null;

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
            Fire(SystemAutomationEventKind.SessionLocked);
        else if (e.Reason == SessionSwitchReason.SessionUnlock)
            Fire(SystemAutomationEventKind.SessionUnlocked);
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
            Fire(SystemAutomationEventKind.Suspend);
        else if (e.Mode == PowerModes.Resume)
            Fire(SystemAutomationEventKind.Resume);
    }

    private void OnSessionEnding(object sender, SessionEndingEventArgs e) =>
        Fire(e.Reason == SessionEndReasons.Logoff
            ? SystemAutomationEventKind.UserLogoff
            : SystemAutomationEventKind.SystemShutdown);

    private void Fire(SystemAutomationEventKind kind)
    {
        foreach (var pair in _entries.ToArray())
            if (pair.Value.Trigger is SystemEventAutomationTrigger trigger && trigger.EventKind == kind)
                Triggered?.Invoke(pair.Key);
    }
}
