using System.Windows;
using DesktopAutomationApp.Views.Library;

namespace TaskAutomation.Tests.Organization;

public sealed class LibraryDragPreviewPositionTests
{
    [Fact]
    public void PreviewStaysOffsetFromPointerInAvailableSpace()
    {
        var position = LibraryDragPreviewPosition.Calculate(
            new Point(100, 80),
            new Size(180, 36),
            new Size(600, 400));

        Assert.Equal(new Point(116, 96), position);
    }

    [Fact]
    public void PreviewStopsContinuouslyInsideViewAtBottomRightEdge()
    {
        var position = LibraryDragPreviewPosition.Calculate(
            new Point(590, 390),
            new Size(180, 36),
            new Size(600, 400));

        Assert.Equal(new Point(412, 356), position);
        Assert.InRange(position.X, 8, 412);
        Assert.InRange(position.Y, 8, 356);
    }

    [Fact]
    public void PreviewIsClampedForPointerOutsideView()
    {
        var position = LibraryDragPreviewPosition.Calculate(
            new Point(-500, -500),
            new Size(180, 36),
            new Size(600, 400));

        Assert.Equal(new Point(8, 8), position);
    }
}
