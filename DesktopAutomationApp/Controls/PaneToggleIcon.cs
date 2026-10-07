using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopAutomationApp.Controls;

/// <summary>A rounded window outline with a divider on the side of the controlled pane.</summary>
public sealed class PaneToggleIcon : Control
{
    public PaneToggleIcon()
    {
        Focusable = false;
        IsHitTestVisible = false;
    }

    public static readonly DependencyProperty IsRightPaneProperty = DependencyProperty.Register(
        nameof(IsRightPane), typeof(bool), typeof(PaneToggleIcon),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsRightPane
    {
        get => (bool)GetValue(IsRightPaneProperty);
        set => SetValue(IsRightPaneProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var bounds = new Rect(2, 3, Math.Max(0, ActualWidth - 4), Math.Max(0, ActualHeight - 6));
        if (bounds.Width == 0 || bounds.Height == 0) return;
        var pen = new Pen(Foreground, 1.25);
        drawingContext.DrawRoundedRectangle(null, pen, bounds, 3, 3);
        var divider = bounds.Left + bounds.Width * (IsRightPane ? 0.68 : 0.32);
        drawingContext.DrawLine(pen, new Point(divider, bounds.Top), new Point(divider, bounds.Bottom));
    }
}
