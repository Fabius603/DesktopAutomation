using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;

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

    private readonly List<BlockHitGeometry> _blockHitGeometries = [];
    private int? _hoveredBlockStartIndex;

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

        var steps = owner.Items.Cast<object>().OfType<JobStep>().ToArray();
        if (steps.Length == 0) return;

        var structure = ControlFlowStructureAnalyzer.Analyze(steps);
        _blockHitGeometries.Clear();

        foreach (var block in structure.Blocks.OrderBy(candidate => candidate.Depth))
            DrawBlock(drawingContext, owner, structure, block, steps.Length);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var position = e.GetPosition(this);
        var hovered = _blockHitGeometries
            .Where(candidate => candidate.Geometry.FillContains(position))
            .OrderByDescending(candidate => candidate.Depth)
            .Select(candidate => (int?)candidate.StartIndex)
            .FirstOrDefault();
        SetHoveredBlock(hovered);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        SetHoveredBlock(null);
    }

    private void DrawBlock(
        DrawingContext drawingContext,
        ItemsControl owner,
        ControlFlowStructure structure,
        ControlFlowBlock block,
        int stepCount)
    {
        var start = GetItemRect(owner, block.StartIndex);
        var endIndex = block.EndIndex ?? block.LastIndex(stepCount);
        var end = GetItemRect(owner, endIndex);
        if (start is null || end is null) return;

        const double gutterWidth = 32;
        const double depthIndent = 16;
        const double railWidth = 12;
        const double baseWidth = 358;
        const double radius = 9;

        var descendantDepth = structure.Blocks
            .Where(candidate => block.Contains(candidate.StartIndex, stepCount))
            .Select(candidate => candidate.Depth)
            .DefaultIfEmpty(block.Depth)
            .Max();
        var width = baseWidth + ((descendantDepth - block.Depth) * depthIndent);
        var left = gutterWidth + (block.Depth * depthIndent);

        var sectionBars = block.Sections
            .OrderBy(section => section.MarkerIndex)
            .Select(section => (Section: section, ItemRect: GetItemRect(owner, section.MarkerIndex)))
            .Where(item => item.ItemRect is not null)
            .Select(item => (item.Section, Rect: new Rect(
                left, item.ItemRect!.Value.Top, width, item.ItemRect.Value.Height)))
            .ToArray();
        if (sectionBars.Length == 0) return;

        var footer = new Rect(left, end.Value.Top, width, end.Value.Height);
        var bars = sectionBars.Select(item => item.Rect).Append(footer).ToArray();

        var shape = CreateBlockGeometry(bars, railWidth, radius);
        _blockHitGeometries.Add(new BlockHitGeometry(block.StartIndex, block.Depth, shape));
        var hovered = _hoveredBlockStartIndex == block.StartIndex;
        var pen = new Pen(hovered ? BlockHoverBorderBrush : BlockBorderBrush, 1.5)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        drawingContext.DrawGeometry(hovered ? BlockHoverBackground : BlockBackground, pen, shape);
        DrawEmptySectionPlaceholders(drawingContext, block, sectionBars, footer, stepCount);
    }

    private void DrawEmptySectionPlaceholders(
        DrawingContext drawingContext,
        ControlFlowBlock block,
        IReadOnlyList<(ControlFlowSection Section, Rect Rect)> sectionBars,
        Rect footer,
        int stepCount)
    {
        var borderPen = new Pen(PlaceholderBorderBrush, 1)
        {
            DashStyle = DashStyles.Dash
        };
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var text = new FormattedText(
            Loc.Get("Ui.Job.Steps.EmptyBranchPlaceholder"),
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            PlaceholderForeground,
            dpi);

        for (var index = 0; index < sectionBars.Count; index++)
        {
            var current = sectionBars[index];
            if (!block.IsSectionEmpty(current.Section.MarkerIndex, stepCount)) continue;

            var nextTop = index + 1 < sectionBars.Count
                ? sectionBars[index + 1].Rect.Top
                : footer.Top;
            var availableHeight = nextTop - current.Rect.Bottom;
            if (availableHeight < 24) continue;

            var placeholder = new Rect(
                current.Rect.Left + 20,
                current.Rect.Bottom + 4,
                330,
                Math.Min(24, availableHeight - 12));
            var textLeft = placeholder.Left + Math.Max(0, (placeholder.Width - text.Width) / 2);
            var lineY = placeholder.Top + (placeholder.Height / 2);
            const double textGap = 10;
            drawingContext.DrawLine(
                borderPen,
                new Point(placeholder.Left, lineY),
                new Point(Math.Max(placeholder.Left, textLeft - textGap), lineY));
            drawingContext.DrawLine(
                borderPen,
                new Point(Math.Min(placeholder.Right, textLeft + text.Width + textGap), lineY),
                new Point(placeholder.Right, lineY));
            drawingContext.DrawText(text, new Point(
                textLeft,
                placeholder.Top + Math.Max(0, (placeholder.Height - text.Height) / 2)));
        }
    }

    private static Geometry CreateBlockGeometry(IReadOnlyList<Rect> bars, double railWidth, double radius)
    {
        var first = bars[0];
        var last = bars[^1];
        var left = first.Left;
        var inner = left + railWidth;
        var right = first.Right;
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(left + radius, first.Top), true, true);
        context.LineTo(new Point(right - radius, first.Top), true, false);
        context.QuadraticBezierTo(new Point(right, first.Top), new Point(right, first.Top + radius), true, false);

        for (var index = 0; index < bars.Count; index++)
        {
            var bar = bars[index];
            context.LineTo(new Point(right, bar.Bottom - radius), true, false);
            context.QuadraticBezierTo(new Point(right, bar.Bottom), new Point(right - radius, bar.Bottom), true, false);

            if (index == bars.Count - 1)
                break;

            context.LineTo(new Point(inner + radius, bar.Bottom), true, false);
            context.QuadraticBezierTo(new Point(inner, bar.Bottom), new Point(inner, bar.Bottom + radius), true, false);
            var next = bars[index + 1];
            context.LineTo(new Point(inner, next.Top - radius), true, false);
            context.QuadraticBezierTo(new Point(inner, next.Top), new Point(inner + radius, next.Top), true, false);
            context.LineTo(new Point(right - radius, next.Top), true, false);
            context.QuadraticBezierTo(new Point(right, next.Top), new Point(right, next.Top + radius), true, false);
        }

        context.LineTo(new Point(left + radius, last.Bottom), true, false);
        context.QuadraticBezierTo(new Point(left, last.Bottom), new Point(left, last.Bottom - radius), true, false);
        context.LineTo(new Point(left, first.Top + radius), true, false);
        context.QuadraticBezierTo(new Point(left, first.Top), new Point(left + radius, first.Top), true, false);
        geometry.Freeze();
        return geometry;
    }

    private void SetHoveredBlock(int? startIndex)
    {
        if (_hoveredBlockStartIndex == startIndex) return;
        _hoveredBlockStartIndex = startIndex;
        InvalidateVisual();
    }

    private Rect? GetItemRect(ItemsControl owner, int index)
    {
        if (owner.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement item)
            return null;

        var topLeft = item.TranslatePoint(new Point(0, 0), this);
        return new Rect(topLeft, item.RenderSize);
    }

    private sealed record BlockHitGeometry(int StartIndex, int Depth, Geometry Geometry);
}
