using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Behaviors;

/// <summary>
/// A visual-only drag session. A source ghost stays in its logical slot; target spacing never
/// changes collection order. Hit testing subtracts our displacement to keep insertion slots stable.
/// </summary>
internal sealed class StepDragPreviewSession : IDisposable
{
    private readonly FrameworkElement _root;
    private readonly Dictionary<FrameworkElement, double> _sourceOpacities = [];
    private readonly Dictionary<FrameworkElement, Transform> _gapTransforms = [];
    private readonly AdornerLayer? _layer;
    private readonly DragCardAdorner _adorner;
    private readonly DispatcherTimer _timer;
    private ListBox? _target;
    private ListBox? _tracking;
    private int _index = -1;
    private StepDragDrop.InsertionPlacement? _placement;
    private double _originalMinHeight;
    private double _originalHeight;
    private bool _disposed;
    private Point? _targetPointer;

    internal ListBox? Target => _target;
    internal int TargetIndex => _index;
    internal bool IsSnapped => _target is not null;
    internal Rect? PreviewBounds => _adorner.Snap;
    internal int MovingStepCount { get; }

    internal StepDragPreviewSession(ListBox source, StepDragDrop.DragPayload payload, bool startTimer = true)
    {
        _root = FindRoot(source);
        var images = payload.SourceIndices.Select(index => source.ItemContainerGenerator.ContainerFromIndex(index))
            .OfType<ListBoxItem>().Where(item => item.Visibility == Visibility.Visible && item.ActualHeight > 0).ToArray();
        var card = images.Select(item => item.Template.FindName("Card", item)).OfType<FrameworkElement>()
            .FirstOrDefault(element => element.ActualHeight >= 40);
        var image = card is null ? null : Capture(card);
        var count = payload.SourceIndices.Count(index => index >= 0 && index < payload.Source.Count
            && (payload.Source[index] is not JobStep step || StepLocalization.HasListPosition(step)));
        MovingStepCount = count;
        _adorner = new DragCardAdorner(_root, image, card?.ActualWidth ?? 320, source,
            count == 1 ? Loc.Get("Ui.Job.Steps.Drag.Single") : Loc.Format("Ui.Job.Steps.Drag.Count", count));
        _layer = AdornerLayer.GetAdornerLayer(_root);
        _layer?.Add(_adorner);
        UpdatePointer(source.TranslatePoint(new Point(80, 32), _root));
        RefreshPointer();
        foreach (var item in images)
        {
            _sourceOpacities[item] = item.Opacity;
            item.Opacity = 0.22;
        }
        _timer = new DispatcherTimer(DispatcherPriority.Render, source.Dispatcher) { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
        if (startTimer) _timer.Start();
    }

    internal void UpdatePointer(Point rootPoint)
    {
        if (_disposed) return;
        PointerPosition = rootPoint;
        _adorner.Pointer = rootPoint;
        RefreshSnapBounds();
    }

    private void RefreshSnapBounds()
    {
        if (_target is not { } target || _placement is not { } placement) return;
        _adorner.Snap = new Rect(target.TranslatePoint(new Point(placement.X, placement.Y + 4), _root), new Size(placement.Width, 64));
        _adorner.InvalidateVisual();
    }
    internal Point PointerRelativeTo(FrameworkElement element) => _root.TranslatePoint(PointerPosition, element);
    internal Point PointerPosition { get; private set; }

    internal void UpdatePointerFrom(FrameworkElement source, Point position)
    {
        UpdatePointer(source.TranslatePoint(position, _root));
    }

    internal bool UpdatePointerFromScreen(Point position)
    {
        if (PresentationSource.FromVisual(_root) is null) return false;
        UpdatePointer(_root.PointFromScreen(position));
        return true;
    }

    internal void RefreshPointer()
    {
        // WPF Mouse.GetPosition can remain at (0,0) inside the native OLE drag loop.
        // Read screen coordinates and let WPF convert them using the connected surface's DPI.
        if (PresentationSource.FromVisual(_root) is not null && GetCursorPos(out var point))
            UpdatePointerFromScreen(new Point(point.X, point.Y));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenPoint { public int X; public int Y; }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out ScreenPoint point);

    internal void ReleasePointerRetention() => _targetPointer = null;

    internal bool TryRetainStationaryTarget(out ListBox list)
    {
        list = _target!;
        if (_disposed || _target is null || _targetPointer is not { } anchor) return false;
        var inGap = Math.Abs(PointerPosition.X - anchor.X) <= 6
            && _placement is { } placement && PointerRelativeTo(_target) is var pointer
            && pointer.X >= placement.X && pointer.X <= placement.X + placement.Width
            && pointer.Y >= placement.Y && pointer.Y <= placement.Y + StepDragPreviewPolicy.GapHeight;
        if (!inGap && (PointerPosition - anchor).Length > 6) return false;
        // The preview can move a list boundary. The surrounding viewport remains the drag region.
        FrameworkElement region = Ancestors(_target).OfType<ScrollViewer>().FirstOrDefault() ?? (FrameworkElement)_target;
        var point = PointerRelativeTo(region);
        return point.X >= 0 && point.Y >= 0 && point.X <= region.ActualWidth && point.Y <= region.ActualHeight;
    }

    internal StepDragDrop.InsertionPlacement RetainTarget(ListBox list, Point pointer, StepDragDrop.InsertionPlacement candidate)
    {
        if (!ReferenceEquals(_target, list) || _placement is not { } previous) return candidate;
        if (TryRetainStationaryTarget(out _)) return previous with { Distance = 0, Width = candidate.Width };
        var distance = Math.Max(previous.Y - pointer.Y, pointer.Y - previous.Y - StepDragPreviewPolicy.GapHeight);
        return _targetPointer is not null && Math.Abs(previous.X - candidate.X) < 20
            && StepDragPreviewPolicy.CanSnap(Math.Max(0, distance), retainingTarget: true)
                ? previous with { Distance = Math.Max(0, distance), Width = candidate.Width } : candidate;
    }

    internal Point WithoutPreviewGap(ListBox list, Point pointer)
    {
        if (_targetPointer is null || !ReferenceEquals(_target, list) || _placement is not { } placement || pointer.Y <= placement.Y) return pointer;
        // Rows are resolved in their undisplaced coordinates. Map the pointer to that same
        // space: the open gap has no logical height, and rows below it moved by GapHeight.
        return new Point(pointer.X, Math.Max(placement.Y, pointer.Y - StepDragPreviewPolicy.GapHeight));
    }

    internal bool UpdateTarget(ListBox list, StepDragDrop.InsertionPlacement placement, bool valid, bool force = false)
    {
        if (_disposed) return false;
        _tracking = list;
        var retaining = ReferenceEquals(_target, list) && _index == placement.Index;
        if (!valid || !force && !StepDragPreviewPolicy.CanSnap(placement.Distance, ReferenceEquals(_target, list)))
        {
            ClearTarget();
            _tracking = list;
            return false;
        }
        if (!retaining)
        {
            if (ReferenceEquals(_target, list))
                ClearGapTransforms();
            else
            {
                ClearGap();
                _target = list;
                _originalMinHeight = list.MinHeight;
                _originalHeight = list.ActualHeight;
            }
            _index = placement.Index;
            _targetPointer = PointerPosition;
        }
        _targetPointer ??= PointerPosition;
        ApplyGap();
        list.UpdateLayout();
        // Reserving the gap can reveal the outer scrollbar and reduce the list viewport.
        // Resolve the measured width again so the preview retains the same right inset as cards.
        var measured = StepDragDrop.GetPlacementResolver(list)?.Invoke(list, new Point(placement.X, placement.Y));
        var width = measured is { Width: var currentWidth } && double.IsFinite(currentWidth)
            ? currentWidth : double.IsFinite(placement.Width) ? placement.Width : Math.Max(160, list.ActualWidth - placement.X - 36);
        _placement = placement with { Width = width };
        RefreshSnapBounds();
        return true;
    }

    internal void ClearTarget(ListBox? list = null)
    {
        if (list is not null && !ReferenceEquals(list, _target) && !ReferenceEquals(list, _tracking)) return;
        ClearGap();
        _tracking = null;
        _adorner.Snap = null;
        _adorner.InvalidateVisual();
    }

    private void ApplyGap()
    {
        if (_target is null) return;
        for (var index = _index; index < _target.Items.Count; index++)
        {
            if (_target.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem item
                || item.Visibility != Visibility.Visible || item.ActualHeight <= 0 || _gapTransforms.ContainsKey(item)) continue;
            _gapTransforms[item] = item.RenderTransform;
            var group = new TransformGroup();
            group.Children.Add(item.RenderTransform);
            group.Children.Add(new TranslateTransform(0, StepDragPreviewPolicy.GapHeight));
            item.RenderTransform = group;
            StepDragDrop.SetPreviewDisplacement(item, StepDragPreviewPolicy.GapHeight);
        }
        _target.SetCurrentValue(FrameworkElement.MinHeightProperty, Math.Max(_originalMinHeight, _originalHeight + StepDragPreviewPolicy.GapHeight));
        InvalidatePanel(_target);
    }

    private void ClearGapTransforms()
    {
        foreach (var (item, transform) in _gapTransforms)
        {
            item.RenderTransform = transform;
            StepDragDrop.SetPreviewDisplacement(item, 0);
        }
        _gapTransforms.Clear();
    }

    private void ClearGap()
    {
        ClearGapTransforms();
        var target = _target;
        _target = null;
        _index = -1;
        _placement = null;
        _targetPointer = null;
        _adorner.Snap = null;
        if (target is not null)
        {
            target.SetCurrentValue(FrameworkElement.MinHeightProperty, _originalMinHeight);
            InvalidatePanel(target);
            target.UpdateLayout();
        }
    }

    private void OnTick(object? sender, EventArgs args)
    {
        if (_disposed) return;
        RefreshPointer();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        ClearGap();
        foreach (var (item, opacity) in _sourceOpacities) item.Opacity = opacity;
        _sourceOpacities.Clear();
        _layer?.Remove(_adorner);
    }

    private static ImageSource Capture(FrameworkElement element)
    {
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap(Math.Max(1, (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX)),
            Math.Max(1, (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY)), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
            context.DrawRectangle(new VisualBrush(element), null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static FrameworkElement FindRoot(ListBox list) => (FrameworkElement?)Ancestors(list).OfType<UserControl>().FirstOrDefault() ?? list;
    private static IEnumerable<DependencyObject> Ancestors(DependencyObject element)
    {
        for (var parent = VisualTreeHelper.GetParent(element); parent is not null; parent = VisualTreeHelper.GetParent(parent)) yield return parent;
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject element)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void InvalidatePanel(ListBox list)
    {
        foreach (var panel in Descendants(list).OfType<Panel>()) panel.InvalidateVisual();
    }

    private sealed class DragCardAdorner : Adorner
    {
        private readonly ImageSource? _image;
        private readonly double _imageWidth;
        private readonly Brush _accent;
        private readonly Brush _background;
        private readonly Brush _text;
        private readonly string _count;
        private Point _pointer;
        internal Rect? Snap { get; set; }
        internal Point Pointer { set { _pointer = value; InvalidateVisual(); } }
        internal DragCardAdorner(UIElement root, ImageSource? image, double imageWidth, FrameworkElement source, string count) : base(root)
        {
            IsHitTestVisible = false;
            _image = image;
            _imageWidth = imageWidth;
            _accent = source.TryFindResource("App.Brush.Accent") as Brush ?? Brushes.DeepSkyBlue;
            _background = source.TryFindResource("App.Brush.StepCard") as Brush ?? Brushes.DimGray;
            _text = source.TryFindResource("App.Brush.TextPrimary") as Brush ?? Brushes.White;
            _count = count;
        }
        protected override void OnRender(DrawingContext context)
        {
            var snapped = Snap is not null;
            var rect = Snap ?? new Rect(_pointer + new Vector(18, 18), new Size(Math.Min(360, _imageWidth), 64));
            if (rect.Width <= 0) return;
            var shadow = rect; shadow.Offset(2, 4);
            context.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(75, 0, 0, 0)), null, shadow, 8, 8);
            context.PushOpacity(snapped ? 0.92 : 0.78);
            context.DrawRoundedRectangle(_background, null, rect, 8, 8);
            context.PushClip(new RectangleGeometry(rect, 8, 8));
            if (_image is not null) context.DrawImage(_image, new Rect(rect.TopLeft, new Size(_imageWidth, 64)));
            context.Pop();
            context.Pop();
            var border = _accent.CloneCurrentValue(); border.Opacity = snapped ? 0.6 : 0.3;
            context.DrawRoundedRectangle(null, new Pen(border, snapped ? 2 : 1), rect, 8, 8);
            var text = new FormattedText(_count, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 11, _text, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var badge = new Rect(rect.Right - text.Width - 20, rect.Top - 10, text.Width + 12, 22);
            context.DrawRoundedRectangle(_background, new Pen(border, 1), badge, 6, 6);
            context.DrawText(text, badge.TopLeft + new Vector(6, 3));
        }
    }
}
