using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Common.JsonRepository;
using Microsoft.Extensions.Logging;
using TaskAutomation.Hotkeys;
using TaskAutomation.Orchestration;

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
        private readonly ConcurrentDictionary<Guid, AutomationDefinition> _automations = new();
        private readonly ConcurrentDictionary<Guid, AutomationRuntimeInfo> _runtime = new();
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _executionGates = new();
        private readonly SemaphoreSlim _reloadGate = new(1, 1);
        private readonly SemaphoreSlim _pauseGate = new(1, 1);
        private bool _started;
        private volatile bool _isPaused;

        public event Action<Guid>? RuntimeChanged;
        public event Action? PausedChanged;
        public bool IsPaused => _isPaused;

        public AutomationEngine(
            IJsonRepository<AutomationDefinition> repository,
            IJobDispatcher dispatcher,
            IEnumerable<IAutomationTriggerProvider> providers,
            ILogger<AutomationEngine> log)
        {
            _repository = repository;
            _dispatcher = dispatcher;
            _providers = providers.ToList();
            _log = log;

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
                foreach (var automation in definitions)
                {
                    _automations[automation.Id] = automation;
                    if (!automation.Active)
                        continue;

                    var provider = _providers.FirstOrDefault(p => p.SupportedKinds.Contains(automation.Trigger.Kind));
                    if (provider == null)
                    {
                        _log.LogWarning("Kein Trigger-Provider für {Kind} registriert.", automation.Trigger.Kind);
                        continue;
                    }

                    await provider.RegisterAsync(automation, ct).ConfigureAwait(false);
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
            foreach (var provider in _providers)
                await provider.StartAsync(ct).ConfigureAwait(false);
            await ReloadAsync(ct).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            if (!_started) return;
            _started = false;
            foreach (var provider in _providers.Reverse())
                await provider.StopAsync(ct).ConfigureAwait(false);
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
            if (_isPaused)
                return;

            if (!_automations.TryGetValue(automationId, out var automation) || !automation.Active)
                return;

            var gate = _executionGates.GetOrAdd(automationId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_isPaused)
                    return;

                var now = DateTimeOffset.Now;
                if (!IsInsideEnabledWindow(automation.RunPolicy, TimeOnly.FromDateTime(now.LocalDateTime)))
                    return;

                var lastRun = _runtime.GetValueOrDefault(automationId)?.LastRunAt ?? automation.LastRunAt;
                if (lastRun is { } last && automation.RunPolicy.Cooldown > TimeSpan.Zero
                    && now - last < automation.RunPolicy.Cooldown)
                    return;

                if (!ExecuteAction(automation))
                    return;

                automation.LastRunAt = now;
                await _repository.SaveAsync(automation).ConfigureAwait(false);
                _runtime[automationId] = new AutomationRuntimeInfo(now, GetRuntimeInfo(automationId).NextRunAt);
                RuntimeChanged?.Invoke(automationId);
                _log.LogInformation("Automation ausgelöst: {Name}", automation.Name);
            }
            catch (Exception ex)
            {
                _runtime[automationId] = new AutomationRuntimeInfo(automation.LastRunAt, GetRuntimeInfo(automationId).NextRunAt, ex.Message);
                RuntimeChanged?.Invoke(automationId);
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
                StartTarget(isMakro, targetId.Value);
                return true;
            }

            switch (automation.RunPolicy.AlreadyRunningBehavior)
            {
                case AutomationAlreadyRunningBehavior.StartParallel:
                    StartTarget(isMakro, targetId.Value);
                    return true;
                case AutomationAlreadyRunningBehavior.Stop:
                    StopTarget(isMakro, targetId.Value);
                    return true;
                case AutomationAlreadyRunningBehavior.Restart:
                    StopTarget(isMakro, targetId.Value);
                    StartTarget(isMakro, targetId.Value);
                    return true;
                default:
                    return false;
            }
        }

        private void StartTarget(bool isMakro, Guid id)
        {
            if (isMakro) _dispatcher.StartMakro(id);
            else _dispatcher.StartJob(id);
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
        private readonly IGlobalHotkeyService _hotkeys;
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
            return Task.CompletedTask;
        }

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            if (automation.Trigger is HotkeyAutomationTrigger trigger)
                _hotkeys.RegisterAutomationHotkey(automation.Id, trigger.Modifiers, trigger.VirtualKeyCode);
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            _hotkeys.UnregisterAutomationHotkey(automationId);
            return Task.CompletedTask;
        }

        public DateTimeOffset? GetNextRun(Guid automationId) => null;
        private void OnHotkeyPressed(Guid id) => Triggered?.Invoke(id);
    }

    public sealed class ScheduleAutomationTriggerProvider : IAutomationTriggerProvider
    {
        private sealed record Entry(AutomationDefinition Definition, DateTimeOffset? NextRun);
        private readonly ConcurrentDictionary<Guid, Entry> _entries = new();
        private CancellationTokenSource? _cts;
        private Task? _loop;

        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } =
            [AutomationTriggerKind.OnceAt, AutomationTriggerKind.Schedule];
        public event Action<Guid>? Triggered;

        public Task StartAsync(CancellationToken ct = default)
        {
            if (_loop != null) return Task.CompletedTask;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _loop = RunAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            if (_cts == null) return;
            _cts.Cancel();
            try { if (_loop != null) await _loop.ConfigureAwait(false); } catch (OperationCanceledException) { }
            _cts.Dispose();
            _cts = null;
            _loop = null;
            _entries.Clear();
        }

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            _entries[automation.Id] = new Entry(automation, CalculateNext(automation.Trigger, DateTimeOffset.Now));
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            _entries.TryRemove(automationId, out _);
            return Task.CompletedTask;
        }

        public DateTimeOffset? GetNextRun(Guid automationId)
            => _entries.TryGetValue(automationId, out var entry) ? entry.NextRun : null;

        private async Task RunAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                var now = DateTimeOffset.Now;
                foreach (var pair in _entries.ToArray())
                {
                    if (pair.Value.NextRun is not { } next || next > now) continue;
                    var following = pair.Value.Definition.Trigger is OnceAtAutomationTrigger
                        ? null
                        : CalculateNext(pair.Value.Definition.Trigger, now.AddSeconds(1));
                    _entries[pair.Key] = pair.Value with { NextRun = following };
                    Triggered?.Invoke(pair.Key);
                }
            }
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

    public sealed class IntervalAutomationTriggerProvider : IAutomationTriggerProvider
    {
        private sealed record Entry(TimeSpan Interval, DateTimeOffset NextRun);
        private readonly ConcurrentDictionary<Guid, Entry> _entries = new();
        private CancellationTokenSource? _cts;
        private Task? _loop;
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.Interval];
        public event Action<Guid>? Triggered;

        public Task StartAsync(CancellationToken ct = default)
        {
            if (_loop != null) return Task.CompletedTask;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _loop = RunAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            if (_cts == null) return;
            _cts.Cancel();
            try { if (_loop != null) await _loop.ConfigureAwait(false); } catch (OperationCanceledException) { }
            _cts.Dispose(); _cts = null; _loop = null; _entries.Clear();
        }

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            if (automation.Trigger is IntervalAutomationTrigger trigger)
            {
                var interval = trigger.Interval < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : trigger.Interval;
                _entries[automation.Id] = new Entry(interval, trigger.StartImmediately ? DateTimeOffset.Now : DateTimeOffset.Now + interval);
            }
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            _entries.TryRemove(automationId, out _);
            return Task.CompletedTask;
        }

        public DateTimeOffset? GetNextRun(Guid automationId)
            => _entries.TryGetValue(automationId, out var entry) ? entry.NextRun : null;

        private async Task RunAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                var now = DateTimeOffset.Now;
                foreach (var pair in _entries.ToArray())
                {
                    if (pair.Value.NextRun > now) continue;
                    var next = pair.Value.NextRun;
                    do next += pair.Value.Interval; while (next <= now);
                    _entries[pair.Key] = pair.Value with { NextRun = next };
                    Triggered?.Invoke(pair.Key);
                }
            }
        }
    }

    public sealed class ProcessAutomationTriggerProvider : IAutomationTriggerProvider
    {
        private sealed record ProcessInfo(string Name, string WindowTitle);
        private readonly ConcurrentDictionary<Guid, AutomationDefinition> _entries = new();
        private Dictionary<int, ProcessInfo> _snapshot = new();
        private CancellationTokenSource? _cts;
        private Task? _loop;
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } =
            [AutomationTriggerKind.ProcessStarted, AutomationTriggerKind.ProcessExited];
        public event Action<Guid>? Triggered;

        public Task StartAsync(CancellationToken ct = default)
        {
            if (_loop != null) return Task.CompletedTask;
            _snapshot = CaptureProcesses();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _loop = RunAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            if (_cts == null) return;
            _cts.Cancel();
            try { if (_loop != null) await _loop.ConfigureAwait(false); } catch (OperationCanceledException) { }
            _cts.Dispose(); _cts = null; _loop = null; _entries.Clear();
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

        private async Task RunAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                var current = CaptureProcesses();
                var started = current.Where(p => !_snapshot.ContainsKey(p.Key)).Select(p => p.Value).ToArray();
                var exited = _snapshot.Where(p => !current.ContainsKey(p.Key)).Select(p => p.Value).ToArray();
                _snapshot = current;

                foreach (var pair in _entries.ToArray())
                {
                    if (pair.Value.Trigger is not ProcessAutomationTrigger trigger) continue;
                    var candidates = trigger.Kind == AutomationTriggerKind.ProcessStarted ? started : exited;
                    if (!candidates.Any(process => Matches(trigger, process))) continue;
                    _ = FireAfterDelayAsync(pair.Key, trigger.DelayAfterEvent, ct);
                }
            }
        }

        private async Task FireAfterDelayAsync(Guid id, TimeSpan delay, CancellationToken ct)
        {
            try
            {
                if (delay > TimeSpan.Zero) await Task.Delay(delay, ct).ConfigureAwait(false);
                if (_entries.ContainsKey(id)) Triggered?.Invoke(id);
            }
            catch (OperationCanceledException) { }
        }

        private static bool Matches(ProcessAutomationTrigger trigger, ProcessInfo process)
        {
            var expected = Path.GetFileNameWithoutExtension(trigger.ProcessName.Trim());
            if (!string.Equals(expected, process.Name, StringComparison.OrdinalIgnoreCase)) return false;
            return string.IsNullOrWhiteSpace(trigger.WindowTitleContains)
                || process.WindowTitle.Contains(trigger.WindowTitleContains, StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<int, ProcessInfo> CaptureProcesses()
        {
            var result = new Dictionary<int, ProcessInfo>();
            foreach (var process in Process.GetProcesses())
            {
                try { result[process.Id] = new ProcessInfo(process.ProcessName, process.MainWindowTitle ?? string.Empty); }
                catch { }
                finally { process.Dispose(); }
            }
            return result;
        }
    }
}
