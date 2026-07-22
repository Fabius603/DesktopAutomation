using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.WindowsIntegration;

public interface IWindowsEventSource
{
    event Action<WindowsSystemEvent>? EventReceived;
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

/// <summary>Event source whose native registration depends on automation filters.</summary>
public interface IWindowsSubscriptionEventSource
{
    bool Supports(string eventType);
    IDisposable Subscribe(WindowsEventSubscription subscription, Action<WindowsSystemEvent> handler);
}

public interface IWindowsSystemEventHub
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    IDisposable Subscribe(WindowsEventSubscription subscription, Action<WindowsSystemEvent> handler);
}

public sealed class WindowsSystemEventHub : IWindowsSystemEventHub
{
    private sealed record Registration(Guid Id, WindowsEventSubscription Subscription,
        Action<WindowsSystemEvent> Handler, IReadOnlyList<IDisposable> SourceSubscriptions);

    private readonly IReadOnlyList<IWindowsEventSource> _sources;
    private readonly IReadOnlyList<IWindowsSubscriptionEventSource> _subscriptionSources;
    private readonly IWindowsCapabilityCatalog _catalog;
    private readonly ILogger<WindowsSystemEventHub> _log;
    private readonly ConcurrentDictionary<Guid, Registration> _registrations = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastDispatch = new();
    private volatile bool _started;

    public WindowsSystemEventHub(IEnumerable<IWindowsEventSource> sources,
        IEnumerable<IWindowsSubscriptionEventSource> subscriptionSources,
        IWindowsCapabilityCatalog catalog, ILogger<WindowsSystemEventHub> log)
    {
        _sources = sources.ToArray();
        _subscriptionSources = subscriptionSources.ToArray();
        _catalog = catalog;
        _log = log;
        foreach (var source in _sources) source.EventReceived += Publish;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started) return;
        _started = true;
        foreach (var source in _sources)
        {
            try { await source.StartAsync(cancellationToken).ConfigureAwait(false); }
            catch (Exception ex) { _log.LogWarning(ex, "Windows-Ereignisquelle {Source} konnte nicht gestartet werden.", source.GetType().Name); }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_started) return;
        _started = false;
        foreach (var registration in _registrations.Values)
            foreach (var sourceSubscription in registration.SourceSubscriptions) sourceSubscription.Dispose();
        foreach (var source in _sources.Reverse())
        {
            try { await source.StopAsync(cancellationToken).ConfigureAwait(false); }
            catch (Exception ex) { _log.LogWarning(ex, "Windows-Ereignisquelle {Source} konnte nicht sauber beendet werden.", source.GetType().Name); }
        }
    }

    public IDisposable Subscribe(WindowsEventSubscription subscription, Action<WindowsSystemEvent> handler)
    {
        if (string.IsNullOrWhiteSpace(subscription.EventType)) throw new ArgumentException("EventType fehlt.", nameof(subscription));
        var descriptor = _catalog.Find(subscription.EventType)
            ?? throw new NotSupportedException($"Unbekanntes Windows-Ereignis: {subscription.EventType}");
        if (!descriptor.SupportsEvents) throw new NotSupportedException($"{subscription.EventType} ist kein Ereignis.");

        var id = Guid.NewGuid();
        Registration? registration = null;
        var nativeSubscriptions = _subscriptionSources.Where(x => x.Supports(subscription.EventType))
            .Select(x => x.Subscribe(subscription, systemEvent =>
            {
                if (registration is not null) DispatchIfMatching(registration, systemEvent);
            })).ToArray();
        registration = new Registration(id, subscription, handler, nativeSubscriptions);
        _registrations[id] = registration;

        return new CallbackDisposable(() =>
        {
            if (_registrations.TryRemove(id, out var removed))
                foreach (var sourceSubscription in removed.SourceSubscriptions) sourceSubscription.Dispose();
            _lastDispatch.TryRemove(id, out _);
        });
    }

    private void Publish(WindowsSystemEvent systemEvent)
    {
        foreach (var registration in _registrations.Values) DispatchIfMatching(registration, systemEvent);
    }

    private void DispatchIfMatching(Registration registration, WindowsSystemEvent systemEvent)
    {
        if (string.Equals(registration.Subscription.EventType, systemEvent.EventType, StringComparison.OrdinalIgnoreCase)
            && Matches(registration.Subscription.Filters, systemEvent.Data)) Dispatch(registration, systemEvent);
    }

    private void Dispatch(Registration registration, WindowsSystemEvent systemEvent)
    {
        var now = DateTimeOffset.UtcNow;
        if (registration.Subscription.Debounce > TimeSpan.Zero
            && _lastDispatch.TryGetValue(registration.Id, out var last)
            && now - last < registration.Subscription.Debounce) return;
        _lastDispatch[registration.Id] = now;
        try { registration.Handler(systemEvent); }
        catch (Exception ex) { _log.LogError(ex, "Windows-Ereignisempfänger für {EventType} ist fehlgeschlagen.", systemEvent.EventType); }
    }

    private static bool Matches(IReadOnlyDictionary<string, string?> filters, IReadOnlyDictionary<string, string?>? data)
    {
        if (filters.Count == 0) return true;
        if (data is null) return false;
        return filters.All(filter => data.TryGetValue(filter.Key, out var actual)
            && (string.IsNullOrWhiteSpace(filter.Value) || (actual?.Contains(filter.Value, StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        private Action? _callback = callback;
        public void Dispose() => Interlocked.Exchange(ref _callback, null)?.Invoke();
    }
}
