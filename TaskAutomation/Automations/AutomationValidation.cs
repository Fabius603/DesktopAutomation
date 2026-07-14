using System.IO;

namespace TaskAutomation.Automations;

public enum AutomationValidationError
{
    None,
    NameRequired,
    ActionRequired,
    HotkeyRequired,
    ProcessNameRequired,
    WindowFilterRequired,
    FolderRequired,
    FolderNotFound,
    FileFilterRequired,
    WeekdayRequired,
    IntervalPositive,
    ActiveWindowPair
}

public sealed record AutomationValidationResult(bool IsValid, AutomationValidationError Error);

public static class AutomationValidation
{
    public static bool IsAutomationAllowed(AutomationDefinition automation) => Validate(automation).IsValid;

    public static AutomationValidationResult Validate(AutomationDefinition automation)
    {
        AutomationValidationError error = automation switch
        {
            { Name: var name } when string.IsNullOrWhiteSpace(name) => AutomationValidationError.NameRequired,
            { Action: null } => AutomationValidationError.ActionRequired,
            { Action: { ActionType: AutomationActionTarget.Job, JobId: null } } => AutomationValidationError.ActionRequired,
            { Action: { ActionType: AutomationActionTarget.Makro, MakroId: null } } => AutomationValidationError.ActionRequired,
            { Trigger: HotkeyAutomationTrigger { VirtualKeyCode: 0 } } => AutomationValidationError.HotkeyRequired,
            { Trigger: ProcessAutomationTrigger { ProcessName: var name } } when string.IsNullOrWhiteSpace(name) => AutomationValidationError.ProcessNameRequired,
            { Trigger: WindowEventAutomationTrigger { ProcessName: var process, WindowTitleContains: var title } }
                when string.IsNullOrWhiteSpace(process) && string.IsNullOrWhiteSpace(title) => AutomationValidationError.WindowFilterRequired,
            { Trigger: FileSystemAutomationTrigger { DirectoryPath: var path } } when string.IsNullOrWhiteSpace(path) => AutomationValidationError.FolderRequired,
            { Trigger: FileSystemAutomationTrigger { DirectoryPath: var path } } when !Directory.Exists(Environment.ExpandEnvironmentVariables(path.Trim())) => AutomationValidationError.FolderNotFound,
            { Trigger: FileSystemAutomationTrigger { Filter: var filter } } when string.IsNullOrWhiteSpace(filter) => AutomationValidationError.FileFilterRequired,
            { Trigger: ScheduleAutomationTrigger { Days.Count: 0 } } => AutomationValidationError.WeekdayRequired,
            { Trigger: IntervalAutomationTrigger { Interval: var interval } } when interval <= TimeSpan.Zero => AutomationValidationError.IntervalPositive,
            { RunPolicy: { EnabledFrom: var from, EnabledUntil: var until } } when from.HasValue != until.HasValue => AutomationValidationError.ActiveWindowPair,
            _ => AutomationValidationError.None
        };
        return new(error == AutomationValidationError.None, error);
    }
}
