using System.Windows;

namespace DesktopAutomationApp.Views.Library;

public static class LibraryDragPreviewPosition
{
    private const double CursorOffset = 16;
    private const double EdgeMargin = 8;

    public static Point Calculate(Point pointer, Size preview, Size bounds)
    {
        return new Point(
            ClampToBounds(pointer.X + CursorOffset, preview.Width, bounds.Width),
            ClampToBounds(pointer.Y + CursorOffset, preview.Height, bounds.Height));
    }

    private static double ClampToBounds(double value, double previewLength, double boundsLength)
    {
        var maximum = Math.Max(EdgeMargin, boundsLength - previewLength - EdgeMargin);
        return Math.Clamp(value, EdgeMargin, maximum);
    }
}
