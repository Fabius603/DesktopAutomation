using DesktopAutomation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;

namespace DesktopAutomation.Application.Services
{
    public sealed class AutomationApplicationService : IAutomationApplicationService
    {
        private readonly ILogger<AutomationApplicationService> _log;

        public AutomationApplicationService(ILogger<AutomationApplicationService> log)
        {
            _log = log;
        }

        public Task<IReadOnlyList<AutomationDefinition>> LoadAllAsync()
        {
            // TODO Automation: Durch Repository-Laden und Hotkey-Migration ersetzen.
            return Task.FromResult(CreateDummyAutomations());
        }

        public Task SaveAsync(AutomationDefinition automation)
        {
            // TODO Automation: Dummy-Funktion. Später persistieren und die AutomationEngine neu laden.
            _log.LogInformation("Automation-Speichern ist noch Dummy: {Name}", automation.Name);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            // TODO Automation: Dummy-Funktion. Später aus dem Repository löschen und die AutomationEngine neu laden.
            _log.LogInformation("Automation-Löschen ist noch Dummy: {Id}", id);
            return Task.CompletedTask;
        }

        public string GetStoragePath()
        {
            // TODO Automation: Pfad später aus dem echten Repository beziehen.
            return Path.GetFullPath("Configs/Automation");
        }

        private static IReadOnlyList<AutomationDefinition> CreateDummyAutomations()
        {
            return
            [
                new AutomationDefinition
                {
                    Name = "Dummy: Tagesstart",
                    Description = "Beispiel für einen geplanten Job.",
                    Active = true,
                    Trigger = new ScheduleAutomationTrigger
                    {
                        TimeOfDay = new TimeOnly(8, 0)
                    },
                    Action = new AutomationAction
                    {
                        Name = "Beispiel-Job",
                        ActionType = HotkeyActionType.Job,
                        Command = ActionCommand.Start
                    },
                    RunPolicy = new AutomationRunPolicy
                    {
                        AlreadyRunningBehavior = AutomationAlreadyRunningBehavior.Ignore,
                        Cooldown = TimeSpan.FromSeconds(10)
                    }
                },
                new AutomationDefinition
                {
                    Name = "Dummy: Browser Setup",
                    Description = "Beispiel für einen App-Start-Trigger.",
                    Active = true,
                    Trigger = new ProcessStartedAutomationTrigger
                    {
                        ProcessName = "chrome.exe",
                        DelayAfterEvent = TimeSpan.FromSeconds(2)
                    },
                    Action = new AutomationAction
                    {
                        Name = "Browser Setup",
                        ActionType = HotkeyActionType.Job,
                        Command = ActionCommand.Start
                    },
                    RunPolicy = new AutomationRunPolicy
                    {
                        AlreadyRunningBehavior = AutomationAlreadyRunningBehavior.Ignore,
                        Cooldown = TimeSpan.FromSeconds(30)
                    }
                },
                new AutomationDefinition
                {
                    Name = "Dummy: Debug Hotkey",
                    Description = "Beispiel für einen Hotkey-Trigger.",
                    Active = false,
                    Trigger = new HotkeyAutomationTrigger
                    {
                        Modifiers = KeyModifiers.Control | KeyModifiers.Alt,
                        VirtualKeyCode = 0x44
                    },
                    Action = new AutomationAction
                    {
                        Name = "Debug Job",
                        ActionType = HotkeyActionType.Job,
                        Command = ActionCommand.Toggle
                    }
                }
            ];
        }
    }
}
