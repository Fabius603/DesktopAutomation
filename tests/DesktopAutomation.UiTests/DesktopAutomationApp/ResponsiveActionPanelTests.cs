using DesktopAutomationApp.Controls;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class ResponsiveActionPanelTests
{
    [Theory]
    [InlineData(320, 220, 92, 8, false)]
    [InlineData(319, 220, 92, 8, true)]
    [InlineData(199, 160, 34, 5, false)]
    [InlineData(198, 160, 34, 5, true)]
    public void ShouldStack_PreservesTheMinimumPrimaryWidth(
        double availableWidth,
        double minPrimaryWidth,
        double secondaryWidth,
        double spacing,
        bool expected)
    {
        Assert.Equal(expected, ResponsiveActionPanel.ShouldStack(
            availableWidth, minPrimaryWidth, secondaryWidth, spacing));
    }

    [Theory]
    [InlineData(0, 0, 34, -5, true)]
    [InlineData(double.PositiveInfinity, 220, 92, 8, false)]
    public void ShouldStack_HandlesConstrainedAndUnboundedLayouts(
        double availableWidth,
        double minPrimaryWidth,
        double secondaryWidth,
        double spacing,
        bool expected)
    {
        Assert.Equal(expected, ResponsiveActionPanel.ShouldStack(
            availableWidth, minPrimaryWidth, secondaryWidth, spacing));
    }
}
