namespace DesktopAutomationApp.Localization;

public static class LibrarySubtitleFormatter
{
    public static string Job(int activeStepCount, bool repeating) =>
        Loc.Format(
            "Ui.Library.JobSubtitle",
            activeStepCount,
            Loc.Get(repeating
                ? "Ui.Library.JobRepeating"
                : "Ui.Library.JobSingleRun"));

    public static string Automation(bool active, string trigger, string action) =>
        Loc.Format(
            "Ui.Library.AutomationSubtitle",
            active ? Loc.Get("Ui.Automation.Details.Active") : Loc.Get("Ui.Library.Inactive"),
            trigger,
            action);
}
