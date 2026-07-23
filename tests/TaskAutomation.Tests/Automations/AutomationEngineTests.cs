using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Automations;
using TaskAutomation.Logging;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Automations;

public sealed class AutomationEngineTests
{
    [Fact]
    public async Task StartAsync_StartsProviderAndRegistersOnlyActiveAutomations()
    {
        var active = Definition();
        var inactive = Definition();
        inactive.Active = false;
        var setup = Setup(active, inactive);
        await setup.Engine.StartAsync();
        Assert.Equal(1, setup.Provider.StartCalls);
        Assert.Equal([active.Id], setup.Provider.Registered);
    }

    [Fact]
    public async Task StartAsync_WhenOneRegistrationFails_RegistersRemainingAutomations()
    {
        var failing = Definition();
        var valid = Definition();
        var repository = new AutomationRepository(failing, valid);
        var provider = new PartiallyFailingTriggerProvider(failing.Id);
        var logs = new RecordingAutomationLogService();
        var engine = new AutomationEngine(repository, new RecordingJobDispatcher(), [provider],
            NullLogger<AutomationEngine>.Instance, logs);

        await engine.StartAsync();

        Assert.Equal([valid.Id], provider.Registered);
        Assert.Equal("registration failed", engine.GetRuntimeInfo(failing.Id).LastError);
        Assert.Contains(logs.Entries, entry => entry.AutomationId == failing.Id
            && entry.Level == ExecutionLogLevel.Error
            && entry.Details == "registration failed");
    }

    [Fact]
    public async Task StopAsync_IsIdempotentAndStopsProvidersOnce()
    {
        var setup = Setup(Definition());
        await setup.Engine.StartAsync();
        await setup.Engine.StopAsync();
        await setup.Engine.StopAsync();
        Assert.Equal(1, setup.Provider.StopCalls);
    }

    [Fact]
    public async Task TriggerAsync_BeforeStart_DoesNotStartTarget()
    {
        var definition = Definition();
        var setup = Setup(definition);
        await setup.Engine.ReloadAsync();
        await setup.Engine.TriggerAsync(definition.Id);
        Assert.Empty(setup.Dispatcher.StartedJobs);
    }

    [Fact]
    public async Task TriggerAsync_StartsJobWithAutomationContextAndPersistsLastRun()
    {
        var definition = Definition();
        var setup = Setup(definition);
        await setup.Engine.StartAsync();
        await setup.Engine.TriggerAsync(definition.Id);
        var call = Assert.Single(setup.Dispatcher.StartedJobs);
        Assert.Equal(definition.Action.JobId, call.Id);
        Assert.Equal(JobStartSource.Automation, call.Context!.Source);
        Assert.Equal(definition.Name, call.Context.SourceName);
        Assert.Equal(1, setup.Repository.SaveCalls);
        Assert.NotNull(setup.Engine.GetRuntimeInfo(definition.Id).LastRunAt);
    }

    [Fact]
    public async Task TriggerAsync_CooldownPreventsSecondExecution()
    {
        var definition = Definition();
        definition.RunPolicy.Cooldown = TimeSpan.FromHours(1);
        var setup = Setup(definition);
        await setup.Engine.StartAsync();
        await setup.Engine.TriggerAsync(definition.Id);
        await setup.Engine.TriggerAsync(definition.Id);
        Assert.Single(setup.Dispatcher.StartedJobs);
        Assert.Equal(1, setup.Repository.SaveCalls);
        Assert.Contains(setup.Logs.Entries, entry => entry.Details?.Contains("Cooldown") == true);
    }

    [Fact]
    public async Task SetPausedAsync_StopsProviderAndIgnoresTriggersUntilResumed()
    {
        var definition = Definition();
        var setup = Setup(definition);
        await setup.Engine.StartAsync();
        await setup.Engine.SetPausedAsync(true);
        await setup.Engine.TriggerAsync(definition.Id);
        Assert.True(setup.Engine.IsPaused);
        Assert.Empty(setup.Dispatcher.StartedJobs);
        Assert.Equal(1, setup.Provider.StopCalls);
        await setup.Engine.SetPausedAsync(false);
        await setup.Engine.TriggerAsync(definition.Id);
        Assert.False(setup.Engine.IsPaused);
        Assert.Single(setup.Dispatcher.StartedJobs);
        Assert.Equal(2, setup.Provider.StartCalls);
    }

    [Theory]
    [InlineData(AutomationAlreadyRunningBehavior.Ignore, 0, 0)]
    [InlineData(AutomationAlreadyRunningBehavior.StartParallel, 1, 0)]
    [InlineData(AutomationAlreadyRunningBehavior.Stop, 0, 1)]
    [InlineData(AutomationAlreadyRunningBehavior.Restart, 1, 1)]
    public async Task TriggerAsync_AppliesAlreadyRunningPolicy(AutomationAlreadyRunningBehavior behavior,
        int expectedStarts, int expectedStops)
    {
        var definition = Definition();
        definition.RunPolicy.AlreadyRunningBehavior = behavior;
        var setup = Setup(definition);
        setup.Dispatcher.MutableRunningJobs.Add(definition.Action.JobId!.Value);
        await setup.Engine.StartAsync();
        await setup.Engine.TriggerAsync(definition.Id);
        Assert.Equal(expectedStarts, setup.Dispatcher.StartedJobs.Count);
        Assert.Equal(expectedStops, setup.Dispatcher.CancelledJobDefinitions.Count);
    }

    [Fact]
    public async Task TriggerAsync_OutsideEnabledWindow_DoesNotStartTarget()
    {
        var definition = Definition();
        var now = TimeOnly.FromDateTime(DateTime.Now);
        definition.RunPolicy.EnabledFrom = now.AddMinutes(1);
        definition.RunPolicy.EnabledUntil = now.AddMinutes(2);
        var setup = Setup(definition);
        await setup.Engine.StartAsync();
        await setup.Engine.TriggerAsync(definition.Id);
        Assert.Empty(setup.Dispatcher.StartedJobs);
        Assert.Contains(setup.Logs.Entries, entry => entry.Details?.Contains("Zeitfensters") == true);
    }

    [Fact]
    public async Task ProviderEvent_TriggersRegisteredAutomation()
    {
        var definition = Definition();
        var setup = Setup(definition);
        await setup.Engine.StartAsync();
        setup.Provider.Fire(definition.Id);
        await WaitUntilAsync(() => setup.Dispatcher.StartedJobs.Count == 1);
    }

    private static AutomationDefinition Definition() => new()
    {
        Name = "automation",
        Trigger = new HotkeyAutomationTrigger { VirtualKeyCode = 65 },
        Action = new AutomationAction { Name = "job", JobId = Guid.NewGuid(), ActionType = AutomationActionTarget.Job }
    };

    private static SetupResult Setup(params AutomationDefinition[] definitions)
    {
        var repository = new AutomationRepository(definitions);
        var dispatcher = new RecordingJobDispatcher();
        var provider = new ManualAutomationTriggerProvider(AutomationTriggerKind.Hotkey);
        var logs = new RecordingAutomationLogService();
        var engine = new AutomationEngine(repository, dispatcher, [provider], NullLogger<AutomationEngine>.Instance, logs);
        return new(engine, repository, dispatcher, provider, logs);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) { timeout.Token.ThrowIfCancellationRequested(); await Task.Yield(); }
    }

    private sealed record SetupResult(AutomationEngine Engine, AutomationRepository Repository,
        RecordingJobDispatcher Dispatcher, ManualAutomationTriggerProvider Provider, RecordingAutomationLogService Logs);

    private sealed class PartiallyFailingTriggerProvider(Guid failingId) : IAutomationTriggerProvider
    {
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.Hotkey];
        public event Action<Guid>? Triggered;
        public List<Guid> Registered { get; } = [];
        public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default) => Task.CompletedTask;
        public DateTimeOffset? GetNextRun(Guid automationId) => null;

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            if (automation.Id == failingId) throw new InvalidOperationException("registration failed");
            Registered.Add(automation.Id);
            return Task.CompletedTask;
        }
    }
}
