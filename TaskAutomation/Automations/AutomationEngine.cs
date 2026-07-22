using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Management;
using Common.JsonRepository;
using Microsoft.Extensions.Logging;
using TaskAutomation.Hotkeys;
using TaskAutomation.Orchestration;
using TaskAutomation.Logging;

namespace TaskAutomation.Automations
{
    public sealed record AutomationRuntimeInfo(
        DateTimeOffset? LastRunAt = null,
        DateTimeOffset? NextRunAt = null,
        string? LastError = null);

    public interface IAutomationEngine
    {
        event Action<Guid>? RuntimeChanged;
        event Action? PausedChanged;
        bool IsPaused { get; }
        Task ReloadAsync(CancellationToken ct = default);
        Task StartAsync(CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);
        Task SetPausedAsync(bool paused, CancellationToken ct = default);
        Task TriggerAsync(Guid automationId, CancellationToken ct = default);
        AutomationRuntimeInfo GetRuntimeInfo(Guid automationId);
    }

    public interface IAutomationTriggerProvider
    {
        IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; }
        event Action<Guid>? Triggered;
        Task StartAsync(CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);
        Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default);
        Task UnregisterAsync(Guid automationId, CancellationToken ct = default);
        DateTimeOffset? GetNextRun(Guid automationId);
    }

    public sealed class AutomationEngine : IAutomationEngine
    {
        private readonly IJsonRepository<AutomationDefinition> _repository;
        private readonly IJobDispatcher _dispatcher;
        private readonly IReadOnlyList<IAutomationTriggerProvider> _providers;
        private readonly ILogger<AutomationEngine> _log;
        private readonly IAutomationLogService _automationLogs;
        private readonly ConcurrentDictionary<Guid, AutomationDefinition> _automations = new();
        private readonly ConcurrentDictionary<Guid, AutomationRuntimeInfo> _runtime = new();
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _executionGates = new();
        private readonly SemaphoreSlim _reloadGate = new(1, 1);
        private readonly SemaphoreSlim _pauseGate = new(1, 1);
        private volatile bool _started;
        private volatile bool _isPaused;

        public event Action<Guid>? RuntimeChanged;
        public event Action? PausedChanged;
        public bool IsPaused => _isPaused;

        public AutomationEngine(
            IJsonRepository<AutomationDefinition> repository,
            IJobDispatcher dispatcher,
            IEnumerable<IAutomationTriggerProvider> providers,
            ILogger<AutomationEngine> log,
            IAutomationLogService automationLogs)
        {
            _repository = repository;
            _dispatcher = dispatcher;
            _providers = providers.ToList();
            _log = log;
            _automationLogs = automationLogs;

            foreach (var provider in _providers)
                provider.Triggered += OnTriggered;
        }

        public async Task ReloadAsync(CancellationToken ct = default)
        {
            await _reloadGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var previousIds = _automations.Keys.ToArray();
                foreach (var id in previousIds)
                    foreach (var provider in _providers)
                        await provider.UnregisterAsync(id, ct).ConfigureAwait(false);

                _automations.Clear();
                var definitions = await _repository.LoadAllAsync().ConfigureAwait(false);
                _automationLogs.Synchronize(definitions);
                foreach (var automation in definitions)
                {
                    _automations[automation.Id] = automation;
                    if (!automation.Active)
                        continue;

                    var provider = _providers.FirstOrDefault(p => p.SupportedKinds.Contains(automation.Trigger.Kind));
                    if (provider == null)
                    {
                        _log.LogWarning("Kein Trigger-Provider für {Kind} registriert.", automation.Trigger.Kind);
                        _automationLogs.Write(automation.Id, ExecutionLogLevel.Warning,
                            "Trigger konnte nicht registriert werden.", $"Kein Provider für Trigger-Typ: {automation.Trigger.Kind}");
                        continue;
                    }

                    await provider.RegisterAsync(automation, ct).ConfigureAwait(false);
                    _automationLogs.Write(automation.Id, ExecutionLogLevel.Information,
                        "Trigger registriert.", $"Typ: {automation.Trigger.Kind}");
                }

                foreach (var automation in definitions)
                    RuntimeChanged?.Invoke(automation.Id);
                _log.LogInformation("AutomationEngine neu geladen: {Count} Definitionen.", definitions.Count);
            }
            finally
            {
                _reloadGate.Release();
            }
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            if (_started) return;
            _started = true;
            _log.LogInformation("Automationssystem wird gestartet. {ProviderCount} Trigger-Provider werden initialisiert.", _providers.Count);
            foreach (var provider in _providers)
                await provider.StartAsync(ct).ConfigureAwait(false);
            await ReloadAsync(ct).ConfigureAwait(false);
            _log.LogInformation("Automationssystem gestartet.");
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            if (!_started) return;
            _log.LogInformation("Automationssystem wird beendet.");
            _started = false;
            foreach (var provider in _providers.Reverse())
                await provider.StopAsync(ct).ConfigureAwait(false);
            _log.LogInformation("Automationssystem beendet.");
        }

        public async Task SetPausedAsync(bool paused, CancellationToken ct = default)
        {
            await _pauseGate.WaitAsync(ct).ConfigureAwait(false);
            bool changed = false;
            try
            {
                if (_isPaused == paused)
                    return;

                _isPaused = paused;
                changed = true;
                _log.LogInformation("Automationen werden {State}.", paused ? "pausiert" : "fortgesetzt");

                if (!_started)
                    return;

                if (paused)
                {
                    foreach (var provider in _providers.Reverse())
                        await provider.StopAsync(ct).ConfigureAwait(false);
                }
                else
                {
                    foreach (var provider in _providers)
                        await provider.StartAsync(ct).ConfigureAwait(false);
                    await ReloadAsync(ct).ConfigureAwait(false);
                }
                _log.LogInformation("Automationen wurden {State}.", paused ? "pausiert" : "fortgesetzt");
            }
            catch
            {
                _isPaused = !paused;
                changed = false;
                throw;
            }
            finally
            {
                _pauseGate.Release();
                if (changed)
                    PausedChanged?.Invoke();
            }
        }

        public AutomationRuntimeInfo GetRuntimeInfo(Guid automationId)
        {
            var current = _runtime.GetValueOrDefault(automationId) ?? new AutomationRuntimeInfo();
            var next = _providers.Select(p => p.GetNextRun(automationId)).FirstOrDefault(value => value.HasValue);
            var persistedLastRun = _automations.GetValueOrDefault(automationId)?.LastRunAt;
            return current with { LastRunAt = current.LastRunAt ?? persistedLastRun, NextRunAt = next };
        }

        public async Task TriggerAsync(Guid automationId, CancellationToken ct = default)
        {
            if (!_automations.TryGetValue(automationId, out var automation))
                return;

            _automationLogs.Write(automationId, ExecutionLogLevel.Debug, "Trigger erkannt.", $"Typ: {automation.Trigger.Kind}");
            if (!_started || _isPaused)
            {
                _automationLogs.Write(automationId, ExecutionLogLevel.Information, "Trigger ignoriert.", "Automationen sind pausiert.");
                return;
            }

            if (!automation.Active)
            {
                _automationLogs.Write(automationId, ExecutionLogLevel.Information, "Trigger ignoriert.", "Automation ist deaktiviert.");
                return;
            }

            var gate = _executionGates.GetOrAdd(automationId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!_started || _isPaused)
                {
                    _automationLogs.Write(automationId, ExecutionLogLevel.Information, "Ausführung übersprungen.", "Automationen wurden pausiert.");
                    return;
                }

                var now = DateTimeOffset.Now;
                if (!IsInsideEnabledWindow(automation.RunPolicy, TimeOnly.FromDateTime(now.LocalDateTime)))
                {
                    _automationLogs.Write(automationId, ExecutionLogLevel.Information, "Ausführung übersprungen.", "Außerhalb des erlaubten Zeitfensters.");
                    return;
                }

                var lastRun = _runtime.GetValueOrDefault(automationId)?.LastRunAt ?? automation.LastRunAt;
                if (lastRun is { } last && automation.RunPolicy.Cooldown > TimeSpan.Zero
                    && now - last < automation.RunPolicy.Cooldown)
                {
                    _automationLogs.Write(automationId, ExecutionLogLevel.Information, "Ausführung übersprungen.", "Cooldown ist noch aktiv.");
                    return;
                }

                if (!ExecuteAction(automation))
                {
                    _automationLogs.Write(automationId, ExecutionLogLevel.Information, "Ausführung übersprungen.", "Ziel läuft bereits.");
                    return;
                }

                automation.LastRunAt = now;
                await _repository.SaveAsync(automation).ConfigureAwait(false);
                _runtime[automationId] = new AutomationRuntimeInfo(now, GetRuntimeInfo(automationId).NextRunAt);
                RuntimeChanged?.Invoke(automationId);
                _automationLogs.Write(automationId, ExecutionLogLevel.Information, "Aktion gestartet.", $"Ziel: {automation.Action.Name}; Typ: {automation.Action.ActionType}");
                _log.LogInformation("Automation ausgelöst: {Name}", automation.Name);
            }
            catch (Exception ex)
            {
                _runtime[automationId] = new AutomationRuntimeInfo(automation.LastRunAt, GetRuntimeInfo(automationId).NextRunAt, ex.Message);
                RuntimeChanged?.Invoke(automationId);
                _automationLogs.Write(automationId, ExecutionLogLevel.Error, "Automation fehlgeschlagen.", ex.ToString());
                _log.LogError(ex, "Automation konnte nicht ausgeführt werden: {Name}", automation.Name);
            }
            finally
            {
                gate.Release();
            }
        }

        private bool ExecuteAction(AutomationDefinition automation)
        {
            var action = automation.Action;
            var isMakro = action.ActionType == AutomationActionTarget.Makro;
            var targetId = isMakro ? action.MakroId : action.JobId;
            if (!targetId.HasValue)
                throw new InvalidOperationException($"Für die Aktion '{action.Name}' ist keine ID gesetzt.");

            var isRunning = isMakro
                ? _dispatcher.RunningMakroIds.Contains(targetId.Value)
                : _dispatcher.RunningJobIds.Contains(targetId.Value);

            if (!isRunning)
            {
                StartTarget(isMakro, targetId.Value, automation);
                return true;
            }

            switch (automation.RunPolicy.AlreadyRunningBehavior)
            {
                case AutomationAlreadyRunningBehavior.StartParallel:
                    StartTarget(isMakro, targetId.Value, automation);
                    return true;
                case AutomationAlreadyRunningBehavior.Stop:
                    StopTarget(isMakro, targetId.Value);
                    return true;
                case AutomationAlreadyRunningBehavior.Restart:
                    StopTarget(isMakro, targetId.Value);
                    StartTarget(isMakro, targetId.Value, automation);
                    return true;
                default:
                    return false;
            }
        }

        private void StartTarget(bool isMakro, Guid id, AutomationDefinition automation)
        {
            if (isMakro) _dispatcher.StartMakro(id);
            else _dispatcher.StartJob(id, new JobStartContext(JobStartSource.Automation, automation.Name, automation.Id));
        }

        private void StopTarget(bool isMakro, Guid id)
        {
            if (isMakro) _dispatcher.CancelMakro(id);
            else _dispatcher.CancelJobsByDefinition(id);
        }

        private static bool IsInsideEnabledWindow(AutomationRunPolicy policy, TimeOnly now)
        {
            if (policy.EnabledFrom is null && policy.EnabledUntil is null) return true;
            var from = policy.EnabledFrom ?? TimeOnly.MinValue;
            var until = policy.EnabledUntil ?? TimeOnly.MaxValue;
            return from <= until ? now >= from && now <= until : now >= from || now <= until;
        }

        private void OnTriggered(Guid id) => _ = TriggerAsync(id);
    }

    public sealed class HotkeyAutomationTriggerProvider : IAutomationTriggerProvider
    {
        private sealed class Registration(HotkeyAutomationTrigger trigger)
        {
            private readonly object _gate = new();
            private DateTimeOffset? _lastAccepted;
            public HotkeyAutomationTrigger Trigger { get; } = trigger;
            public CancellationTokenSource Lifetime { get; } = new();
            public bool TryAccept(DateTimeOffset now)
            {
                lock (_gate)
                {
                    if (Trigger.Debounce > TimeSpan.Zero && _lastAccepted is { } last && now - last < Trigger.Debounce) return false;
                    _lastAccepted = now;
                    return true;
                }
            }
        }

        private readonly IGlobalHotkeyService _hotkeys;
        private readonly ConcurrentDictionary<Guid, Registration> _registrations = new();
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.Hotkey];
        public event Action<Guid>? Triggered;

        public HotkeyAutomationTriggerProvider(IGlobalHotkeyService hotkeys) => _hotkeys = hotkeys;

        public Task StartAsync(CancellationToken ct = default)
        {
            _hotkeys.AutomationHotkeyPressed += OnHotkeyPressed;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            _hotkeys.AutomationHotkeyPressed -= OnHotkeyPressed;
            foreach (var registration in _registrations.Values) registration.Lifetime.Cancel();
            _registrations.Clear();
            return Task.CompletedTask;
        }

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            if (automation.Trigger is HotkeyAutomationTrigger trigger)
            {
                if (_registrations.TryRemove(automation.Id, out var previous)) previous.Lifetime.Cancel();
                _registrations[automation.Id] = new Registration(trigger);
                _hotkeys.RegisterAutomationHotkey(automation.Id, trigger.Modifiers, trigger.VirtualKeyCode);
            }
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            if (_registrations.TryRemove(automationId, out var registration)) registration.Lifetime.Cancel();
            _hotkeys.UnregisterAutomationHotkey(automationId);
            return Task.CompletedTask;
        }

        public DateTimeOffset? GetNextRun(Guid automationId) => null;
        private void OnHotkeyPressed(Guid id)
        {
            if (!_registrations.TryGetValue(id, out var registration) || !registration.TryAccept(DateTimeOffset.UtcNow)) return;
            if (registration.Trigger.DelayAfterEvent <= TimeSpan.Zero) Triggered?.Invoke(id);
            else _ = FireAfterDelayAsync(id, registration);
        }

        private async Task FireAfterDelayAsync(Guid id, Registration registration)
        {
            try
            {
                await Task.Delay(registration.Trigger.DelayAfterEvent, registration.Lifetime.Token).ConfigureAwait(false);
                if (_registrations.TryGetValue(id, out var current) && ReferenceEquals(current, registration)) Triggered?.Invoke(id);
            }
            catch (OperationCanceledException) when (registration.Lifetime.IsCancellationRequested) { }
        }
    }

    public sealed class TimeAutomationTriggerProvider : IAutomationTriggerProvider
    {
        private sealed record Entry(AutomationTrigger Trigger, DateTimeOffset? NextRun, TimeSpan? Interval = null);
        private static readonly TimeSpan MaximumTimerDelay = TimeSpan.FromHours(1);
        private readonly Dictionary<Guid, Entry> _entries = new();
        private readonly object _gate = new();
        private Timer? _timer;
        private bool _started;

        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } =
            [AutomationTriggerKind.OnceAt, AutomationTriggerKind.Schedule, AutomationTriggerKind.Interval];
        public event Action<Guid>? Triggered;

        public Task StartAsync(CancellationToken ct = default)
        {
            lock (_gate)
            {
                if (_started) return Task.CompletedTask;
                _started = true;
                _timer = new Timer(OnTimerElapsed);
                ArmTimerLocked(DateTimeOffset.Now);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            lock (_gate)
            {
                _started = false;
                _timer?.Dispose();
                _timer = null;
                _entries.Clear();
            }
            return Task.CompletedTask;
        }

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            var now = DateTimeOffset.Now;
            Entry entry;
            if (automation.Trigger is IntervalAutomationTrigger intervalTrigger)
            {
                var interval = intervalTrigger.Interval < TimeSpan.FromSeconds(1)
                    ? TimeSpan.FromSeconds(1)
                    : intervalTrigger.Interval;
                entry = new Entry(
                    automation.Trigger,
                    intervalTrigger.StartImmediately ? now : now + interval,
                    interval);
            }
            else
            {
                entry = new Entry(automation.Trigger, CalculateNext(automation.Trigger, now));
            }

            lock (_gate)
            {
                _entries[automation.Id] = entry;
                ArmTimerLocked(now);
            }
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            lock (_gate)
            {
                _entries.Remove(automationId);
                ArmTimerLocked(DateTimeOffset.Now);
            }
            return Task.CompletedTask;
        }

        public DateTimeOffset? GetNextRun(Guid automationId)
        {
            lock (_gate)
                return _entries.TryGetValue(automationId, out var entry) ? entry.NextRun : null;
        }

        private void OnTimerElapsed(object? state)
        {
            List<Guid> dueIds = new();
            lock (_gate)
            {
                if (!_started) return;
                var now = DateTimeOffset.Now;
                foreach (var pair in _entries.ToArray())
                {
                    if (pair.Value.NextRun is not { } next || next > now) continue;
                    DateTimeOffset? following;
                    if (pair.Value.Interval is { } interval)
                    {
                        do next += interval; while (next <= now);
                        following = next;
                    }
                    else
                    {
                        following = pair.Value.Trigger is OnceAtAutomationTrigger
                            ? null
                            : CalculateNext(pair.Value.Trigger, now);
                    }

                    _entries[pair.Key] = pair.Value with { NextRun = following };
                    dueIds.Add(pair.Key);
                }
                ArmTimerLocked(now);
            }

            foreach (var id in dueIds)
                Triggered?.Invoke(id);
        }

        private void ArmTimerLocked(DateTimeOffset now)
        {
            if (!_started || _timer == null) return;
            var next = _entries.Values
                .Where(entry => entry.NextRun.HasValue)
                .Select(entry => entry.NextRun!.Value)
                .DefaultIfEmpty(DateTimeOffset.MaxValue)
                .Min();
            if (next == DateTimeOffset.MaxValue)
            {
                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }

            var delay = next - now;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
            if (delay > MaximumTimerDelay) delay = MaximumTimerDelay;
            _timer.Change(delay, Timeout.InfiniteTimeSpan);
        }

        internal static DateTimeOffset? CalculateNext(AutomationTrigger trigger, DateTimeOffset after)
        {
            if (trigger is OnceAtAutomationTrigger once)
                return once.RunAt > after ? once.RunAt : null;
            if (trigger is not ScheduleAutomationTrigger schedule || schedule.Days.Count == 0)
                return null;

            var localAfter = after.LocalDateTime;
            for (var offset = 0; offset <= 7; offset++)
            {
                var date = localAfter.Date.AddDays(offset);
                if (!schedule.Days.Contains(date.DayOfWeek)) continue;
                var candidate = new DateTimeOffset(date + schedule.TimeOfDay.ToTimeSpan());
                if (candidate > after) return candidate;
            }
            return null;
        }
    }

    public sealed class ProcessAutomationTriggerProvider : IAutomationTriggerProvider
    {
        private sealed record ProcessInfo(string Name, string WindowTitle);
        private readonly ConcurrentDictionary<Guid, AutomationDefinition> _entries = new();
        private readonly ConcurrentDictionary<uint, ProcessInfo> _processes = new();
        private readonly ILogger<ProcessAutomationTriggerProvider> _log;
        private CancellationTokenSource? _cts;
        private ManagementEventWatcher? _startWatcher;
        private ManagementEventWatcher? _stopWatcher;
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } =
            [AutomationTriggerKind.ProcessStarted, AutomationTriggerKind.ProcessExited];
        public event Action<Guid>? Triggered;

        public ProcessAutomationTriggerProvider(ILogger<ProcessAutomationTriggerProvider> log) => _log = log;

        public Task StartAsync(CancellationToken ct = default)
        {
            if (_cts != null) return Task.CompletedTask;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _processes.Clear();
            foreach (var process in CaptureProcesses())
                _processes[process.Key] = process.Value;

            try
            {
                _startWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                _stopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
                _startWatcher.EventArrived += OnProcessStarted;
                _stopWatcher.EventArrived += OnProcessExited;
                _startWatcher.Start();
                _stopWatcher.Start();
                _log.LogInformation("Windows-Prozessereignisse für Automationen registriert.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Windows-Prozessereignisse konnten nicht registriert werden.");
                DisposeWatchers();
                _cts.Dispose();
                _cts = null;
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            if (_cts == null) return Task.CompletedTask;
            _cts.Cancel();
            DisposeWatchers();
            _cts.Dispose();
            _cts = null;
            _entries.Clear();
            _processes.Clear();
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

        private void OnProcessStarted(object sender, EventArrivedEventArgs args)
        {
            var token = _cts?.Token;
            if (!token.HasValue || token.Value.IsCancellationRequested) return;
            try
            {
                var processId = Convert.ToUInt32(args.NewEvent.Properties["ProcessID"].Value);
                var processName = NormalizeProcessName(Convert.ToString(args.NewEvent.Properties["ProcessName"].Value));
                var info = new ProcessInfo(processName, string.Empty);
                _processes[processId] = info;
                _ = HandleStartedProcessAsync(processId, info, token.Value);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Ein Prozessstart-Ereignis konnte nicht verarbeitet werden.");
            }
        }

        private void OnProcessExited(object sender, EventArrivedEventArgs args)
        {
            var token = _cts?.Token;
            if (!token.HasValue || token.Value.IsCancellationRequested) return;
            try
            {
                var processId = Convert.ToUInt32(args.NewEvent.Properties["ProcessID"].Value);
                var eventName = NormalizeProcessName(Convert.ToString(args.NewEvent.Properties["ProcessName"].Value));
                if (!_processes.TryRemove(processId, out var process))
                    process = new ProcessInfo(eventName, string.Empty);

                foreach (var pair in _entries.ToArray())
                {
                    if (pair.Value.Trigger is not ProcessExitedAutomationTrigger trigger || !Matches(trigger, process))
                        continue;
                    _ = FireAfterDelayAsync(pair.Key, pair.Value, trigger.DelayAfterEvent, token.Value);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Ein Prozessende-Ereignis konnte nicht verarbeitet werden.");
            }
        }

        private async Task HandleStartedProcessAsync(uint processId, ProcessInfo process, CancellationToken ct)
        {
            try
            {
                var matching = _entries.ToArray()
                    .Where(pair => pair.Value.Trigger is ProcessStartedAutomationTrigger trigger
                                   && ProcessNameMatches(trigger, process))
                    .ToArray();
                var fired = new HashSet<Guid>();

                foreach (var pair in matching)
                {
                    var trigger = (ProcessStartedAutomationTrigger)pair.Value.Trigger;
                    if (!string.IsNullOrWhiteSpace(trigger.WindowTitleContains)) continue;
                    fired.Add(pair.Key);
                    _ = FireAfterDelayAsync(pair.Key, pair.Value, trigger.DelayAfterEvent, ct);
                }

                // Ein neues Hauptfenster steht beim Windows-Startsignal meist noch nicht bereit.
                // Deshalb wird ausschließlich dieser neue Prozess kurz und gezielt nachgeprüft.
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!TryReadWindowTitle(processId, out var windowTitle)) return;
                    process = process with { WindowTitle = windowTitle };
                    _processes[processId] = process;

                    foreach (var pair in matching)
                    {
                        if (fired.Contains(pair.Key)) continue;
                        var trigger = (ProcessStartedAutomationTrigger)pair.Value.Trigger;
                        if (!Matches(trigger, process)) continue;
                        fired.Add(pair.Key);
                        _ = FireAfterDelayAsync(pair.Key, pair.Value, trigger.DelayAfterEvent, ct);
                    }

                    var pendingTitleFilters = matching.Any(pair => !fired.Contains(pair.Key));
                    if (!pendingTitleFilters && !string.IsNullOrWhiteSpace(windowTitle)) return;
                    await Task.Delay(TimeSpan.FromMilliseconds(250), ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Fenstertitel für Prozess {ProcessId} konnte nicht geprüft werden.", processId);
            }
        }

        private async Task FireAfterDelayAsync(
            Guid id,
            AutomationDefinition expectedDefinition,
            TimeSpan delay,
            CancellationToken ct)
        {
            try
            {
                if (delay > TimeSpan.Zero) await Task.Delay(delay, ct).ConfigureAwait(false);
                if (_entries.TryGetValue(id, out var current) && ReferenceEquals(current, expectedDefinition))
                    Triggered?.Invoke(id);
            }
            catch (OperationCanceledException) { }
        }

        private static bool Matches(ProcessAutomationTrigger trigger, ProcessInfo process)
        {
            if (!ProcessNameMatches(trigger, process)) return false;
            return string.IsNullOrWhiteSpace(trigger.WindowTitleContains)
                || process.WindowTitle.Contains(trigger.WindowTitleContains, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ProcessNameMatches(ProcessAutomationTrigger trigger, ProcessInfo process) =>
            string.Equals(
                NormalizeProcessName(trigger.ProcessName),
                process.Name,
                StringComparison.OrdinalIgnoreCase);

        private static string NormalizeProcessName(string? name) =>
            Path.GetFileNameWithoutExtension(name?.Trim() ?? string.Empty);

        private static bool TryReadWindowTitle(uint processId, out string windowTitle)
        {
            try
            {
                using var process = Process.GetProcessById(checked((int)processId));
                windowTitle = process.MainWindowTitle ?? string.Empty;
                return !process.HasExited;
            }
            catch
            {
                windowTitle = string.Empty;
                return false;
            }
        }

        private static Dictionary<uint, ProcessInfo> CaptureProcesses()
        {
            var result = new Dictionary<uint, ProcessInfo>();
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    result[checked((uint)process.Id)] = new ProcessInfo(
                        NormalizeProcessName(process.ProcessName),
                        process.MainWindowTitle ?? string.Empty);
                }
                catch { }
                finally { process.Dispose(); }
            }
            return result;
        }

        private void DisposeWatchers()
        {
            if (_startWatcher != null)
            {
                _startWatcher.EventArrived -= OnProcessStarted;
                try { _startWatcher.Stop(); } catch { }
                _startWatcher.Dispose();
                _startWatcher = null;
            }
            if (_stopWatcher != null)
            {
                _stopWatcher.EventArrived -= OnProcessExited;
                try { _stopWatcher.Stop(); } catch { }
                _stopWatcher.Dispose();
                _stopWatcher = null;
            }
        }
    }
}
