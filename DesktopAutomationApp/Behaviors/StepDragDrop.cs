using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopAutomationApp.Behaviors;

/// <summary>
/// Gemeinsames Drag-and-Drop fuer Step-Listen.
/// Standardmaessig wird die Zielposition als Linie gezeichnet; optional ordnet eine Live-Vorschau
/// die sichtbaren Karten an der gueltigen Zielposition an. Die Collections aendern sich erst beim Drop.
/// </summary>
public static class StepDragDrop
{
    public const string DataFormat = "DesktopAutomation.StepDragDrop";

    public sealed record MoveRequest(
        IList Source,
        int SourceIndex,
        IList Target,
        int TargetIndex,
        object? TargetItem = null,
        bool InsertAfterTarget = false,
        IReadOnlyList<int>? SourceIndices = null);
    public sealed record DragPayload(IList Source, int SourceIndex, IReadOnlyList<int> SourceIndices);
    public sealed record DragStartRequest(IList Source, int SourceIndex, IReadOnlyList<int> SelectedIndices);
    public delegate IReadOnlyList<int> DragIndexResolver(DragStartRequest request);
    public delegate bool PreviewValidator(MoveRequest request);
    public sealed record InsertionPlacement(int Index, double Y, double X = 8, double Distance = 0, double Width = double.NaN);
    public delegate InsertionPlacement InsertionPlacementResolver(ListBox list, Point pointer);

    public static readonly DependencyProperty IsDragHandleProperty = DependencyProperty.RegisterAttached(
        "IsDragHandle", typeof(bool), typeof(StepDragDrop), new PropertyMetadata(false));
    public static void SetIsDragHandle(DependencyObject element, bool value) => element.SetValue(IsDragHandleProperty, value);
    public static bool GetIsDragHandle(DependencyObject element) => (bool)element.GetValue(IsDragHandleProperty);
    public static readonly DependencyProperty RequireDragHandleProperty = DependencyProperty.RegisterAttached(
        "RequireDragHandle", typeof(bool), typeof(StepDragDrop), new PropertyMetadata(false));
    public static void SetRequireDragHandle(DependencyObject element, bool value) => element.SetValue(RequireDragHandleProperty, value);
    public static bool GetRequireDragHandle(DependencyObject element) => (bool)element.GetValue(RequireDragHandleProperty);
    public static readonly DependencyProperty IsGhostPreviewEnabledProperty = DependencyProperty.RegisterAttached(
        "IsGhostPreviewEnabled", typeof(bool), typeof(StepDragDrop), new PropertyMetadata(false));
    public static void SetIsGhostPreviewEnabled(DependencyObject element, bool value) => element.SetValue(IsGhostPreviewEnabledProperty, value);
    public static bool GetIsGhostPreviewEnabled(DependencyObject element) => (bool)element.GetValue(IsGhostPreviewEnabledProperty);
    private static readonly DependencyProperty PreviewDisplacementProperty = DependencyProperty.RegisterAttached(
        "PreviewDisplacement", typeof(double), typeof(StepDragDrop), new PropertyMetadata(0d));
    internal static void SetPreviewDisplacement(DependencyObject element, double value) => element.SetValue(PreviewDisplacementProperty, value);
    internal static double GetPreviewDisplacement(DependencyObject element) => (double)element.GetValue(PreviewDisplacementProperty);
    private static StepDragPreviewSession? _ghostPreview;

    public static readonly DependencyProperty PlacementResolverProperty = DependencyProperty.RegisterAttached(
        "PlacementResolver", typeof(InsertionPlacementResolver), typeof(StepDragDrop), new PropertyMetadata(null));
    public static void SetPlacementResolver(DependencyObject element, InsertionPlacementResolver? value)
        => element.SetValue(PlacementResolverProperty, value);
    public static InsertionPlacementResolver? GetPlacementResolver(DependencyObject element)
        => element.GetValue(PlacementResolverProperty) as InsertionPlacementResolver;

    public static readonly DependencyProperty HoverExpandCommandProperty = DependencyProperty.RegisterAttached(
        "HoverExpandCommand", typeof(ICommand), typeof(StepDragDrop), new PropertyMetadata(null));
    public static void SetHoverExpandCommand(DependencyObject element, ICommand? value)
        => element.SetValue(HoverExpandCommandProperty, value);
    public static ICommand? GetHoverExpandCommand(DependencyObject element)
        => element.GetValue(HoverExpandCommandProperty) as ICommand;

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

    public static readonly DependencyProperty IsLivePreviewEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsLivePreviewEnabled",
            typeof(bool),
            typeof(StepDragDrop),
            new PropertyMetadata(false));

    public static void SetIsLivePreviewEnabled(DependencyObject element, bool value)
        => element.SetValue(IsLivePreviewEnabledProperty, value);

    public static bool GetIsLivePreviewEnabled(DependencyObject element)
        => (bool)element.GetValue(IsLivePreviewEnabledProperty);

    public static readonly DependencyProperty DragIndicesResolverProperty =
        DependencyProperty.RegisterAttached(
            "DragIndicesResolver",
            typeof(DragIndexResolver),
            typeof(StepDragDrop),
            new PropertyMetadata(null));

    public static void SetDragIndicesResolver(DependencyObject element, DragIndexResolver? value)
        => element.SetValue(DragIndicesResolverProperty, value);

    public static DragIndexResolver? GetDragIndicesResolver(DependencyObject element)
        => element.GetValue(DragIndicesResolverProperty) as DragIndexResolver;

    public static readonly DependencyProperty PreviewMoveValidatorProperty =
        DependencyProperty.RegisterAttached(
            "PreviewMoveValidator",
            typeof(PreviewValidator),
            typeof(StepDragDrop),
            new PropertyMetadata(null));

    public static void SetPreviewMoveValidator(DependencyObject element, PreviewValidator? value)
        => element.SetValue(PreviewMoveValidatorProperty, value);

    public static PreviewValidator? GetPreviewMoveValidator(DependencyObject element)
        => element.GetValue(PreviewMoveValidatorProperty) as PreviewValidator;

    public static readonly DependencyProperty PreviewItemsProperty =
        DependencyProperty.RegisterAttached(
            "PreviewItems",
            typeof(IList),
            typeof(StepDragDrop),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static void SetPreviewItems(DependencyObject element, IList? value)
        => element.SetValue(PreviewItemsProperty, value);

    public static IList? GetPreviewItems(DependencyObject element)
        => element.GetValue(PreviewItemsProperty) as IList;

    private static Point _dragStart;
    private static ListBox? _sourceList;
    private static int _sourceIndex = -1;
    private static IReadOnlyList<int> _dragSelection = [];
    private static object? _deferredSelectionItem;
    private static bool _isDragging;
    private static DragPayload? _activePayload;
    private static IReadOnlyList<PreviewImage> _previewImages = [];
    private static readonly Dictionary<FrameworkElement, ElementVisualState> PreviewElementStates = [];
    private static ListBox? _previewSizeOwner;
    private static double _previewOwnerMinHeight;
    private static LivePlacementAdorner? _livePreview;
    private static ListBox? _livePreviewOwner;
    private static int _livePreviewTargetIndex = -1;
    private static IReadOnlyList<PreviewHitZone> _livePreviewHitZones = [];
    private static ListCollectionView? _previewSortedView;
    private static IComparer? _previewOriginalComparer;
    private static ListBox? _previewProjectionOwner;
    private static ListBox? _indicatorOwner;
    private static InsertionAdorner? _indicator;
    private static int _targetIndex = -1;
    private static DragScrollSession? _scrollSession;
    private static ListBox? _scrollTarget;
    private static DispatcherTimer? _hoverTimer;
    private static object? _hoverItem;
    private static ListBox? _hoverList;

    private static void OnMoveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox list)
            return;

        if (e.NewValue is null && (ReferenceEquals(_sourceList, list) || ReferenceEquals(_ghostPreview?.Target, list))) CleanupDrag();

        list.PreviewMouseLeftButtonDown -= OnMouseDown;
        list.PreviewMouseLeftButtonUp -= OnMouseUp;
        list.PreviewMouseRightButtonDown -= OnCancelPendingDrag;
        list.PreviewMouseMove -= OnMouseMove;
        list.DragOver -= OnDragOver;
        list.DragLeave -= OnDragLeave;
        list.Drop -= OnDrop;
        list.Unloaded -= OnListUnloaded;
        list.QueryContinueDrag -= OnQueryContinueDrag;
        list.GiveFeedback -= OnGiveFeedback;
        list.AllowDrop = false;

        if (e.NewValue is not ICommand)
            return;

        list.AllowDrop = true;
        list.PreviewMouseLeftButtonDown += OnMouseDown;
        list.PreviewMouseLeftButtonUp += OnMouseUp;
        list.PreviewMouseRightButtonDown += OnCancelPendingDrag;
        list.PreviewMouseMove += OnMouseMove;
        list.DragOver += OnDragOver;
        list.DragLeave += OnDragLeave;
        list.Drop += OnDrop;
        list.Unloaded += OnListUnloaded;
        list.QueryContinueDrag += OnQueryContinueDrag;
        list.GiveFeedback += OnGiveFeedback;
    }

    private static void OnListUnloaded(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, _sourceList) || ReferenceEquals(sender, _indicatorOwner)
            || ReferenceEquals(sender, _livePreviewOwner) || ReferenceEquals(sender, _hoverList)
            || ReferenceEquals(sender, _ghostPreview?.Target))
            CleanupDrag();
    }

    private static void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        _ghostPreview?.RefreshPointer();
        if (_scrollTarget is { } list && _ghostPreview is { } preview
            && !ContainsPointer(list, preview.PointerRelativeTo(list))
            && !preview.TryRetainStationaryTarget(out _))
            ClearTargetPreview(list);
    }

    private static void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
    {
        if (!e.EscapePressed) return;
        e.Action = DragAction.Cancel;
        e.Handled = true;
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ResetPendingDrag();
        if (sender is not ListBox list || IsInteractiveElement(e.OriginalSource as DependencyObject)
            || GetRequireDragHandle(list) && !IsInsideDragHandle(e.OriginalSource as DependencyObject))
            return;

        var container = ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (container == null)
            return;

        _dragStart = e.GetPosition(list);
        _sourceIndex = list.ItemContainerGenerator.IndexFromContainer(container);
        _sourceList = _sourceIndex >= 0 ? list : null;
        if (_sourceList is null || list.ItemsSource is not IList source) return;
        _dragSelection = list.SelectedItems.Contains(source[_sourceIndex])
            ? list.SelectedItems.Cast<object>().Select(source.IndexOf).Where(index => index >= 0).Order().ToArray()
            : [_sourceIndex];
        if (_dragSelection.Count > 1 && Keyboard.Modifiers == ModifierKeys.None)
        {
            // Keep a group selected until we know whether this gesture is a click or a drag.
            _deferredSelectionItem = source[_sourceIndex];
            e.Handled = true;
        }
    }

    private static void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            if (sender is ListBox list && ReferenceEquals(list, _sourceList) && _deferredSelectionItem is { } item)
            {
                list.SelectedItems.Clear();
                list.SelectedItem = item;
            }
            ResetPendingDrag();
        }
    }

    private static void OnCancelPendingDrag(object sender, MouseButtonEventArgs e)
        => ResetPendingDrag();

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging
            || sender is not ListBox list
            || !ReferenceEquals(list, _sourceList)
            || _sourceIndex < 0
            || e.LeftButton != MouseButtonState.Pressed
            || (e.GetPosition(list) - _dragStart).Length <= 6
            || list.ItemsSource is not IList source
            || _sourceIndex >= source.Count)
            return;

        _isDragging = true;

        try
        {
            IReadOnlyList<int> sourceIndices = _dragSelection;
            if (GetDragIndicesResolver(list) is { } resolver)
                sourceIndices = resolver(new DragStartRequest(source, _sourceIndex, sourceIndices));
            sourceIndices = sourceIndices
                .Where(index => index >= 0 && index < source.Count)
                .Distinct()
                .OrderBy(index => index)
                .ToArray();
            if (sourceIndices.Count == 0)
                sourceIndices = [_sourceIndex];

            BeginDragSession(list, new DragPayload(source, _sourceIndex, sourceIndices));
            _previewImages = GetIsLivePreviewEnabled(list) ? CapturePreviewImages(list, sourceIndices) : [];
            foreach (var index in GetIsLivePreviewEnabled(list) ? sourceIndices : Array.Empty<int>())
            {
                if (list.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem item)
                    continue;
                RememberVisualState(item);
                item.Opacity = 0.45;
            }

            var data = new DataObject(DataFormat, _activePayload);
            DragDrop.DoDragDrop(list, data, DragDropEffects.Move);
        }
        finally
        {
            CleanupDrag();
        }
    }

    internal static void BeginDragSession(ListBox list, DragPayload payload, bool startTimers = true)
    {
        _sourceList = list;
        _activePayload = payload;
        _isDragging = true;
        if (GetIsGhostPreviewEnabled(list)) _ghostPreview = new StepDragPreviewSession(list, payload, startTimers);
        FrameworkElement root = list;
        for (var parent = VisualTreeHelper.GetParent(list); parent is not null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is UserControl control) { root = control; break; }
        _scrollSession = new DragScrollSession(root, RefreshAfterScroll, startTimers);
    }

    internal static StepDragPreviewSession? ActivePreview => _ghostPreview;
    internal static DragScrollSession? ActiveScrolling => _scrollSession;

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if (sender is not ListBox list || !TryGetPayload(e.Data, out var payload))
            return;

        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        _scrollTarget = list;
        e.Effects = UpdateDragTarget(list, payload, e.GetPosition(list));
    }

    private static void RefreshAfterScroll()
    {
        if (_scrollTarget is not { } list || _activePayload is not { } payload || _scrollSession is null) return;
        _ghostPreview?.ReleasePointerRetention();
        var position = _scrollSession.PointerRelativeTo(list);
        if (!ContainsPointer(list, position))
        {
            _ghostPreview?.UpdatePointerFrom(list, position);
            ClearTargetPreview(list);
            return;
        }
        if (_ghostPreview is null) ClearLivePreviewVisuals();
        UpdateDragTarget(list, payload, position);
    }

    private static bool ContainsPointer(ListBox list, Point position) =>
        list.Visibility == Visibility.Visible && position.X >= 0 && position.Y >= 0
        && position.X <= list.ActualWidth && position.Y <= list.ActualHeight;

    internal static DragDropEffects UpdateDragTarget(ListBox list, DragPayload payload, Point position)
    {
        var stationary = RetainStationaryTarget(ref list, ref position);
        if (!stationary && !ContainsPointer(list, position))
        {
            _ghostPreview?.UpdatePointerFrom(list, position);
            ClearTargetPreview(list);
            return DragDropEffects.None;
        }
        _scrollTarget = list;
        var effects = DragDropEffects.Move;
        _ghostPreview?.UpdatePointerFrom(list, position);
        UpdateHoverExpansion(list, payload, position);

        var placement = GetInsertionPlacement(list, position);
        placement = _ghostPreview?.RetainTarget(list, position, placement) ?? placement;
        var targetIndex = TryGetLiveTargetIndex(list, position.Y, out var liveTargetIndex)
            ? liveTargetIndex
            : placement.Index;
        _targetIndex = targetIndex;
        if (_ghostPreview is not null)
        {
            effects = _ghostPreview.UpdateTarget(list, placement, CanAcceptDrop(list, payload, targetIndex))
                ? DragDropEffects.Move : DragDropEffects.None;
        }
        else if (GetIsLivePreviewEnabled(list))
            effects = ShowLivePreview(list, targetIndex) ? DragDropEffects.Move : DragDropEffects.None;
        else
        {
            ClearLivePreviewVisuals();
            if (CanAcceptDrop(list, payload, targetIndex))
                ShowIndicator(list, placement.Y, list.Items.Count == 0, placement.X);
            else
            {
                RemoveIndicator();
                effects = DragDropEffects.None;
            }
        }
        return effects;
    }

    private static bool RetainStationaryTarget(ref ListBox list, ref Point position)
    {
        if (_ghostPreview is not { } preview) return false;
        preview.UpdatePointerFrom(list, position);
        if (!preview.TryRetainStationaryTarget(out var retained)) return false;
        position = list.TranslatePoint(position, retained);
        list = retained;
        return true;
    }

    public static bool KeepStationaryPreview(ListBox list, Point position)
        => RetainStationaryTarget(ref list, ref position) && _activePayload is { } payload
           && _ghostPreview is { } preview && CanAcceptDrop(list, payload, preview.TargetIndex);

    public static DragDropEffects? DropRetainedTarget(ListBox list, Point position, IDataObject data)
    {
        if (!TryGetPayload(data, out var payload) || !RetainStationaryTarget(ref list, ref position)
            || list.ItemsSource is not IList target || GetMoveCommand(list) is not { } command
            || _ghostPreview is not { } preview) return null;
        var request = new MoveRequest(payload.Source, payload.SourceIndex, target, preview.TargetIndex,
            SourceIndices: payload.SourceIndices);
        var valid = CanAcceptDrop(list, payload, preview.TargetIndex) && command.CanExecute(request);
        if (valid) command.Execute(request);
        ClearTargetPreview();
        return valid ? DragDropEffects.Move : DragDropEffects.None;
    }

    private static void OnDragLeave(object sender, DragEventArgs e)
    {
        if (sender is not ListBox list
            || _ghostPreview is null && !ReferenceEquals(list, _indicatorOwner)
               && !ReferenceEquals(list, _livePreview?.AdornedElement)
               && !ReferenceEquals(list, _hoverList))
            return;

        var point = e.GetPosition(list);
        if (point.X >= 0 && point.X <= list.ActualWidth
            && point.Y >= 0 && point.Y <= list.ActualHeight)
            return;

        _ghostPreview?.UpdatePointerFrom(list, point);
        if (_ghostPreview?.TryRetainStationaryTarget(out _) == true) return;
        ClearTargetPreview(list);
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not ListBox list
            || list.ItemsSource is not IList target
            || !TryGetPayload(e.Data, out var payload)
            || GetMoveCommand(list) is not { } command)
            return;

        var position = e.GetPosition(list);
        if (DropRetainedTarget(list, position, e.Data) is { } retainedEffect)
        {
            e.Effects = retainedEffect;
            e.Handled = true;
            return;
        }
        RetainStationaryTarget(ref list, ref position);
        if (list.ItemsSource is not IList retainedTarget || GetMoveCommand(list) is not { } retainedCommand) return;
        target = retainedTarget;
        command = retainedCommand;
        var placement = GetInsertionPlacement(list, position);
        placement = _ghostPreview?.RetainTarget(list, position, placement) ?? placement;
        var targetIndex = TryGetLiveTargetIndex(list, position.Y, out var liveTargetIndex)
            ? liveTargetIndex
            : placement.Index;
        if (_ghostPreview is not null && !_ghostPreview.UpdateTarget(list, placement,
                CanAcceptDrop(list, payload, targetIndex)))
        {
            ClearTargetPreview();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }
        ClearLivePreviewVisuals();
        var dropTarget = GetDropTarget(list, e);
        var request = new MoveRequest(
            payload.Source,
            payload.SourceIndex,
            target,
            targetIndex,
            dropTarget.Item,
            dropTarget.InsertAfter,
            payload.SourceIndices);
        _ghostPreview?.ClearTarget();
        if (CanAcceptDrop(list, payload, targetIndex) && command.CanExecute(request))
            command.Execute(request);

        ClearTargetPreview();
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

    internal static bool CanAcceptDrop(ListBox list, DragPayload payload, int targetIndex)
    {
        if (list.ItemsSource is not IList target || GetMoveCommand(list) is not { } command) return false;
        var request = new MoveRequest(payload.Source, payload.SourceIndex, target, targetIndex,
            SourceIndices: payload.SourceIndices);
        return command.CanExecute(request) && (GetPreviewMoveValidator(list)?.Invoke(request) ?? true);
    }

    /// <summary>
    /// Zeigt die Ablageposition an, wenn der Mauszeiger ueber dem Balken eines Bereichs liegt.
    /// Ein leerer Bereich zeigt seine gesamte Ablageflaeche, ein belegter die Position am Ende.
    /// </summary>
    public static bool ShowSectionTarget(ListBox list, Point? pointer = null)
    {
        if (!_isDragging || _activePayload is not { } payload)
        { ClearTargetPreview(); return false; }

        if (pointer is { } point && KeepStationaryPreview(list, point)) return true;
        if (!CanAcceptDrop(list, payload, list.Items.Count)) { ClearTargetPreview(); return false; }
        _targetIndex = list.Items.Count;
        if (_ghostPreview is not null)
            return _ghostPreview.UpdateTarget(list, new(_targetIndex, GetEndInsertionY(list), GetSectionInsertionX(list)), true, force: true);
        if (GetIsLivePreviewEnabled(list))
            return ShowLivePreview(list, _targetIndex);
        else
            ShowIndicator(list, GetEndInsertionY(list), list.Items.Count == 0, GetSectionInsertionX(list));
        return true;
    }

    /// <summary>
    /// Shows the insertion target at index zero when hovering a section header.
    /// </summary>
    public static bool ShowSectionStartTarget(ListBox list, Point? pointer = null)
    {
        if (!_isDragging || _activePayload is not { } payload)
        { ClearTargetPreview(); return false; }

        if (pointer is { } point && KeepStationaryPreview(list, point)) return true;
        if (!CanAcceptDrop(list, payload, 0)) { ClearTargetPreview(); return false; }
        _targetIndex = 0;
        if (_ghostPreview is not null)
            return _ghostPreview.UpdateTarget(list, new(0, Math.Max(2, GetInsertionPlacement(list, new Point(0, 0)).Y), GetSectionInsertionX(list)), true, force: true);
        if (GetIsLivePreviewEnabled(list))
            return ShowLivePreview(list, _targetIndex);
        else
        {
            var insertionY = list.Items.Count == 0
                ? Math.Max(1, list.ActualHeight / 2)
                : GetInsertionPlacement(list, new Point(0, 0)).Y;
            ShowIndicator(list, insertionY, list.Items.Count == 0, GetSectionInsertionX(list));
        }
        return true;
    }

    public static void ClearTargetPreview(ListBox? list = null)
    {
        if (list is null || ReferenceEquals(list, _scrollTarget)) _scrollTarget = null;
        _ghostPreview?.ClearTarget(list);
        if (list is null || ReferenceEquals(list, _hoverList)) ClearHoverExpansion();
        if (list == null || ReferenceEquals(list, _indicatorOwner) || ReferenceEquals(list, _livePreview?.AdornedElement))
        {
            ClearLivePreviewVisuals();
            RemoveIndicator();
        }
    }

    private static double GetSectionInsertionX(ListBox list)
        => GetPlacementResolver(list)?.Invoke(list, new Point(0, double.NegativeInfinity)).X ?? 8;

    private static void UpdateHoverExpansion(ListBox list, DragPayload payload, Point position)
    {
        var hit = list.InputHitTest(position) as DependencyObject;
        var container = hit is null ? null : ItemsControl.ContainerFromElement(list, hit) as ListBoxItem;
        var item = container?.DataContext;
        var command = GetHoverExpandCommand(list);
        var draggedItem = item is not null && payload.SourceIndices.Any(index => index >= 0 && index < payload.Source.Count
            && ReferenceEquals(payload.Source[index], item));
        if (item is null || command is null || draggedItem || !command.CanExecute(item))
        {
            ClearHoverExpansion();
            return;
        }
        if (ReferenceEquals(list, _hoverList) && ReferenceEquals(item, _hoverItem)) return;
        ClearHoverExpansion();
        _hoverList = list;
        _hoverItem = item;
        _hoverTimer = new DispatcherTimer(DispatcherPriority.Input, list.Dispatcher) { Interval = TimeSpan.FromMilliseconds(700) };
        _hoverTimer.Tick += (_, _) =>
        {
            ClearHoverExpansion();
            if (!_isDragging || !command.CanExecute(item)) return;
            command.Execute(item);
            _ghostPreview?.ReleasePointerRetention();
            list.UpdateLayout();
            // Recompute the insertion lane immediately after the block opens.
            _ghostPreview?.RefreshPointer();
            var placement = GetInsertionPlacement(list, _ghostPreview?.PointerRelativeTo(list) ?? Mouse.GetPosition(list));
            if (_ghostPreview is not null) _ghostPreview.UpdateTarget(list, placement, CanAcceptDrop(list, payload, placement.Index));
            else if (CanAcceptDrop(list, payload, placement.Index))
                ShowIndicator(list, placement.Y, list.Items.Count == 0, placement.X);
            else RemoveIndicator();
        };
        _hoverTimer.Start();
    }

    private static void ClearHoverExpansion()
    {
        _hoverTimer?.Stop();
        _hoverTimer = null;
        _hoverList = null;
        _hoverItem = null;
    }

    internal static IReadOnlyList<int> BuildPreviewOrder(
        int itemCount,
        IReadOnlyList<int> movingIndices,
        int targetIndex)
    {
        var moving = movingIndices
            .Where(index => index >= 0 && index < itemCount)
            .Distinct()
            .OrderBy(index => index)
            .ToArray();
        var movingSet = moving.ToHashSet();
        var remaining = Enumerable.Range(0, itemCount).Where(index => !movingSet.Contains(index)).ToList();
        var insertAt = Math.Clamp(
            targetIndex - moving.Count(index => index < targetIndex),
            0,
            remaining.Count);
        remaining.InsertRange(insertAt, moving);
        return remaining;
    }

    private static bool ShowLivePreview(ListBox targetList, int targetIndex)
    {
        if (_activePayload is not { } payload || targetList.ItemsSource is not IList target)
            return false;

        targetIndex = Math.Clamp(targetIndex, 0, target.Count);
        if (ReferenceEquals(targetList, _livePreviewOwner) && targetIndex == _livePreviewTargetIndex)
            return true;

        ClearLivePreviewVisuals();

        var request = new MoveRequest(
            payload.Source,
            payload.SourceIndex,
            target,
            targetIndex,
            SourceIndices: payload.SourceIndices);
        if (GetPreviewMoveValidator(targetList) is { } validator && !validator(request))
            return false;

        if (_sourceList == null || _previewImages.Count == 0)
            return false;

        if (ReferenceEquals(_sourceList, targetList))
            ApplySameListPreview(targetList, payload.SourceIndices, targetIndex);
        else
            ApplyCrossListPreview(_sourceList, targetList, payload.SourceIndices, targetIndex);
        _livePreviewOwner = targetList;
        _livePreviewTargetIndex = targetIndex;
        return true;
    }

    private static void ApplySameListPreview(ListBox list, IReadOnlyList<int> movingIndices, int targetIndex)
    {
        var order = BuildPreviewOrder(list.Items.Count, movingIndices, targetIndex);
        if (list.ItemsSource is not IList source
            || CollectionViewSource.GetDefaultView(source) is not ListCollectionView view)
            return;

        var projectedItems = order.Select(index => source[index]!).ToList();
        _previewSortedView = view;
        _previewOriginalComparer = view.CustomSort;
        _previewProjectionOwner = list;
        SetPreviewItems(list, projectedItems);
        view.CustomSort = new PreviewOrderComparer(projectedItems);
        view.Refresh();
        list.UpdateLayout();

        var movingSet = movingIndices.ToHashSet();
        var hitZones = new List<PreviewHitZone>();
        for (var visualIndex = 0; visualIndex < list.Items.Count; visualIndex++)
        {
            var projectedItem = list.Items[visualIndex];
            var originalIndex = source.IndexOf(projectedItem);
            if (originalIndex < 0
                || list.ItemContainerGenerator.ContainerFromIndex(visualIndex) is not ListBoxItem item)
                continue;
            RememberVisualState(item);
            var top = item.TranslatePoint(new Point(0, 0), list).Y;
            var zoneBottom = top + Math.Max(item.ActualHeight, item.DesiredSize.Height);
            if (!movingSet.Contains(originalIndex))
            {
                hitZones.Add(new PreviewHitZone(
                    top,
                    zoneBottom,
                    originalIndex,
                    originalIndex + 1,
                    HoldsCurrentTarget: false));
                continue;
            }

            item.Opacity = 0.62;
            hitZones.Add(new PreviewHitZone(
                top,
                zoneBottom,
                targetIndex,
                targetIndex,
                HoldsCurrentTarget: true));
        }
        _livePreviewHitZones = hitZones;
    }

    private static void ApplyCrossListPreview(
        ListBox sourceList,
        ListBox targetList,
        IReadOnlyList<int> movingIndices,
        int targetIndex)
    {
        CollapseSourcePreview(sourceList, movingIndices);

        var insertionY = GetInsertionTop(targetList, targetIndex);
        var previewHeight = _previewImages.Sum(image => image.SlotHeight);
        for (var index = Math.Clamp(targetIndex, 0, targetList.Items.Count); index < targetList.Items.Count; index++)
        {
            if (targetList.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem item)
                continue;
            RememberVisualState(item);
            item.RenderTransform = new TranslateTransform(0, previewHeight);
        }

        _previewSizeOwner = targetList;
        _previewOwnerMinHeight = targetList.MinHeight;
        targetList.MinHeight = Math.Max(targetList.ActualHeight + previewHeight, targetList.MinHeight);

        var placements = new List<PreviewPlacement>();
        var hitZones = new List<PreviewHitZone>();
        var y = insertionY;
        foreach (var image in _previewImages)
        {
            placements.Add(new PreviewPlacement(image, y));
            hitZones.Add(new PreviewHitZone(
                y,
                y + image.SlotHeight,
                targetIndex,
                targetIndex,
                HoldsCurrentTarget: true));
            y += image.SlotHeight;
        }
        for (var index = 0; index < targetList.Items.Count; index++)
        {
            if (targetList.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem item)
                continue;
            var top = item.TranslatePoint(new Point(0, 0), targetList).Y;
            hitZones.Add(new PreviewHitZone(
                top,
                top + Math.Max(item.ActualHeight, item.DesiredSize.Height),
                index,
                index + 1,
                HoldsCurrentTarget: false));
        }
        _livePreviewHitZones = hitZones.OrderBy(zone => zone.Top).ToArray();
        ShowLivePlacementAdorner(targetList, placements);
    }

    private static bool TryGetLiveTargetIndex(ListBox list, double pointerY, out int targetIndex)
    {
        targetIndex = -1;
        if (!ReferenceEquals(list, _livePreviewOwner) || _livePreviewHitZones.Count == 0)
            return false;

        var zone = _livePreviewHitZones.FirstOrDefault(candidate =>
            pointerY >= candidate.Top && pointerY <= candidate.Bottom);
        if (zone == null)
        {
            if (pointerY < _livePreviewHitZones[0].Top)
                targetIndex = 0;
            else if (pointerY > _livePreviewHitZones[^1].Bottom)
                targetIndex = list.Items.Count;
            else
                targetIndex = _livePreviewTargetIndex;
            return true;
        }

        targetIndex = ResolvePreviewHitZoneTarget(
            pointerY,
            zone.Top,
            zone.Bottom,
            zone.BeforeIndex,
            zone.AfterIndex,
            zone.HoldsCurrentTarget,
            _livePreviewTargetIndex);
        return true;
    }

    internal static int ResolvePreviewHitZoneTarget(
        double pointerY,
        double top,
        double bottom,
        int beforeIndex,
        int afterIndex,
        bool holdsCurrentTarget,
        int currentTargetIndex)
        => holdsCurrentTarget
            ? currentTargetIndex
            : pointerY < (top + bottom) / 2
                ? beforeIndex
                : afterIndex;

    private static void CollapseSourcePreview(ListBox sourceList, IReadOnlyList<int> movingIndices)
    {
        var movingSet = movingIndices.ToHashSet();
        var slotTops = GetSlotTops(sourceList);
        var remaining = Enumerable.Range(0, sourceList.Items.Count)
            .Where(index => !movingSet.Contains(index))
            .ToArray();
        for (var visualIndex = 0; visualIndex < remaining.Length && visualIndex < slotTops.Count; visualIndex++)
        {
            var originalIndex = remaining[visualIndex];
            if (sourceList.ItemContainerGenerator.ContainerFromIndex(originalIndex) is not ListBoxItem item)
                continue;
            RememberVisualState(item);
            item.RenderTransform = new TranslateTransform(0, slotTops[visualIndex] - slotTops[originalIndex]);
        }
        foreach (var index in movingIndices)
        {
            if (sourceList.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem item)
                continue;
            RememberVisualState(item);
            item.Opacity = 0.08;
        }
    }

    private static List<double> GetSlotTops(ListBox list)
    {
        var result = new List<double>(list.Items.Count);
        for (var index = 0; index < list.Items.Count; index++)
        {
            if (list.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem item)
                return [];
            result.Add(item.TranslatePoint(new Point(0, 0), list).Y);
        }
        return result;
    }

    private static double GetInsertionTop(ListBox list, int targetIndex)
    {
        if (targetIndex >= 0
            && targetIndex < list.Items.Count
            && list.ItemContainerGenerator.ContainerFromIndex(targetIndex) is ListBoxItem target)
            return target.TranslatePoint(new Point(0, 0), list).Y;
        if (list.Items.Count > 0
            && list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1) is ListBoxItem last)
            return last.TranslatePoint(new Point(0, last.ActualHeight), list).Y + last.Margin.Bottom;
        return 4;
    }

    private static IReadOnlyList<PreviewImage> CapturePreviewImages(ListBox list, IReadOnlyList<int> indices)
    {
        var result = new List<PreviewImage>();
        foreach (var index in indices)
        {
            if (list.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem item
                || item.ActualWidth <= 0
                || item.ActualHeight <= 0)
                continue;
            var dpi = VisualTreeHelper.GetDpi(item);
            var bitmap = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Ceiling(item.ActualWidth * dpi.DpiScaleX)),
                Math.Max(1, (int)Math.Ceiling(item.ActualHeight * dpi.DpiScaleY)),
                dpi.PixelsPerInchX,
                dpi.PixelsPerInchY,
                PixelFormats.Pbgra32);
            bitmap.Render(item);
            bitmap.Freeze();
            result.Add(new PreviewImage(
                bitmap,
                new Size(item.ActualWidth, item.ActualHeight),
                Math.Max(item.ActualHeight, item.DesiredSize.Height)));
        }
        return result;
    }

    private static void ShowLivePlacementAdorner(ListBox list, IReadOnlyList<PreviewPlacement> placements)
    {
        if (placements.Count == 0)
            return;
        var layer = AdornerLayer.GetAdornerLayer(list);
        if (layer == null)
            return;
        _livePreview = new LivePlacementAdorner(list, placements);
        layer.Add(_livePreview);
    }

    private static void RememberVisualState(FrameworkElement element)
    {
        if (!PreviewElementStates.ContainsKey(element))
            PreviewElementStates[element] = new ElementVisualState(
                element.Opacity,
                element.RenderTransform,
                Panel.GetZIndex(element));
    }

    private static void ClearLivePreviewVisuals()
    {
        if (_livePreview != null)
            AdornerLayer.GetAdornerLayer(_livePreview.AdornedElement)?.Remove(_livePreview);
        _livePreview = null;
        foreach (var (element, state) in PreviewElementStates)
        {
            element.Opacity = state.Opacity;
            element.RenderTransform = state.RenderTransform;
            Panel.SetZIndex(element, state.ZIndex);
        }
        PreviewElementStates.Clear();
        if (_previewSizeOwner != null)
            _previewSizeOwner.MinHeight = _previewOwnerMinHeight;
        _previewSizeOwner = null;
        if (_previewProjectionOwner != null)
            SetPreviewItems(_previewProjectionOwner, null);
        if (_previewSortedView != null)
        {
            _previewSortedView.CustomSort = _previewOriginalComparer;
            _previewSortedView.Refresh();
        }
        _previewProjectionOwner?.UpdateLayout();
        _previewSortedView = null;
        _previewOriginalComparer = null;
        _previewProjectionOwner = null;
        _livePreviewOwner = null;
        _livePreviewTargetIndex = -1;
        _livePreviewHitZones = [];
    }

    private static InsertionPlacement GetInsertionPlacement(ListBox list, Point pointer)
    {
        pointer = _ghostPreview?.WithoutPreviewGap(list, pointer) ?? pointer;
        if (GetPlacementResolver(list) is { } resolver) return resolver(list, pointer);
        if (list.Items.Count == 0)
            return new(0, Math.Max(1, list.ActualHeight / 2));

        (int Index, double Bottom, Thickness Margin)? previous = null;
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (list.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem item)
                continue;

            var top = item.TranslatePoint(new Point(0, 0), list).Y - GetPreviewDisplacement(item);
            var bottom = top + item.ActualHeight;
            if (pointer.Y < top + item.ActualHeight / 2)
            {
                var lineY = previous is { } before
                    ? (before.Bottom + top) / 2
                    : Math.Max(2, top - item.Margin.Top / 2);
                return new(i, lineY);
            }
            previous = (i, bottom, item.Margin);
        }

        return previous is { } last
            ? new(last.Index + 1, last.Bottom + last.Margin.Bottom / 2)
            : new(list.Items.Count, Math.Max(1, list.ActualHeight - 2));
    }

    private static (object? Item, bool InsertAfter) GetDropTarget(ListBox list, DragEventArgs e)
    {
        var container = ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (container == null)
            return (null, false);

        var index = list.ItemContainerGenerator.IndexFromContainer(container);
        if (index < 0 || index >= list.Items.Count)
            return (null, false);

        return (list.Items[index], e.GetPosition(container).Y >= container.ActualHeight / 2);
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
            return bottom - GetPreviewDisplacement(item) + item.Margin.Bottom / 2;
        }

        return Math.Max(2, list.ActualHeight - 2);
    }

    private static void ShowIndicator(ListBox list, double y, bool empty, double x = 8)
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

        _indicator?.Update(y, empty, x);
    }

    private static void RemoveIndicator()
    {
        if (_indicator != null && _indicatorOwner != null)
            AdornerLayer.GetAdornerLayer(_indicatorOwner)?.Remove(_indicator);
        _indicator = null;
        _indicatorOwner = null;
        _targetIndex = -1;
    }

    internal static double GetHorizontalScrollOffset(ListBox list) => FindDescendantScrollViewer(list)?.HorizontalOffset ?? 0;

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

    internal static bool IsInsideDragHandle(DependencyObject? element)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (GetIsDragHandle(current)) return true;
            if (current is ListBoxItem) return false;
        }
        return false;
    }

    private static void ResetPendingDrag()
    {
        _sourceList = null;
        _sourceIndex = -1;
        _dragSelection = [];
        _deferredSelectionItem = null;
    }

    internal static void CleanupDrag()
    {
        _scrollSession?.Dispose();
        _scrollSession = null;
        _scrollTarget = null;
        _ghostPreview?.Dispose();
        _ghostPreview = null;
        ClearHoverExpansion();
        ClearLivePreviewVisuals();
        RemoveIndicator();
        _activePayload = null;
        _previewImages = [];
        _isDragging = false;
        ResetPendingDrag();
    }

    private sealed record ElementVisualState(double Opacity, Transform RenderTransform, int ZIndex);
    private sealed record PreviewImage(ImageSource Image, Size Size, double SlotHeight);
    private sealed record PreviewPlacement(PreviewImage Image, double Y);
    private sealed record PreviewHitZone(
        double Top,
        double Bottom,
        int BeforeIndex,
        int AfterIndex,
        bool HoldsCurrentTarget);

    private sealed class PreviewOrderComparer(IReadOnlyList<object> orderedItems) : IComparer
    {
        private readonly Dictionary<object, int> _rank = orderedItems
            .Select((item, index) => (item, index))
            .ToDictionary(pair => pair.item, pair => pair.index, ReferenceEqualityComparer.Instance);

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            var xRank = x != null && _rank.TryGetValue(x, out var left) ? left : int.MaxValue;
            var yRank = y != null && _rank.TryGetValue(y, out var right) ? right : int.MaxValue;
            return xRank.CompareTo(yRank);
        }
    }

    private sealed class LivePlacementAdorner : Adorner
    {
        private readonly IReadOnlyList<PreviewPlacement> _placements;

        public LivePlacementAdorner(UIElement adornedElement, IReadOnlyList<PreviewPlacement> placements)
            : base(adornedElement)
        {
            _placements = placements;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.PushOpacity(0.62);
            foreach (var placement in _placements)
            {
                drawingContext.DrawImage(
                    placement.Image.Image,
                    new Rect(new Point(0, placement.Y), placement.Image.Size));
            }
            drawingContext.Pop();
        }
    }

    private sealed class InsertionAdorner : Adorner
    {
        private double _y;
        private double _x = 8;
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

        public void Update(double y, bool empty, double x)
        {
            _y = y;
            _empty = empty;
            _x = x;
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
            drawingContext.DrawLine(penLine, new Point(_x, y), new Point(Math.Max(_x, width - 8), y));
            drawingContext.DrawEllipse(_accent, null, new Point(_x, y), 4, 4);
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
