using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls.Jobs.Geometry;

public sealed class ResponsiveGeometryPanel : Panel
{
    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.Register(
        nameof(MinItemWidth), typeof(double), typeof(ResponsiveGeometryPanel),
        new FrameworkPropertyMetadata(170d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(
        nameof(HorizontalSpacing), typeof(double), typeof(ResponsiveGeometryPanel),
        new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(
        nameof(VerticalSpacing), typeof(double), typeof(ResponsiveGeometryPanel),
        new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0) return new Size();

        var width = double.IsInfinity(availableSize.Width)
            ? Math.Min(InternalChildren.Count, 2) * MinItemWidth
              + Math.Max(0, Math.Min(InternalChildren.Count, 2) - 1) * HorizontalSpacing
            : availableSize.Width;
        var columns = ColumnCount(width);
        var itemWidth = ItemWidth(width, columns);
        var rowHeights = new double[(InternalChildren.Count + columns - 1) / columns];

        for (var index = 0; index < InternalChildren.Count; index++)
        {
            var child = InternalChildren[index];
            child.Measure(new Size(itemWidth, double.PositiveInfinity));
            rowHeights[index / columns] = Math.Max(rowHeights[index / columns], child.DesiredSize.Height);
        }

        return new Size(width, rowHeights.Sum() + Math.Max(0, rowHeights.Length - 1) * VerticalSpacing);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count == 0) return finalSize;

        var columns = ColumnCount(finalSize.Width);
        var itemWidth = ItemWidth(finalSize.Width, columns);
        var rowHeights = new double[(InternalChildren.Count + columns - 1) / columns];
        for (var index = 0; index < InternalChildren.Count; index++)
            rowHeights[index / columns] = Math.Max(rowHeights[index / columns], InternalChildren[index].DesiredSize.Height);

        var y = 0d;
        for (var row = 0; row < rowHeights.Length; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var index = row * columns + column;
                if (index >= InternalChildren.Count) break;
                InternalChildren[index].Arrange(new Rect(
                    column * (itemWidth + HorizontalSpacing), y, itemWidth, rowHeights[row]));
            }

            y += rowHeights[row] + VerticalSpacing;
        }

        return finalSize;
    }

    private int ColumnCount(double width) =>
        InternalChildren.Count > 1 && width >= MinItemWidth * 2 + HorizontalSpacing ? 2 : 1;

    private double ItemWidth(double width, int columns) =>
        Math.Max(0, (width - Math.Max(0, columns - 1) * HorizontalSpacing) / columns);
}
