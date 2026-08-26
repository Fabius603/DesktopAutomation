using System.Windows;
using DesktopAutomationApp.Views;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobStepsViewInteractionTests
{
    [Fact]
    public void DetailsDoubleClick_OnlyTogglesInsideTheNonInteractiveHeader()
    {
        Assert.True(JobStepsView.ShouldToggleDetails(
            new Point(120, 24), headerWidth: 600, headerHeight: 48, isInteractiveControl: false));

        Assert.False(JobStepsView.ShouldToggleDetails(
            new Point(120, 72), headerWidth: 600, headerHeight: 48, isInteractiveControl: false));

        Assert.False(JobStepsView.ShouldToggleDetails(
            new Point(560, 24), headerWidth: 600, headerHeight: 48, isInteractiveControl: true));
    }
}
