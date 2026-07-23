using TaskAutomation.Automations;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.Automations;

public sealed class WindowsEventAutomationTriggerProviderTests
{
    [Fact]
    public async Task RegisterAsync_ReplacingRegistration_CancelsPendingDelayedTrigger()
    {
        var hub = new RecordingWindowsEventHub();
        var provider = new WindowsEventAutomationTriggerProvider(hub);
        var automation = Definition(TimeSpan.FromMilliseconds(60));
        var triggerCount = 0;
        provider.Triggered += _ => Interlocked.Increment(ref triggerCount);

        await provider.RegisterAsync(automation);
        hub.Fire(0);
        await provider.RegisterAsync(automation);
        await Task.Delay(150);

        Assert.Equal(0, triggerCount);

        hub.Fire(1);
        await WaitUntilAsync(() => Volatile.Read(ref triggerCount) == 1);
    }

    [Fact]
    public async Task UnregisterAsync_CancelsPendingDelayedTrigger()
    {
        var hub = new RecordingWindowsEventHub();
        var provider = new WindowsEventAutomationTriggerProvider(hub);
        var automation = Definition(TimeSpan.FromMilliseconds(60));
        var triggerCount = 0;
        provider.Triggered += _ => Interlocked.Increment(ref triggerCount);

        await provider.RegisterAsync(automation);
        hub.Fire(0);
        await provider.UnregisterAsync(automation.Id);
        await Task.Delay(150);

        Assert.Equal(0, triggerCount);
    }

    private static AutomationDefinition Definition(TimeSpan delay) => new()
    {
        Name = "windows event",
        Trigger = new WindowsEventAutomationTrigger
        {
            EventType = "printer.job.added",
            DelayAfterEvent = delay
        },
        Action = new AutomationAction { Name = "job", JobId = Guid.NewGuid(), ActionType = AutomationActionTarget.Job }
    };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class RecordingWindowsEventHub : IWindowsSystemEventHub
    {
        private readonly List<Action<WindowsSystemEvent>> _handlers = [];

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IDisposable Subscribe(WindowsEventSubscription subscription, Action<WindowsSystemEvent> handler)
        {
            _handlers.Add(handler);
            return new CallbackDisposable();
        }

        public void Fire(int registrationIndex) =>
            _handlers[registrationIndex](new WindowsSystemEvent("printer.job.added", WindowsEventCategory.Printer,
                DateTimeOffset.UtcNow));

        private sealed class CallbackDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
