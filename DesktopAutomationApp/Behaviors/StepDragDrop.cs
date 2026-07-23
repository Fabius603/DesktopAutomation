using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace DesktopAutomationApp.Behaviors;

/// <summary>
/// Gemeinsames, layoutstabiles Drag-and-Drop fuer Step-Listen.
/// Die Zielposition wird als Adorner gezeichnet; die Karten selbst werden erst beim Drop bewegt.
/// </summary>
public static class StepDragDrop
{
    public const string DataFormat = "DesktopAutomation.StepDragDrop";

    public sealed record MoveRequest(IList Source, int SourceIndex, IList Target, int TargetIndex);
    public sealed record DragPayload(IList Source, int SourceIndex);

    public static readonly DependencyProperty MoveCommandProperty =
        DependencyProperty.RegisterAttached(
            "MoveCommand",
            typeof(ICommand),
            typeof(StepDragDrop),
            new PropertyMetadata(null, OnMoveCommandChanged));

    public static void SetMoveCommand(DependencyObject element, ICommand value)
        => element.SetValue(MoveCommandProperty, value);

    public static ICommand? GetMoveCommand(DependencyObject element)
        => element.GetValue(MoveCommandProperty) as ICommand;

    private static Point _dragStart;
    private static ListBox? _sourceList;
    private static int _sourceIndex = -1;
    private static bool _isDragging;
    private static double _sourceOpacity = 1;
    private static ListBoxItem? _sourceContainer;
    private static ListBox? _indicatorOwner;
    private static InsertionAdorner? _indicator;
    private static int _targetIndex = -1;

    private static void OnMoveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox list)
            return;

        list.PreviewMouseLeftButtonDown -= OnMouseDown;
        list.PreviewMouseMove -= OnMouseMove;
        list.DragOver -= OnDragOver;
        list.DragLeave -= OnDragLeave;
        list.Drop -= OnDrop;
        list.AllowDrop = false;

        if (e.NewValue is not ICommand)
            return;

        list.AllowDrop = true;
        list.PreviewMouseLeftButtonDown += OnMouseDown;
        list.PreviewMouseMove += OnMouseMove;
        list.DragOver += OnDragOver;
        list.DragLeave += OnDragLeave;
        list.Drop += OnDrop;
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ResetPendingDrag();
        if (sender is not ListBox list || IsInteractiveElement(e.OriginalSource as DependencyObject))
            return;

        var container = ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (container == null)
            return;

        _dragStart = e.GetPosition(list);
        _sourceIndex = list.ItemContainerGenerator.IndexFromContainer(container);
        _sourceList = _sourceIndex >= 0 ? list : null;
        _sourceContainer = _sourceList == null ? null : container;
    }

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging
            || sender is not ListBox list
            || !ReferenceEquals(list, _sourceList)
            || _sourceIndex < 0
            || e.LeftButton != MouseButtonState.Pressed
            || (e.GetPosition(list) - _dragStart).Length <= 6
            || list.ItemsSource is not IList source)
            return;

        _isDragging = true;
        if (_sourceContainer != null)
        {
            _sourceOpacity = _sourceContainer.Opacity;
            _sourceContainer.Opacity = 0.45;
        }

        try
        {
            var data = new DataObject(DataFormat, new DragPayload(source, _sourceIndex));
            DragDrop.DoDragDrop(list, data, DragDropEffects.Move);
        }
        finally
        {
            CleanupDrag();
        }
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if (sender is not ListBox list || !TryGetPayload(e.Data, out _))
            return;

        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        ScrollAtEdge(list, e);
        var placement = GetInsertionPlacement(list, e.GetPosition(list));
        _targetIndex = placement.Index;
        ShowIndicator(list, placement.Y, list.Items.Count == 0);
    }

    private static void OnDragLeave(object sender, DragEventArgs e)
    {
        if (sender is not ListBox list || !ReferenceEquals(list, _indicatorOwner))
            return;

        var point = e.GetPosition(list);
        if (point.X >= 0 && point.X <= list.ActualWidth
            && point.Y >= 0 && point.Y <= list.ActualHeight)
            return;

        RemoveIndicator();
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not ListBox list
            || list.ItemsSource is not IList target
            || !TryGetPayload(e.Data, out var payload)
            || GetMoveCommand(list) is not { } command)
            return;

        var placement = GetInsertionPlacement(list, e.GetPosition(list));
        var request = new MoveRequest(payload.Source, payload.SourceIndex, target, placement.Index);
        if (command.CanExecute(request))
            command.Execute(request);

        RemoveIndicator();
        e.Handled = true;
    }

    public static bool TryGetPayload(IDataObject data, out DragPayload payload)
    {
        payload = null!;
        if (!data.GetDataPresent(DataFormat) || data.GetData(DataFormat) is not DragPayload value)
            return false;
        payload = value;
        return true;
    }

    /// <summary>
    /// Zeigt die Ablageposition an, wenn der Mauszeiger ueber dem Balken eines Bereichs liegt.
    /// Ein leerer Bereich zeigt seine gesamte Ablageflaeche, ein belegter die Position am Ende.
    /// </summary>
    public static void ShowSectionTarget(ListBox list)
    {
        if (!_isDragging)
            return;

        _targetIndex = list.Items.Count;
        ShowIndicator(list, GetEndInsertionY(list), list.Items.Count == 0);
    }

    /// <summary>
    /// Shows the insertion target at index zero when hovering a section header.
    /// </summary>
    public static void ShowSectionStartTarget(ListBox list)
    {
        if (!_isDragging)
            return;

        _targetIndex = 0;
        var insertionY = list.Items.Count == 0
            ? Math.Max(1, list.ActualHeight / 2)
            : GetInsertionPlacement(list, new Point(0, 0)).Y;
        ShowIndicator(list, insertionY, list.Items.Count == 0);
    }

    public static void ClearTargetPreview(ListBox? list = null)
    {
        if (list == null || ReferenceEquals(list, _indicatorOwner))
            RemoveIndicator();
    }

    private static (int Index, double Y) GetInsertionPlacement(ListBox list, Point pointer)
    {
        if (list.Items.Count == 0)
            return (0, Math.Max(1, list.ActualHeight / 2));

        (int Index, double Bottom, Thickness Margin)? previous = null;
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (list.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem item)
                continue;

            var top = item.TranslatePoint(new Point(0, 0), list).Y;
            var bottom = top + item.ActualHeight;
            if (pointer.Y < top + item.ActualHeight / 2)
            {
                var lineY = previous is { } before
                    ? (before.Bottom + top) / 2
                    : Math.Max(2, top - item.Margin.Top / 2);
                return (i, lineY);
            }
            previous = (i, bottom, item.Margin);
        }

        return previous is { } last
            ? (last.Index + 1, last.Bottom + last.Margin.Bottom / 2)
            : (list.Items.Count, Math.Max(1, list.ActualHeight - 2));
    }

    private static double GetEndInsertionY(ListBox list)
    {
        if (list.Items.Count == 0)
            return Math.Max(1, list.ActualHeight / 2);

        for (var i = list.Items.Count - 1; i >= 0; i--)
        {
            if (list.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem item)
                continue;
            var bottom = item.TranslatePoint(new Point(0, 0), list).Y + item.ActualHeight;
            return bottom + item.Margin.Bottom / 2;
        }

        return Math.Max(2, list.ActualHeight - 2);
    }

    private static void ShowIndicator(ListBox list, double y, bool empty)
    {
        if (!ReferenceEquals(_indicatorOwner, list))
        {
            RemoveIndicator();
            var layer = AdornerLayer.GetAdornerLayer(list);
            if (layer == null)
                return;
            _indicatorOwner = list;
            _indicator = new InsertionAdorner(list);
            layer.Add(_indicator);
        }

        _indicator?.Update(y, empty);
    }

    private static void RemoveIndicator()
    {
        if (_indicator != null && _indicatorOwner != null)
            AdornerLayer.GetAdornerLayer(_indicatorOwner)?.Remove(_indicator);
        _indicator = null;
        _indicatorOwner = null;
        _targetIndex = -1;
    }

    private static void ScrollAtEdge(ListBox list, DragEventArgs e)
    {
        var scroller = FindScrollViewerForDrag(list);
        if (scroller == null)
            return;

        var position = e.GetPosition(scroller);
        const double edge = 44;
        var oldOffset = scroller.VerticalOffset;
        if (position.Y < edge)
            scroller.ScrollToVerticalOffset(oldOffset - 14);
        else if (position.Y > scroller.ViewportHeight - edge)
            scroller.ScrollToVerticalOffset(oldOffset + 14);

        if (Math.Abs(oldOffset - scroller.VerticalOffset) > 0.1)
            list.Dispatcher.BeginInvoke(() => _indicator?.InvalidateVisual());
    }

    private static ScrollViewer? FindScrollViewerForDrag(ListBox list)
        => FindAncestorScrollViewer(list) ?? FindDescendantScrollViewer(list);

    private static ScrollViewer? FindAncestorScrollViewer(DependencyObject element)
    {
        for (var current = VisualTreeHelper.GetParent(element); current != null; current = VisualTreeHelper.GetParent(current))
            if (current is ScrollViewer viewer)
                return viewer;
        return null;
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ScrollViewer viewer)
                return viewer;
            if (FindDescendantScrollViewer(child) is { } nested)
                return nested;
        }
        return null;
    }

    private static bool IsInteractiveElement(DependencyObject? element)
    {
        for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ButtonBase or TextBoxBase or ComboBox)
                return true;
            if (current is ListBoxItem)
                return false;
        }
        return false;
    }

    private static void ResetPendingDrag()
    {
        _sourceList = null;
        _sourceContainer = null;
        _sourceIndex = -1;
    }

    private static void CleanupDrag()
    {
        if (_sourceContainer != null)
            _sourceContainer.Opacity = _sourceOpacity;
        RemoveIndicator();
        _isDragging = false;
        ResetPendingDrag();
    }

    private sealed class InsertionAdorner : Adorner
    {
        private double _y;
        private bool _empty;
        private readonly Brush _accent;
        private readonly Brush _fill;

        public InsertionAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
            _accent = FindAccentBrush(adornedElement);
            _fill = _accent.CloneCurrentValue();
            _fill.Opacity = 0.12;
            Effect = new DropShadowEffect
            {
                Color = _accent is SolidColorBrush solid ? solid.Color : Colors.DodgerBlue,
                BlurRadius = 7,
                ShadowDepth = 0,
                Opacity = 0.45
            };
        }

        public void Update(double y, bool empty)
        {
            _y = y;
            _empty = empty;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var width = AdornedElement.RenderSize.Width;
            if (width <= 0)
                return;

            if (_empty)
            {
                var height = Math.Max(36, AdornedElement.RenderSize.Height - 8);
                var rect = new Rect(5, 4, Math.Max(1, width - 10), height);
                var pen = new Pen(_accent, 2) { DashStyle = new DashStyle(new[] { 4d, 3d }, 0) };
                drawingContext.DrawRoundedRectangle(_fill, pen, rect, 8, 8);
                return;
            }

            var y = Math.Clamp(_y, 2, Math.Max(2, AdornedElement.RenderSize.Height - 2));
            var penLine = new Pen(_accent, 3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            drawingContext.DrawLine(penLine, new Point(8, y), new Point(Math.Max(8, width - 8), y));
            drawingContext.DrawEllipse(_accent, null, new Point(8, y), 4, 4);
            drawingContext.DrawEllipse(_accent, null, new Point(Math.Max(8, width - 8), y), 4, 4);
        }

        private static Brush FindAccentBrush(DependencyObject element)
        {
            if (element is FrameworkElement frameworkElement)
                return frameworkElement.TryFindResource("MahApps.Brushes.Accent") as Brush
                    ?? frameworkElement.TryFindResource("App.Brush.Accent") as Brush
                    ?? Brushes.DodgerBlue;
            return Brushes.DodgerBlue;
        }
    }
}
