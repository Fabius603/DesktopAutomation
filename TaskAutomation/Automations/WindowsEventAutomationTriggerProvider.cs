using System.Collections.Concurrent;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Automations;

public sealed class WindowsEventAutomationTriggerProvider : IAutomationTriggerProvider
{
    private readonly IWindowsSystemEventHub _hub;
    private readonly ConcurrentDictionary<Guid, Registration> _subscriptions = new();
    public WindowsEventAutomationTriggerProvider(IWindowsSystemEventHub hub) => _hub = hub;

    public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.WindowsEvent];
    public event Action<Guid>? Triggered;

    public Task StartAsync(CancellationToken ct = default) => _hub.StartAsync(ct);

    public async Task StopAsync(CancellationToken ct = default)
    {
        foreach (var item in _subscriptions.ToArray())
            if (_subscriptions.TryRemove(item.Key, out var subscription)) subscription.Dispose();
        await _hub.StopAsync(ct).ConfigureAwait(false);
    }

    public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
    {
        if (automation.Trigger is not WindowsEventAutomationTrigger trigger) return Task.CompletedTask;
        _subscriptions.TryRemove(automation.Id, out var old); old?.Dispose();
        Registration? registration = null;
        var lifetime = new CancellationTokenSource();
        var subscription = _hub.Subscribe(new WindowsEventSubscription
        {
            EventType = trigger.EventType,
            Filters = new Dictionary<string, string?>(trigger.Filters, StringComparer.OrdinalIgnoreCase),
            Debounce = trigger.Debounce
        }, systemEvent =>
        {
            var current = registration;
            if (current is null || !IsCurrent(automation.Id, current)) return;
            if (trigger.DelayAfterEvent <= TimeSpan.Zero) Triggered?.Invoke(automation.Id);
            else _ = DelayedTriggerAsync(automation.Id, current, trigger.DelayAfterEvent);
        });
        registration = new Registration(subscription, lifetime);
        _subscriptions[automation.Id] = registration;
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
    {
        if (_subscriptions.TryRemove(automationId, out var subscription)) subscription.Dispose();
        return Task.CompletedTask;
    }

    public DateTimeOffset? GetNextRun(Guid automationId) => null;

    private bool IsCurrent(Guid id, Registration registration) =>
        _subscriptions.TryGetValue(id, out var current) && ReferenceEquals(current, registration);

    private async Task DelayedTriggerAsync(Guid id, Registration registration, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, registration.Lifetime.Token).ConfigureAwait(false);
            if (IsCurrent(id, registration)) Triggered?.Invoke(id);
        }
        catch (OperationCanceledException) when (registration.Lifetime.IsCancellationRequested)
        {
        }
    }

    private sealed class Registration(IDisposable subscription, CancellationTokenSource lifetime) : IDisposable
    {
        public CancellationTokenSource Lifetime { get; } = lifetime;

        public void Dispose()
        {
            Lifetime.Cancel();
            subscription.Dispose();
            Lifetime.Dispose();
        }
    }
}
