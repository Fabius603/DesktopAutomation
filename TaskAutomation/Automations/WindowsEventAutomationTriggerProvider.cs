using System.Collections.Concurrent;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Automations;

public sealed class WindowsEventAutomationTriggerProvider : IAutomationTriggerProvider
{
    private readonly IWindowsSystemEventHub _hub;
    private readonly ConcurrentDictionary<Guid, IDisposable> _subscriptions = new();
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
        _subscriptions[automation.Id] = _hub.Subscribe(new WindowsEventSubscription
        {
            EventType = trigger.EventType,
            Filters = new Dictionary<string, string?>(trigger.Filters, StringComparer.OrdinalIgnoreCase),
            Debounce = trigger.Debounce
        }, systemEvent =>
        {
            if (trigger.DelayAfterEvent <= TimeSpan.Zero) Triggered?.Invoke(automation.Id);
            else _ = DelayedTriggerAsync(automation.Id, trigger.DelayAfterEvent);
        });
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
    {
        if (_subscriptions.TryRemove(automationId, out var subscription)) subscription.Dispose();
        return Task.CompletedTask;
    }

    public DateTimeOffset? GetNextRun(Guid automationId) => null;

    private async Task DelayedTriggerAsync(Guid id, TimeSpan delay)
    {
        await Task.Delay(delay).ConfigureAwait(false);
        if (_subscriptions.ContainsKey(id)) Triggered?.Invoke(id);
    }
}
