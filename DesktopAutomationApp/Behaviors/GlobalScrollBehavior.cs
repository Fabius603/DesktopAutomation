using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopAutomationApp.Behaviors;

/// <summary>
/// Provides consistent, touchpad-friendly scrolling for every window in the application.
/// </summary>
internal static class GlobalScrollBehavior
{
    private const double WheelScrollFactor = 0.25;
    private const double BoundaryTolerance = 0.1;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        EventManager.RegisterClassHandler(
            typeof(UIElement),
            UIElement.PreviewMouseWheelEvent,
            new MouseWheelEventHandler(OnPreviewMouseWheel),
            handledEventsToo: true);

        EventManager.RegisterClassHandler(
            typeof(ItemsControl),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnItemsControlLoaded),
            handledEventsToo: true);
    }

    private static void OnItemsControlLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsControl itemsControl)
            itemsControl.SetCurrentValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Pixel);
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || e.Delta == 0 || e.OriginalSource is not DependencyObject source)
            return;

        var horizontal = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        var scroller = FindScrollableAncestor(source, horizontal, e.Delta);
        if (scroller == null)
            return;

        var distance = e.Delta * WheelScrollFactor;
        if (horizontal)
            scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset - distance);
        else
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset - distance);

        e.Handled = true;
    }

    private static ScrollViewer? FindScrollableAncestor(
        DependencyObject source,
        bool horizontal,
        int delta)
    {
        for (var current = source; current != null; current = GetParent(current))
        {
            if (current is not ScrollViewer scroller)
                continue;

            var offset = horizontal ? scroller.HorizontalOffset : scroller.VerticalOffset;
            var scrollableSize = horizontal ? scroller.ScrollableWidth : scroller.ScrollableHeight;
            var canScrollInDirection = delta > 0
                ? offset > BoundaryTolerance
                : offset < scrollableSize - BoundaryTolerance;

            if (scrollableSize > BoundaryTolerance && canScrollInDirection)
                return scroller;
        }

        return null;
    }

    private static DependencyObject? GetParent(DependencyObject child)
    {
        if (child is Visual or System.Windows.Media.Media3D.Visual3D)
            return VisualTreeHelper.GetParent(child);

        return LogicalTreeHelper.GetParent(child);
    }
}
