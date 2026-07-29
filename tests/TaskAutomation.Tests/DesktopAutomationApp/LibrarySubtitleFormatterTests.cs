using DesktopAutomationApp.Localization;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class LibrarySubtitleFormatterTests
{
    [Theory]
    [InlineData(true, "4 aktive Steps · Wiederholend")]
    [InlineData(false, "4 aktive Steps · Einmalige Ausführung")]
    public void Job_ShowsExecutionMode(bool repeating, string expected)
    {
        Assert.Equal(expected, LibrarySubtitleFormatter.Job(4, repeating));
    }

    [Fact]
    public void Automation_ShowsActionBeforeTrigger()
    {
        Assert.Equal(
            "Aktiv · Aktion: Job \"Monatsabschluss\" · Täglich um 08:00",
            LibrarySubtitleFormatter.Automation(
                active: true,
                trigger: "Täglich um 08:00",
                action: "Job \"Monatsabschluss\""));
    }
}
