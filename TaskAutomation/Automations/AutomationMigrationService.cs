using TaskAutomation.Hotkeys;

namespace TaskAutomation.Automations
{
    public interface IAutomationMigrationService
    {
        Task<IReadOnlyList<AutomationDefinition>> MigrateHotkeysAsync(
            IReadOnlyList<HotkeyDefinition> hotkeys,
            CancellationToken ct = default);
    }

    public sealed class AutomationMigrationService : IAutomationMigrationService
    {
        public Task<IReadOnlyList<AutomationDefinition>> MigrateHotkeysAsync(
            IReadOnlyList<HotkeyDefinition> hotkeys,
            CancellationToken ct = default)
        {
            // TODO Automation: Dummy-Migration durch echte einmalige Migration von Configs/Hotkey nach Configs/Automation ersetzen.
            IReadOnlyList<AutomationDefinition> result = hotkeys
                .Select(hotkey => new AutomationDefinition
                {
                    Id = hotkey.Id,
                    Name = hotkey.Name,
                    Active = hotkey.Active,
                    Trigger = new HotkeyAutomationTrigger
                    {
                        Modifiers = hotkey.Modifiers,
                        VirtualKeyCode = hotkey.VirtualKeyCode
                    },
                    Action = new AutomationAction
                    {
                        Name = hotkey.Job.Name,
                        JobId = hotkey.Job.JobId,
                        MakroId = hotkey.Job.MakroId,
                        Command = hotkey.Job.Command,
                        ActionType = hotkey.Job.ActionType
                    }
                })
                .ToList();

            return Task.FromResult(result);
        }
    }
}
