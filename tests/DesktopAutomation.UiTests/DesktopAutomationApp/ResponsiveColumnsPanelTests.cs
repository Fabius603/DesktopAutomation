using DesktopAutomationApp.Controls;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class ResponsiveColumnsPanelTests
{
    [Theory]
    [InlineData(320, 3, 170, 3, 8, 1)]
    [InlineData(348, 3, 170, 3, 8, 2)]
    [InlineData(526, 3, 170, 3, 8, 3)]
    [InlineData(900, 4, 100, 2, 8, 2)]
    public void CalculateColumnCount_UsesOnlyColumnsThatFit(
        double width,
        int itemCount,
        double minItemWidth,
        int maxColumns,
        double spacing,
        int expectedColumns)
    {
        Assert.Equal(expectedColumns, ResponsiveColumnsPanel.CalculateColumnCount(
            width, itemCount, minItemWidth, maxColumns, spacing));
    }

    [Theory]
    [InlineData(0, 3, 170, 3, 8, 1)]
    [InlineData(320, 0, 170, 3, 8, 0)]
    [InlineData(320, 3, 0, 0, -5, 1)]
    public void CalculateColumnCount_HandlesConstrainedOrEmptyLayouts(
        double width,
        int itemCount,
        double minItemWidth,
        int maxColumns,
        double spacing,
        int expectedColumns)
    {
        Assert.Equal(expectedColumns, ResponsiveColumnsPanel.CalculateColumnCount(
            width, itemCount, minItemWidth, maxColumns, spacing));
    }
}
