namespace TaskAutomation.Automations
{
    public interface IAutomationEngine
    {
        Task ReloadAsync(CancellationToken ct = default);
        Task StartAsync(CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);
        Task TriggerAsync(Guid automationId, CancellationToken ct = default);
    }

    public interface IAutomationTriggerProvider
    {
        IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; }
        Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default);
        Task UnregisterAsync(Guid automationId, CancellationToken ct = default);
    }

    public sealed class AutomationEngine : IAutomationEngine
    {
        public Task ReloadAsync(CancellationToken ct = default)
        {
            // TODO Automation: aktive Automationen laden und bei den passenden Trigger-Providern registrieren.
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken ct = default)
        {
            // TODO Automation: Scheduler/Trigger-Provider starten, sobald die echte Runtime-Logik umgesetzt wird.
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            // TODO Automation: registrierte Trigger sauber stoppen und Ressourcen freigeben.
            return Task.CompletedTask;
        }

        public Task TriggerAsync(Guid automationId, CancellationToken ct = default)
        {
            // TODO Automation: RunPolicy prüfen und Aktion über IJobDispatcher/IJobLauncher ausführen.
            return Task.CompletedTask;
        }
    }

    public sealed class HotkeyAutomationTriggerProvider : IAutomationTriggerProvider
    {
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.Hotkey];

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            // TODO Automation: HotkeyAutomationTrigger an den bestehenden GlobalHotkeyService anbinden.
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            // TODO Automation: Hotkey-Registrierung für die Automation entfernen.
            return Task.CompletedTask;
        }
    }

    public sealed class ScheduleAutomationTriggerProvider : IAutomationTriggerProvider
    {
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } =
            [AutomationTriggerKind.OnceAt, AutomationTriggerKind.Schedule];

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            // TODO Automation: nächste Fälligkeit berechnen und zentralen Scheduler aktualisieren.
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            // TODO Automation: geplante Ausführung aus dem zentralen Scheduler entfernen.
            return Task.CompletedTask;
        }
    }

    public sealed class IntervalAutomationTriggerProvider : IAutomationTriggerProvider
    {
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = [AutomationTriggerKind.Interval];

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            // TODO Automation: Intervall-Trigger zentral registrieren, ohne Timer pro Automation zu erzeugen.
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            // TODO Automation: Intervall-Trigger deregistrieren.
            return Task.CompletedTask;
        }
    }

    public sealed class ProcessAutomationTriggerProvider : IAutomationTriggerProvider
    {
        public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } =
            [AutomationTriggerKind.ProcessStarted, AutomationTriggerKind.ProcessExited];

        public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
        {
            // TODO Automation: zentralen Prozess-Snapshot/Monitor für Start- und Exit-Events anbinden.
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
        {
            // TODO Automation: Prozess-Trigger aus dem zentralen Monitor entfernen.
            return Task.CompletedTask;
        }
    }
}
