using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows.Threading;

namespace DesktopAutomationApp.Behaviors
{
    public static class SimpleReorderDragDrop
    {
        public static readonly DependencyProperty MoveCommandProperty =
            DependencyProperty.RegisterAttached(
                "MoveCommand",
                typeof(ICommand),
                typeof(SimpleReorderDragDrop),
                new PropertyMetadata(null, OnAttach));

        public static void SetMoveCommand(DependencyObject d, ICommand value)
            => d.SetValue(MoveCommandProperty, value);

        public static ICommand GetMoveCommand(DependencyObject d)
            => (ICommand)d.GetValue(MoveCommandProperty);

        private static Point _dragStart;
        private static int _fromIndex = -1;
        private static FrameworkElement? _draggedContainer;
        private static DragAdorner? _dragAdorner;
        private static GhostInsertionAdorner? _ghostInsertionAdorner;
        private static int _currentInsertIndex = -1;
        private static bool _isDragging;

        private const double MidpointHysteresisPx = 4.0;
        private const double GhostBottomMarginPx = 6.0;
        private const double EdgeAutoScrollZonePx = 36.0;
        private const double MaxEdgeAutoScrollStepPx = 18.0;
        private const double MouseWheelScrollStepPx = 56.0;

        private static double _draggedOriginalOpacity = 1.0;

        private static readonly Dictionary<FrameworkElement, Transform?> _originalContainerTransforms = new();
        private static readonly Dictionary<FrameworkElement, double> _appliedShiftOffsets = new();

        private static int _lastShiftInsertIndex = -1;

        private static readonly DispatcherTimer _edgeAutoScrollTimer =
            new() { Interval = TimeSpan.FromMilliseconds(16) };

        private static ItemsControl? _activeDragItemsControl;
        private static ScrollViewer? _activeDragScrollViewer;
        private static double _lastKnownMouseY;
        private static bool _hasLastKnownMouseY;
        private static Point _lastKnownMousePosition;

        private sealed class LayoutSnapshotItem
        {
            public int Index { get; init; }
            public double Top { get; init; }
            public double Bottom { get; init; }
        }

        private static readonly List<LayoutSnapshotItem> _dragLayoutSnapshot = new();

        static SimpleReorderDragDrop()
        {
            _edgeAutoScrollTimer.Tick += (_, _) => OnAutoScrollTimerTick();
        }

        private static void OnAttach(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ItemsControl ic)
                return;

            ic.PreviewMouseLeftButtonDown -= OnMouseDown;
            ic.PreviewMouseMove -= OnMouseMove;
            ic.Drop -= OnDrop;
            ic.DragOver -= OnDragOver;
            ic.PreviewMouseWheel -= OnPreviewMouseWheel;

            ic.AllowDrop = false;

            if (e.NewValue is ICommand)
            {
                ic.AllowDrop = true;
                ic.PreviewMouseLeftButtonDown += OnMouseDown;
                ic.PreviewMouseMove += OnMouseMove;
                ic.Drop += OnDrop;
                ic.DragOver += OnDragOver;
                ic.PreviewMouseWheel += OnPreviewMouseWheel;
            }
        }

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ItemsControl ic)
                return;

            _dragStart = e.GetPosition(ic);
            _fromIndex = ContainerIndexAt(ic, e.OriginalSource as DependencyObject);

            if (_fromIndex >= 0)
            {
                _draggedContainer =
                    ic.ItemContainerGenerator.ContainerFromIndex(_fromIndex) as FrameworkElement;

                _draggedOriginalOpacity = _draggedContainer?.Opacity ?? 1.0;
            }
            else
            {
                _draggedContainer = null;
                _draggedOriginalOpacity = 1.0;
            }
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ItemsControl ic || e.LeftButton != MouseButtonState.Pressed)
                return;

            if (_fromIndex < 0 || _draggedContainer == null || _isDragging)
                return;

            var pos = e.GetPosition(ic);

            if ((pos - _dragStart).Length <= 6)
                return;

            _isDragging = true;
            _activeDragItemsControl = ic;
            _activeDragScrollViewer = FindScrollViewer(ic);
            _lastKnownMouseY = pos.Y;
            _hasLastKnownMouseY = true;

            _edgeAutoScrollTimer.Start();

            CaptureDragLayoutSnapshot(ic);

            var adornerLayer = AdornerLayer.GetAdornerLayer(ic);
            if (adornerLayer != null)
            {
                _dragAdorner = new DragAdorner(ic, _draggedContainer);
                adornerLayer.Add(_dragAdorner);

                _ghostInsertionAdorner = new GhostInsertionAdorner(ic, _draggedContainer);
                adornerLayer.Add(_ghostInsertionAdorner);
            }

            _currentInsertIndex = -1;
            UpdateDraggedContainerVisibility();

            try
            {
                DragDrop.DoDragDrop(ic, new DataObject("reorder", _fromIndex), DragDropEffects.Move);
            }
            finally
            {
                CleanupAdorners(ic);
            }
        }

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            if (sender is not ItemsControl ic) return;
            if (!e.Data.GetDataPresent("reorder")) return;

            e.Effects = DragDropEffects.Move;
            e.Handled = true;

            var pos = e.GetPosition(ic);

            _lastKnownMouseY = pos.Y;
            _lastKnownMousePosition = pos;
            _hasLastKnownMouseY = true;

            bool didAutoScroll = TryAutoScrollAtEdge(ic, pos.Y);
            if (didAutoScroll)
                CaptureDragLayoutSnapshot(ic);

            bool showGhostAtMouse = didAutoScroll || ShouldShowGhostAtMouseWhileScrolling(ic, pos.Y);

            _dragAdorner?.UpdatePosition(pos);

            UpdateInsertionPosition(ic, pos, showGhostAtMouse);
        }

        private static bool ShouldShowGhostAtMouseWhileScrolling(ItemsControl ic, double mouseY)
        {
            var scrollViewer = _activeDragScrollViewer ?? FindScrollViewer(ic);

            if (scrollViewer == null || scrollViewer.ScrollableHeight <= 0)
                return false;

            bool nearTop = mouseY < EdgeAutoScrollZonePx;
            bool nearBottom = mouseY > ic.ActualHeight - EdgeAutoScrollZonePx;

            bool canScrollUp = scrollViewer.VerticalOffset > 0;
            bool canScrollDown = scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;

            return mouseY < 0
                || mouseY > ic.ActualHeight
                || nearTop && canScrollUp
                || nearBottom && canScrollDown;
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_isDragging || sender is not ItemsControl ic)
                return;

            var scrollViewer = _activeDragScrollViewer ?? FindScrollViewer(ic);
            if (scrollViewer == null)
                return;

            _activeDragScrollViewer ??= scrollViewer;

            var deltaSteps = e.Delta / 120.0;
            var newOffset = scrollViewer.VerticalOffset - deltaSteps * MouseWheelScrollStepPx;
            newOffset = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, newOffset));

            if (Math.Abs(newOffset - scrollViewer.VerticalOffset) > 0.01)
            {
                ResetShiftedContainers();

                scrollViewer.ScrollToVerticalOffset(newOffset);

                // Wichtig: neue Scroll-Position sofort in die Visual-Tree-Geometrie übernehmen.
                ic.UpdateLayout();

                CaptureDragLayoutSnapshot(ic);
            }

            e.Handled = true;
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is not ItemsControl ic)
                return;

            if (!e.Data.GetDataPresent("reorder"))
                return;

            var from = (int)e.Data.GetData("reorder")!;

            var to = _currentInsertIndex >= 0
                ? _currentInsertIndex
                : ContainerIndexAt(ic, e.OriginalSource as DependencyObject);

            if (to > from)
                to--;

            if (to < 0 || from == to)
                return;

            var cmd = GetMoveCommand(ic);

            if (cmd?.CanExecute((from, to)) == true)
                cmd.Execute((from, to));
        }

        private static void UpdateInsertionPosition(ItemsControl ic, Point mousePos, bool showGhostAtMouse)
        {
            if (mousePos.Y < 0 || mousePos.Y > ic.ActualHeight)
            {
                if (TrySnapGhostToVisibleBoundary(ic, mousePos.Y, showGhostAtMouse, mousePos))
                    return;

                RefreshGhostForCurrentInsertIndex(showGhostAtMouse, mousePos);
                return;
            }

            int insertIndex = -1;
            double insertionY = 0;

            if (_dragLayoutSnapshot.Count == 0)
                CaptureDragLayoutSnapshot(ic);

            if (_dragLayoutSnapshot.Count == 0)
                return;

            for (int i = 0; i < _dragLayoutSnapshot.Count; i++)
            {
                var snap = _dragLayoutSnapshot[i];
                var top = snap.Top;
                var bottom = snap.Bottom;
                var midY = (top + bottom) / 2;

                if (mousePos.Y < midY)
                {
                    insertIndex = snap.Index;
                    insertionY = top;
                    break;
                }

                if (i == _dragLayoutSnapshot.Count - 1)
                {
                    insertIndex = snap.Index + 1;
                    insertionY = bottom;
                }
            }

            if (insertIndex < 0)
                return;

            insertIndex = ApplyMidpointHysteresis(mousePos.Y, insertIndex);
            insertionY = GetInsertionYForInsertIndex(insertIndex, insertionY);

            if (insertIndex == _fromIndex || insertIndex == _fromIndex + 1)
            {
                _currentInsertIndex = -1;
                _ghostInsertionAdorner?.Hide();
                ResetShiftedContainers();
                UpdateDraggedContainerVisibility();
                return;
            }

            ApplyInsertPreview(ic, insertIndex, insertionY, showGhostAtMouse, mousePos);
        }

        private static void ApplyInsertPreview(
            ItemsControl ic,
            int insertIndex,
            double insertionY,
            bool showGhostAtMouse = false,
            Point? mousePos = null)
        {
            _currentInsertIndex = insertIndex;
            UpdateDraggedContainerVisibility();

            if (showGhostAtMouse && mousePos.HasValue)
            {
                ResetShiftedContainers();

                _ghostInsertionAdorner?.UpdateMousePosition(mousePos.Value);
                _ghostInsertionAdorner?.Show();
                return;
            }

            var ghostY = insertionY;

            if (_draggedContainer != null && _fromIndex >= 0 && insertIndex > _fromIndex)
                ghostY -= _draggedContainer.ActualHeight;

            _ghostInsertionAdorner?.UpdatePosition(ghostY);
            _ghostInsertionAdorner?.Show();

            UpdateShiftedContainers(ic, insertIndex);
        }

        private static bool IsMouseInAutoScrollArea(ItemsControl ic, double mouseY)
        {
            if (_activeDragScrollViewer == null && FindScrollViewer(ic) == null)
                return false;

            return mouseY < EdgeAutoScrollZonePx
                || mouseY > ic.ActualHeight - EdgeAutoScrollZonePx;
        }

        private static bool TrySnapGhostToVisibleBoundary(
            ItemsControl ic,
            double mouseY,
            bool showGhostAtMouse,
            Point mousePos)
        {
            if (_dragLayoutSnapshot.Count == 0)
                return false;

            const double eps = 0.5;
            bool wantsTop = mouseY < 0;

            LayoutSnapshotItem? target = null;

            if (wantsTop)
            {
                for (int i = 0; i < _dragLayoutSnapshot.Count; i++)
                {
                    var s = _dragLayoutSnapshot[i];

                    if (s.Top >= -eps && s.Bottom <= ic.ActualHeight + eps)
                    {
                        target = s;
                        break;
                    }
                }

                target ??= FirstPartiallyVisibleFromTop();
            }
            else
            {
                for (int i = _dragLayoutSnapshot.Count - 1; i >= 0; i--)
                {
                    var s = _dragLayoutSnapshot[i];

                    if (s.Top >= -eps && s.Bottom <= ic.ActualHeight + eps)
                    {
                        target = s;
                        break;
                    }
                }

                target ??= FirstPartiallyVisibleFromBottom();
            }

            if (target == null)
                return false;

            var insertIndex = wantsTop ? target.Index : target.Index + 1;
            var insertionY = GetInsertionYForInsertIndex(
                insertIndex,
                wantsTop ? target.Top : target.Bottom);

            if (insertIndex == _fromIndex || insertIndex == _fromIndex + 1)
            {
                _currentInsertIndex = -1;
                _ghostInsertionAdorner?.Hide();
                ResetShiftedContainers();
                UpdateDraggedContainerVisibility();
                return true;
            }

            ApplyInsertPreview(ic, insertIndex, insertionY, showGhostAtMouse, mousePos);
            return true;

            LayoutSnapshotItem? FirstPartiallyVisibleFromTop()
            {
                for (int i = 0; i < _dragLayoutSnapshot.Count; i++)
                {
                    var s = _dragLayoutSnapshot[i];

                    if (s.Bottom > 0)
                        return s;
                }

                return null;
            }

            LayoutSnapshotItem? FirstPartiallyVisibleFromBottom()
            {
                for (int i = _dragLayoutSnapshot.Count - 1; i >= 0; i--)
                {
                    var s = _dragLayoutSnapshot[i];

                    if (s.Top < ic.ActualHeight)
                        return s;
                }

                return null;
            }
        }

        private static void RefreshGhostForCurrentInsertIndex(bool showGhostAtMouse = false, Point? mousePos = null)
        {
            if (_currentInsertIndex < 0 || _ghostInsertionAdorner == null)
                return;

            if (showGhostAtMouse && mousePos.HasValue)
            {
                ResetShiftedContainers();
                _ghostInsertionAdorner.UpdateMousePosition(mousePos.Value);
                _ghostInsertionAdorner.Show();
                return;
            }

            var insertionY = GetInsertionYForInsertIndex(_currentInsertIndex, 0);
            var ghostY = insertionY;

            if (_draggedContainer != null && _fromIndex >= 0 && _currentInsertIndex > _fromIndex)
                ghostY -= _draggedContainer.ActualHeight;

            _ghostInsertionAdorner.UpdatePosition(ghostY);
            _ghostInsertionAdorner.Show();
        }

        private static void UpdateDraggedContainerVisibility()
        {
            if (_draggedContainer == null)
                return;

            bool hideOriginal = _currentInsertIndex >= 0;
            _draggedContainer.Opacity = hideOriginal ? 0.0 : _draggedOriginalOpacity;
        }

        private static int ApplyMidpointHysteresis(double mouseY, int candidateInsertIndex)
        {
            if (_currentInsertIndex < 0 || candidateInsertIndex < 0 || _dragLayoutSnapshot.Count == 0)
                return candidateInsertIndex;

            if (candidateInsertIndex == _currentInsertIndex)
                return candidateInsertIndex;

            if (Math.Abs(candidateInsertIndex - _currentInsertIndex) != 1)
                return candidateInsertIndex;

            double boundaryMidY;

            if (candidateInsertIndex > _currentInsertIndex)
            {
                if (!TryGetSnapshotByIndex(_currentInsertIndex, out var stepAtCurrent))
                    return candidateInsertIndex;

                boundaryMidY = (stepAtCurrent.Top + stepAtCurrent.Bottom) / 2.0;

                return mouseY >= boundaryMidY + MidpointHysteresisPx
                    ? candidateInsertIndex
                    : _currentInsertIndex;
            }

            if (!TryGetSnapshotByIndex(candidateInsertIndex, out var stepBeforeCurrent))
                return candidateInsertIndex;

            boundaryMidY = (stepBeforeCurrent.Top + stepBeforeCurrent.Bottom) / 2.0;

            return mouseY <= boundaryMidY - MidpointHysteresisPx
                ? candidateInsertIndex
                : _currentInsertIndex;
        }

        private static bool TryGetSnapshotByIndex(int itemIndex, out LayoutSnapshotItem snapshot)
        {
            for (int i = 0; i < _dragLayoutSnapshot.Count; i++)
            {
                var current = _dragLayoutSnapshot[i];

                if (current.Index == itemIndex)
                {
                    snapshot = current;
                    return true;
                }
            }

            snapshot = null!;
            return false;
        }

        private static double GetInsertionYForInsertIndex(int insertIndex, double fallbackY)
        {
            if (_dragLayoutSnapshot.Count == 0)
                return fallbackY;

            if (insertIndex <= 0)
                return _dragLayoutSnapshot[0].Top;

            var last = _dragLayoutSnapshot[^1];

            if (insertIndex > last.Index)
                return last.Bottom;

            if (TryGetSnapshotByIndex(insertIndex, out var snap))
                return snap.Top;

            return fallbackY;
        }

        private static void UpdateShiftedContainers(ItemsControl ic, int insertIndex)
        {
            if (_fromIndex < 0 || _draggedContainer == null)
            {
                ResetShiftedContainers();
                return;
            }

            if (_lastShiftInsertIndex == insertIndex)
                return;

            var draggedHeight = _draggedContainer.ActualHeight;

            if (draggedHeight <= 0)
            {
                ResetShiftedContainers();
                return;
            }

            bool movingDown = _fromIndex < insertIndex;

            for (int i = 0; i < ic.Items.Count; i++)
            {
                if (i == _fromIndex)
                    continue;

                if (ic.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                    continue;

                double offset = 0;

                if (movingDown)
                {
                    if (i > _fromIndex && i < insertIndex)
                        offset = -draggedHeight;
                }
                else
                {
                    if (i >= insertIndex && i < _fromIndex)
                        offset = draggedHeight;
                }

                if (!_originalContainerTransforms.ContainsKey(container))
                    _originalContainerTransforms[container] = container.RenderTransform;

                _appliedShiftOffsets[container] = offset;

                container.RenderTransform = Math.Abs(offset) > 0.01
                    ? new TranslateTransform(0, offset)
                    : (_originalContainerTransforms.TryGetValue(container, out var original)
                        ? original
                        : Transform.Identity);
            }

            _lastShiftInsertIndex = insertIndex;
        }

        private static void ResetShiftedContainers()
        {
            if (_originalContainerTransforms.Count == 0)
                return;

            foreach (var kvp in _originalContainerTransforms)
            {
                kvp.Key.RenderTransform = kvp.Value ?? Transform.Identity;
            }

            _originalContainerTransforms.Clear();
            _appliedShiftOffsets.Clear();
            _lastShiftInsertIndex = -1;
        }

        private static void CaptureDragLayoutSnapshot(ItemsControl ic)
        {
            _dragLayoutSnapshot.Clear();

            for (int i = 0; i < ic.Items.Count; i++)
            {
                if (ic.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
                    continue;

                var transform = container.TransformToAncestor(ic);
                var top = transform.Transform(new Point(0, 0)).Y;
                var bottom = transform.Transform(new Point(0, container.ActualHeight)).Y;

                if (_appliedShiftOffsets.TryGetValue(container, out var appliedShift) &&
                    Math.Abs(appliedShift) > 0.01)
                {
                    top -= appliedShift;
                    bottom -= appliedShift;
                }

                _dragLayoutSnapshot.Add(new LayoutSnapshotItem
                {
                    Index = i,
                    Top = top,
                    Bottom = bottom
                });
            }
        }

        private static void CleanupAdorners(ItemsControl ic)
        {
            _edgeAutoScrollTimer.Stop();

            _activeDragItemsControl = null;
            _activeDragScrollViewer = null;
            _hasLastKnownMouseY = false;

            var adornerLayer = AdornerLayer.GetAdornerLayer(ic);
            if (adornerLayer != null)
            {
                if (_dragAdorner != null)
                    adornerLayer.Remove(_dragAdorner);

                if (_ghostInsertionAdorner != null)
                    adornerLayer.Remove(_ghostInsertionAdorner);
            }

            if (_draggedContainer != null)
                _draggedContainer.Opacity = _draggedOriginalOpacity;

            ResetShiftedContainers();

            _dragAdorner = null;
            _ghostInsertionAdorner = null;
            _draggedContainer = null;
            _draggedOriginalOpacity = 1.0;
            _currentInsertIndex = -1;
            _isDragging = false;
            _lastShiftInsertIndex = -1;
            _dragLayoutSnapshot.Clear();
        }

        private static void OnAutoScrollTimerTick()
        {
            if (!_isDragging || _activeDragItemsControl == null || !_hasLastKnownMouseY)
                return;

            var ic = _activeDragItemsControl;

            if (TryAutoScrollAtEdge(ic, _lastKnownMouseY))
            {
                CaptureDragLayoutSnapshot(ic);
                UpdateInsertionPosition(ic, _lastKnownMousePosition, showGhostAtMouse: true);
            }
        }

        private static int ContainerIndexAt(ItemsControl ic, DependencyObject? origin)
        {
            if (origin == null)
                return -1;

            var listBoxItem = System.Windows.Media.VisualTreeHelperExtensions.GetAncestor<ListBoxItem>(origin);
            if (listBoxItem != null)
            {
                var item = ic.ItemContainerGenerator.ItemFromContainer(listBoxItem);
                return ic.Items.IndexOf(item);
            }

            var contentPresenter = System.Windows.Media.VisualTreeHelperExtensions.GetAncestor<ContentPresenter>(origin);
            if (contentPresenter != null)
            {
                var item = ic.ItemContainerGenerator.ItemFromContainer(contentPresenter);
                return ic.Items.IndexOf(item);
            }

            return -1;
        }

        private static bool TryAutoScrollAtEdge(ItemsControl ic, double mouseY)
        {
            var scrollViewer = _activeDragScrollViewer ?? FindScrollViewer(ic);

            if (scrollViewer == null || scrollViewer.ScrollableHeight <= 0)
                return false;

            _activeDragScrollViewer ??= scrollViewer;

            double distanceToTop = mouseY;
            double distanceToBottom = ic.ActualHeight - mouseY;
            double delta = 0;

            if (mouseY < 0)
            {
                delta = -MaxEdgeAutoScrollStepPx;
            }
            else if (mouseY > ic.ActualHeight)
            {
                delta = MaxEdgeAutoScrollStepPx;
            }
            else if (distanceToTop >= 0 && distanceToTop < EdgeAutoScrollZonePx)
            {
                var intensity = 1.0 - distanceToTop / EdgeAutoScrollZonePx;
                delta = -(2.0 + intensity * (MaxEdgeAutoScrollStepPx - 2.0));
            }
            else if (distanceToBottom >= 0 && distanceToBottom < EdgeAutoScrollZonePx)
            {
                var intensity = 1.0 - distanceToBottom / EdgeAutoScrollZonePx;
                delta = 2.0 + intensity * (MaxEdgeAutoScrollStepPx - 2.0);
            }

            if (Math.Abs(delta) < 0.01)
                return false;

            var oldOffset = scrollViewer.VerticalOffset;
            var newOffset = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, oldOffset + delta));

            if (Math.Abs(newOffset - oldOffset) < 0.01)
                return false;

            ResetShiftedContainers();

            scrollViewer.ScrollToVerticalOffset(newOffset);

            // Wichtig: Sonst sind die Container-Positionen nach dem Scrollen noch alt.
            ic.UpdateLayout();

            return true;
        }

        private static ScrollViewer? FindScrollViewer(ItemsControl owner)
        {
            owner.ApplyTemplate();

            if (owner.Template?.FindName("PART_ScrollViewer", owner) is ScrollViewer fromTemplate)
                return fromTemplate;

            var strict = FindScrollViewerRecursive(owner, owner, strictOwnerTemplate: true);
            if (strict != null)
                return strict;

            return FindScrollViewerRecursive(owner, owner, strictOwnerTemplate: false);
        }

        private static ScrollViewer? FindScrollViewerRecursive(
            DependencyObject root,
            ItemsControl owner,
            bool strictOwnerTemplate)
        {
            if (root is ScrollViewer sv)
            {
                if (!strictOwnerTemplate)
                    return sv;

                if (ReferenceEquals(sv.TemplatedParent, owner))
                    return sv;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var result = FindScrollViewerRecursive(child, owner, strictOwnerTemplate);

                if (result != null)
                    return result;
            }

            return null;
        }

        private sealed class GhostInsertionAdorner : Adorner
        {
            private readonly Rectangle _child;
            private readonly TranslateTransform _transform;
            private bool _isVisible;

            public GhostInsertionAdorner(UIElement adornedElement, FrameworkElement draggedItem)
                : base(adornedElement)
            {
                IsHitTestVisible = false;

                _transform = new TranslateTransform();

                var accentBrush = TryFindAccentBrush(adornedElement) ?? Brushes.DodgerBlue;

                var ghostFill = accentBrush.CloneCurrentValue();
                ghostFill.Opacity = 0.16;

                _child = new Rectangle
                {
                    Width = Math.Max(1, draggedItem.ActualWidth),
                    Height = Math.Max(1, draggedItem.ActualHeight - GhostBottomMarginPx),
                    Fill = ghostFill,
                    Stroke = accentBrush,
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection { 4, 4 },
                    RadiusX = 8,
                    RadiusY = 8,
                    RenderTransform = _transform,
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false
                };

                AddVisualChild(_child);
            }

            public void UpdatePosition(double y)
            {
                _transform.X = 0;
                _transform.Y = y;
                InvalidateArrange();
            }

            public void UpdateMousePosition(Point mousePosition)
            {
                _transform.X = mousePosition.X - _child.Width / 2;
                _transform.Y = mousePosition.Y - _child.Height / 2;
                InvalidateArrange();
            }

            public void Show()
            {
                if (_isVisible)
                    return;

                _isVisible = true;
                _child.Visibility = Visibility.Visible;
            }

            public void Hide()
            {
                if (!_isVisible)
                    return;

                _isVisible = false;
                _child.Visibility = Visibility.Collapsed;
            }

            protected override int VisualChildrenCount => 1;

            protected override Visual GetVisualChild(int index)
                => _child;

            protected override Size MeasureOverride(Size constraint)
            {
                _child.Measure(constraint);
                return _child.DesiredSize;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                _child.Arrange(new Rect(new Point(0, 0), _child.DesiredSize));
                return finalSize;
            }

            private static Brush? TryFindAccentBrush(DependencyObject element)
            {
                if (element is FrameworkElement fe)
                {
                    return fe.TryFindResource("MahApps.Brushes.Accent") as Brush
                        ?? fe.TryFindResource("App.Brush.Accent") as Brush;
                }

                return null;
            }
        }

        private sealed class DragAdorner : Adorner
        {
            private readonly Rectangle _child;
            private readonly TranslateTransform _transform;
            private readonly double _offsetX;
            private readonly double _offsetY;

            public DragAdorner(UIElement adornedElement, FrameworkElement draggedItem)
                : base(adornedElement)
            {
                IsHitTestVisible = false;

                _transform = new TranslateTransform();

                _child = new Rectangle
                {
                    Width = draggedItem.ActualWidth,
                    Height = draggedItem.ActualHeight,
                    Fill = new VisualBrush(draggedItem)
                    {
                        Opacity = 0.8,
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top
                    },
                    Effect = new DropShadowEffect
                    {
                        ShadowDepth = 4,
                        Opacity = 0.4,
                        BlurRadius = 8
                    },
                    RenderTransform = _transform,
                    IsHitTestVisible = false
                };

                _offsetX = -draggedItem.ActualWidth / 2;
                _offsetY = -draggedItem.ActualHeight / 2;

                AddVisualChild(_child);
            }

            public void UpdatePosition(Point pos)
            {
                _transform.X = pos.X + _offsetX;
                _transform.Y = pos.Y + _offsetY;
            }

            protected override int VisualChildrenCount => 1;

            protected override Visual GetVisualChild(int index)
                => _child;

            protected override Size MeasureOverride(Size constraint)
            {
                _child.Measure(constraint);
                return _child.DesiredSize;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                _child.Arrange(new Rect(finalSize));
                return finalSize;
            }
        }
    }
}