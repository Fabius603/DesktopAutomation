using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation;
using System.Windows.Media;
using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;
using DesktopAutomationApp.Services.Jobs;
using DesktopAutomationApp.ViewModels;
using System.Collections;

namespace DesktopAutomationApp.Controls;

/// <summary>
/// Draws Scratch-style open C-shaped control-flow containers behind the flat job-step list.
/// The persisted list stays unchanged; only its shared structural projection controls rendering.
/// </summary>
public sealed class ControlFlowBlockPanel : VirtualizingStackPanel
{
    public static readonly DependencyProperty BlockBackgroundProperty = DependencyProperty.Register(
        nameof(BlockBackground), typeof(Brush), typeof(ControlFlowBlockPanel),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BlockBorderBrushProperty = DependencyProperty.Register(
        nameof(BlockBorderBrush), typeof(Brush), typeof(ControlFlowBlockPanel),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BlockHoverBackgroundProperty = DependencyProperty.Register(
        nameof(BlockHoverBackground), typeof(Brush), typeof(ControlFlowBlockPanel),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BlockHoverBorderBrushProperty = DependencyProperty.Register(
        nameof(BlockHoverBorderBrush), typeof(Brush), typeof(ControlFlowBlockPanel),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PlaceholderBorderBrushProperty = DependencyProperty.Register(
        nameof(PlaceholderBorderBrush), typeof(Brush), typeof(ControlFlowBlockPanel),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PlaceholderForegroundProperty = DependencyProperty.Register(
        nameof(PlaceholderForeground), typeof(Brush), typeof(ControlFlowBlockPanel),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    private LayoutState? _layoutState;

    public ControlFlowBlockPanel()
    {
        LayoutUpdated += OnLayoutUpdated;
        Loaded += (_, _) => { LayoutUpdated -= OnLayoutUpdated; LayoutUpdated += OnLayoutUpdated; };
        Unloaded += (_, _) => { LayoutUpdated -= OnLayoutUpdated; _layoutState = null; };
    }

    // The panel can render before its generated containers finish arranging. Observe the
    // measured positions, not mouse input, and invalidate only when those positions change.
    private void OnLayoutUpdated(object? sender, EventArgs args)
    {
        var owner = ItemsControl.GetItemsOwner(this);
        if (owner is null) return;
        var model = owner.DataContext as JobStepsViewModel;
        var rows = InternalChildren.OfType<FrameworkElement>().Select(item => new RowLayout(
            item.DataContext, item.Visibility, item.TranslatePoint(new Point(), this), item.RenderSize,
            GetElementRect(FindIcon(item)), GetElementRect((item as Control)?.Template?.FindName("Card", item) as FrameworkElement))).ToArray();
        var next = new LayoutState(owner.ItemsSource, model?.StepsVersion ?? 0, owner.ActualWidth, rows);
        if (_layoutState is { } previous && previous.Source == next.Source && previous.Version == next.Version
            && previous.Width == next.Width && previous.Rows.SequenceEqual(next.Rows)) return;
        _layoutState = next;
        InvalidateVisual();
    }

    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        base.OnItemsChanged(sender, args);
        _layoutState = null;
        InvalidateVisual();
    }

    public Brush BlockBackground
    {
        get => (Brush)GetValue(BlockBackgroundProperty);
        set => SetValue(BlockBackgroundProperty, value);
    }

    public Brush BlockBorderBrush
    {
        get => (Brush)GetValue(BlockBorderBrushProperty);
        set => SetValue(BlockBorderBrushProperty, value);
    }

    public Brush BlockHoverBackground
    {
        get => (Brush)GetValue(BlockHoverBackgroundProperty);
        set => SetValue(BlockHoverBackgroundProperty, value);
    }

    public Brush BlockHoverBorderBrush
    {
        get => (Brush)GetValue(BlockHoverBorderBrushProperty);
        set => SetValue(BlockHoverBorderBrushProperty, value);
    }

    public Brush PlaceholderBorderBrush
    {
        get => (Brush)GetValue(PlaceholderBorderBrushProperty);
        set => SetValue(PlaceholderBorderBrushProperty, value);
    }

    public Brush PlaceholderForeground
    {
        get => (Brush)GetValue(PlaceholderForegroundProperty);
        set => SetValue(PlaceholderForegroundProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var owner = ItemsControl.GetItemsOwner(this);
        if (owner is null) return;

        var viewModel = owner.DataContext as JobStepsViewModel;
        var items = owner.ItemsSource as IList ?? owner.Items;
        var projection = StepListProjection.Get(items, viewModel?.StepsVersion ?? 0);
        var steps = projection.Steps;
        if (steps.Length == 0) return;

        var structure = projection.Structure;

        var blocks = structure.Blocks.OrderBy(candidate => candidate.Depth)
            .Where(block => !projection.IsHidden(block.StartIndex, viewModel?.CollapsedBlockIds ?? [])).ToArray();
        // Paint all surfaces first: a nested surface must never cover an ancestor's route.
        foreach (var block in blocks) DrawBlock(drawingContext, owner, block, steps.Length, surfacesOnly: true);
        DrawSequence(drawingContext, owner, projection, Enumerable.Range(0, steps.Length)
            .Where(index => projection.Depth(index) == 0 && steps[index] is not ElseStep and not ElseIfStep and not EndIfStep));
        foreach (var block in blocks) DrawBlock(drawingContext, owner, block, steps.Length, surfacesOnly: false);
    }

    private void DrawBlock(
        DrawingContext drawingContext,
        ItemsControl owner,
        ControlFlowBlock block,
        int stepCount,
        bool surfacesOnly)
    {
        var viewModel = owner.DataContext as JobStepsViewModel;
        var collapsed = viewModel?.CollapsedBlockIds.Contains(((JobStep)owner.Items[block.StartIndex]).Id) == true;
        var start = GetItemRect(owner, block.StartIndex);
        var endIndex = block.EndIndex ?? block.LastIndex(stepCount);
        var projection = StepListProjection.Get(owner.ItemsSource as IList ?? owner.Items, viewModel?.StepsVersion ?? 0);
        var closing = collapsed ? null : GetClosurePoint(owner, projection, block);
        var end = collapsed ? start : closing is { } point ? new Rect(0, point.Y - 8, 0, 12) : null;
        var realized = Enumerable.Range(block.StartIndex, endIndex - block.StartIndex + 1)
            .Select(index => GetItemRect(owner, index)).Where(rect => rect is { Height: > 0 }).ToArray();
        if (realized.Length == 0) return;
        // Keep ancestor rails visible when their headers are outside the realized viewport.
        var clippedStart = start is null;
        var clippedEnd = end is null || block.EndIndex is null;
        start ??= realized[0];
        end ??= realized[^1];

        var left = StepListProjection.GutterWidth + block.Depth * StepListProjection.Indentation;
        var width = projection.Width(block.Depth, owner.ActualWidth);
        var first = start.GetValueOrDefault();
        var last = end.GetValueOrDefault();
        var frame = new Rect(left, first.Top, width, Math.Max(64, last.Bottom - first.Top - 4));
        if (surfacesOnly)
        {
            DrawLayer(drawingContext, frame, block.Depth, !clippedStart && !clippedEnd);
            return;
        }
        if (collapsed) return;
        foreach (var section in block.Sections)
        {
            if (viewModel?.CollapsedBlockIds.Contains(section.Marker.Id) == true) continue;
            var next = block.SectionEndExclusive(section.MarkerIndex, stepCount) ?? endIndex;
            var header = GetCardRect(owner, section.MarkerIndex);
            var brush = TryFindResource(section.Marker is ElseStep ? "StepList.Rail.Else" : "StepList.Rail.If") as Brush ?? BlockBorderBrush;
            var children = Enumerable.Range(section.MarkerIndex + 1, next - section.MarkerIndex - 1)
                .Where(index => projection.Depth(index) == block.Depth + 1
                    && projection.Steps[index] is not ElseStep and not ElseIfStep and not EndIfStep).ToArray();
            // A branch is a scope, not a step followed by the alternative branch. The rail
            // groups its contents; directional arrows exist only inside this sequence.
            var branchExit = DrawSequence(drawingContext, owner, projection, children, brush);
            var childRows = children.Select(index => GetCardRect(owner, index)).Where(rect => rect is { Height: > 0 }).ToArray();
            var top = header?.Bottom ?? childRows.FirstOrDefault()?.Top;
            var bottom = childRows.Length > 0 ? Math.Max(childRows[^1]!.Value.Bottom, branchExit?.Y ?? 0) : GetEmptyBranchRect(owner, section.MarkerIndex)?.Bottom ?? header?.Bottom;
            if (top is { } railTop && bottom is { } railBottom)
            {
                var lane = left + 14;
                var cap = left + StepListProjection.Indentation - 6;
                DrawScope(drawingContext, brush, lane, cap, railTop + 2, Math.Max(railTop + 10, railBottom + 4));
            }
        }

    }

    private Point? DrawSequence(DrawingContext context, ItemsControl owner, StepListProjection projection,
        IEnumerable<int> indices, Brush? routeBrush = null, Point? previous = null)
    {
        var brush = routeBrush ?? TryFindResource("App.Brush.Accent") as Brush ?? BlockBorderBrush;
        foreach (var index in indices)
        {
            if (GetCardRect(owner, index) is not { Height: > 0 } row || GetIconRect(owner, index) is not { } icon) continue;
            var conditional = projection.Structure.GetBlockStartingAt(index);
            var entry = new Point(icon.Left + icon.Width / 2, row.Top - 2);
            if (previous is { } exit && entry.Y >= exit.Y) DrawConnection(context, brush, exit, entry);
            previous = conditional is not null && GetClosurePoint(owner, projection, conditional) is { } closure
                ? closure : new Point(entry.X, row.Bottom + 2);
        }
        return previous;
    }

    private Point? GetClosurePoint(ItemsControl owner, StepListProjection projection, ControlFlowBlock block)
    {
        if (block.EndIndex is not int end) return null;
        var collapsed = (owner.DataContext as JobStepsViewModel)?.CollapsedBlockIds ?? [];
        if (collapsed.Contains(projection.Steps[block.StartIndex].Id)) return null;
        for (var index = end - 1; index >= block.StartIndex; index--)
        {
            if (projection.Steps[index] is EndIfStep || projection.IsHidden(index, collapsed)) continue;
            if (GetCardRect(owner, index) is not { } row) return null;
            var closures = projection.ClosuresAfter(index, collapsed);
            var order = closures.ToList().FindIndex(candidate => candidate.StartIndex == block.StartIndex);
            var icon = GetIconRect(owner, block.StartIndex);
            var exitX = icon is { } measured ? measured.Left + measured.Width / 2
                : StepListProjection.GutterWidth + block.Depth * StepListProjection.Indentation + 57;
            return order < 0 ? null : new Point(exitX, (GetEmptyBranchRect(owner, index)?.Bottom ?? row.Bottom) + 12 + order * 24);
        }
        return null;
    }

    private static void DrawConnection(DrawingContext drawing, Brush brush, Point from, Point to)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            var middle = (from.Y + to.Y) / 2;
            context.BeginFigure(from, false, false);
            if (Math.Abs(from.X - to.X) < 0.5)
                context.LineTo(to, true, false);
            else
            {
                // Rounded right-angle elbow: direction remains visibly downward at both ports.
                var radius = Math.Min(4, Math.Min(Math.Abs(to.X - from.X) / 2, Math.Max(0, (to.Y - from.Y) / 4)));
                var direction = Math.Sign(to.X - from.X);
                context.LineTo(new Point(from.X, middle - radius), true, false);
                context.QuadraticBezierTo(new Point(from.X, middle), new Point(from.X + direction * radius, middle), true, false);
                context.LineTo(new Point(to.X - direction * radius, middle), true, false);
                context.QuadraticBezierTo(new Point(to.X, middle), new Point(to.X, middle + radius), true, false);
                context.LineTo(to, true, false);
            }
        }
        drawing.DrawGeometry(null, new Pen(brush, 1.75) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, geometry);
        var arrow = new StreamGeometry();
        using (var context = arrow.Open())
        {
            context.BeginFigure(to, true, true);
            context.LineTo(new Point(to.X - 3, to.Y - 4), true, false);
            context.LineTo(new Point(to.X + 3, to.Y - 4), true, false);
        }
        drawing.DrawGeometry(brush, null, arrow);
    }

    private static void DrawScope(DrawingContext drawing, Brush brush, double lane, double cap, double top, double bottom)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(cap, top), false, false);
            context.LineTo(new Point(lane + 4, top), true, false);
            context.QuadraticBezierTo(new Point(lane, top), new Point(lane, top + 4), true, false);
            context.LineTo(new Point(lane, bottom - 4), true, false);
            context.QuadraticBezierTo(new Point(lane, bottom), new Point(lane + 4, bottom), true, false);
            context.LineTo(new Point(cap, bottom), true, false);
        }
        var muted = brush.CloneCurrentValue(); muted.Opacity = 0.75;
        drawing.DrawGeometry(null, new Pen(muted, 1.5), geometry);
    }

    private void DrawLayer(DrawingContext context, Rect frame, int depth, bool complete)
    {
        var baseColor = (BlockBackground as SolidColorBrush)?.Color ?? Color.FromRgb(24, 29, 35);
        var light = baseColor.R + baseColor.G + baseColor.B > 450;
        Color Mix(double ratio) => Color.FromRgb((byte)(baseColor.R + (255 - baseColor.R) * ratio),
            (byte)(baseColor.G + (255 - baseColor.G) * ratio), (byte)(baseColor.B + (255 - baseColor.B) * ratio));
        var elevation = light ? Math.Min(0.75, depth * 0.18) : Math.Min(0.24, depth * 0.055);
        var fill = new LinearGradientBrush(Mix(elevation + 0.025), Mix(elevation), 90);
        if (complete)
        {
            var shadow = frame; shadow.Offset(3, 5);
            context.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(light ? (byte)30 : (byte)85, 0, 0, 0)), null, shadow, 9, 9);
            context.DrawRoundedRectangle(fill, new Pen(BlockBorderBrush, 1), frame, 8, 8);
            var highlight = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
            context.DrawLine(new Pen(highlight, 1), new Point(frame.Left + 8, frame.Top + 1), new Point(frame.Right - 8, frame.Top + 1));
        }
        else
        {
            // An incomplete block has an open surface; never paint a fabricated bottom closure.
            context.DrawRectangle(fill, null, frame);
            context.DrawLine(new Pen(BlockBorderBrush, 1), frame.TopLeft, frame.BottomLeft);
        }
    }

    private Rect? GetItemRect(ItemsControl owner, int index)
    {
        if (owner.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement { Visibility: Visibility.Visible } item)
            return null;

        var topLeft = item.TranslatePoint(new Point(0, 0), this);
        return new Rect(topLeft, item.RenderSize);
    }

    private Rect? GetCardRect(ItemsControl owner, int index)
    {
        if (owner.ItemContainerGenerator.ContainerFromIndex(index) is not Control { Visibility: Visibility.Visible } item) return null;
        return GetElementRect(item.Template?.FindName("Card", item) as FrameworkElement);
    }

    private Rect? GetEmptyBranchRect(ItemsControl owner, int index) => GetElementRect(
        owner.ItemContainerGenerator.ContainerFromIndex(index) is Control item
            ? item.Template?.FindName("EmptyBranchHint", item) as FrameworkElement : null);

    private Rect? GetIconRect(ItemsControl owner, int index) => GetElementRect(
        owner.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement item ? FindIcon(item) : null);

    private Rect? GetElementRect(FrameworkElement? element) => element is { Visibility: Visibility.Visible, ActualHeight: > 0 }
        ? new Rect(element.TranslatePoint(new Point(), this), element.RenderSize) : null;

    private static FrameworkElement? FindIcon(DependencyObject item)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(item); index++)
        {
            var child = VisualTreeHelper.GetChild(item, index);
            if (child is FrameworkElement element && AutomationProperties.GetAutomationId(element) == "JobStepDragHandle") return element;
            if (FindIcon(child) is { } icon) return icon;
        }
        return null;
    }

    private sealed record RowLayout(object Data, Visibility Visibility, Point Position, Size Size, Rect? Icon, Rect? Card);
    private sealed record LayoutState(object? Source, int Version, double Width, RowLayout[] Rows);
}
