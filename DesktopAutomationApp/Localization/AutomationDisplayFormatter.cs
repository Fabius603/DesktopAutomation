using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.Localization;

public static class AutomationDisplayFormatter
{
    public static string Trigger(AutomationTrigger trigger) => trigger switch
    {
        HotkeyAutomationTrigger hotkey => hotkey.VirtualKeyCode == 0
            ? Loc.Get("Automation.Hotkey.NotSet")
            : HotkeyTextFormatter.Format(hotkey.Modifiers, hotkey.VirtualKeyCode),
        OnceAtAutomationTrigger once => Loc.Format("Automation.Trigger.OnceAt", once.RunAt.LocalDateTime.ToString("g", LocalizationService.Instance.CurrentCulture)),
        ScheduleAutomationTrigger schedule => FormatSchedule(schedule),
        IntervalAutomationTrigger interval => Loc.Format("Automation.Trigger.Interval", FormatInterval(interval.Interval)),
        ProcessStartedAutomationTrigger process => FormatProcess(process, "Automation.Trigger.ProcessStarted"),
        ProcessExitedAutomationTrigger process => FormatProcess(process, "Automation.Trigger.ProcessExited"),
        _ => trigger.Kind.ToString()
    };

    public static string Action(AutomationAction action)
    {
        var type = action.ActionType == AutomationActionTarget.Makro ? Loc.Get("Common.Macro") : Loc.Get("Common.Job");
        var name = string.IsNullOrWhiteSpace(action.Name) ? Loc.Get("Common.NotSet") : action.Name;
        return $"{type} \"{name}\"";
    }

    private static string FormatSchedule(ScheduleAutomationTrigger schedule)
    {
        var culture = LocalizationService.Instance.CurrentCulture;
        var days = schedule.Days.Count == 7
            ? Loc.Get("Automation.Trigger.Daily")
            : string.Join(", ", schedule.Days.OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d)
                .Select(culture.DateTimeFormat.GetAbbreviatedDayName));
        return Loc.Format("Automation.Trigger.Schedule", days, schedule.TimeOfDay.ToString("t", culture));
    }

    private static string FormatInterval(TimeSpan interval)
    {
        if (interval.TotalHours >= 1 && interval.TotalHours % 1 == 0)
            return Loc.Format("Automation.Interval.Hours", interval.TotalHours);
        if (interval.TotalMinutes >= 1 && interval.TotalMinutes % 1 == 0)
            return Loc.Format("Automation.Interval.Minutes", interval.TotalMinutes);
        return Loc.Format("Automation.Interval.Seconds", Math.Max(1, interval.TotalSeconds));
    }

    private static string FormatProcess(ProcessAutomationTrigger process, string eventKey)
    {
        var name = string.IsNullOrWhiteSpace(process.ProcessName) ? Loc.Get("Common.NotSet") : process.ProcessName;
        var result = Loc.Format(eventKey, name);
        if (!string.IsNullOrWhiteSpace(process.WindowTitleContains))
            result += Loc.Format("Automation.Trigger.WindowTitle", process.WindowTitleContains);
        if (process.DelayAfterEvent > TimeSpan.Zero)
            result += Loc.Format("Automation.Trigger.AfterSeconds", process.DelayAfterEvent.TotalSeconds);
        return result;
    }
}

