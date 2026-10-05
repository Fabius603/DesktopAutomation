using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls;

/// <summary>
/// Arranges fields in as many equally sized columns as the available width can support and
/// automatically moves remaining fields to following rows when the host becomes narrow.
/// </summary>
public class ResponsiveColumnsPanel : Panel
{
    public static readonly DependencyProperty KeepAllColumnsTogetherProperty = DependencyProperty.Register(
        nameof(KeepAllColumnsTogether), typeof(bool), typeof(ResponsiveColumnsPanel),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public bool KeepAllColumnsTogether
    {
        get => (bool)GetValue(KeepAllColumnsTogetherProperty);
        set => SetValue(KeepAllColumnsTogetherProperty, value);
    }

    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.Register(
        nameof(MinItemWidth), typeof(double), typeof(ResponsiveColumnsPanel),
        new FrameworkPropertyMetadata(170d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MaxColumnsProperty = DependencyProperty.Register(
        nameof(MaxColumns), typeof(int), typeof(ResponsiveColumnsPanel),
        new FrameworkPropertyMetadata(2, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(
        nameof(HorizontalSpacing), typeof(double), typeof(ResponsiveColumnsPanel),
        new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(
        nameof(VerticalSpacing), typeof(double), typeof(ResponsiveColumnsPanel),
        new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public int MaxColumns
    {
        get => (int)GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
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
        var children = VisibleChildren();
        if (children.Count == 0) return new Size();

        var width = double.IsInfinity(availableSize.Width)
            ? Math.Min(children.Count, Math.Max(1, MaxColumns)) * Math.Max(0, MinItemWidth)
              + Math.Max(0, Math.Min(children.Count, Math.Max(1, MaxColumns)) - 1) * Math.Max(0, HorizontalSpacing)
            : Math.Max(0, availableSize.Width);
        var columns = GetColumnCount(width, children.Count);
        var itemWidth = CalculateItemWidth(width, columns, HorizontalSpacing);
        var rowHeights = new double[(children.Count + columns - 1) / columns];

        for (var index = 0; index < children.Count; index++)
        {
            children[index].Measure(new Size(itemWidth, double.PositiveInfinity));
            rowHeights[index / columns] = Math.Max(rowHeights[index / columns], children[index].DesiredSize.Height);
        }

        return new Size(width, rowHeights.Sum() + Math.Max(0, rowHeights.Length - 1) * Math.Max(0, VerticalSpacing));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = VisibleChildren();
        if (children.Count == 0) return finalSize;

        var columns = GetColumnCount(finalSize.Width, children.Count);
        var itemWidth = CalculateItemWidth(finalSize.Width, columns, HorizontalSpacing);
        var rowHeights = new double[(children.Count + columns - 1) / columns];
        for (var index = 0; index < children.Count; index++)
            rowHeights[index / columns] = Math.Max(rowHeights[index / columns], children[index].DesiredSize.Height);

        var y = 0d;
        for (var row = 0; row < rowHeights.Length; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var index = row * columns + column;
                if (index >= children.Count) break;
                children[index].Arrange(new Rect(
                    column * (itemWidth + Math.Max(0, HorizontalSpacing)), y, itemWidth, rowHeights[row]));
            }

            y += rowHeights[row] + Math.Max(0, VerticalSpacing);
        }

        return finalSize;
    }

    internal static int CalculateColumnCount(
        double width,
        int itemCount,
        double minItemWidth,
        int maxColumns,
        double spacing)
    {
        if (itemCount <= 0) return 0;

        var safeMinWidth = Math.Max(1, minItemWidth);
        var safeSpacing = Math.Max(0, spacing);
        var supportedColumns = (int)Math.Floor((Math.Max(0, width) + safeSpacing) / (safeMinWidth + safeSpacing));
        return Math.Max(1, Math.Min(itemCount, Math.Min(Math.Max(1, maxColumns), supportedColumns)));
    }

    private int GetColumnCount(double width, int itemCount)
    {
        var columns = CalculateColumnCount(width, itemCount, MinItemWidth, MaxColumns, HorizontalSpacing);
        return KeepAllColumnsTogether && columns < itemCount ? 1 : columns;
    }

    private static double CalculateItemWidth(double width, int columns, double spacing) =>
        Math.Max(0, (Math.Max(0, width) - Math.Max(0, columns - 1) * Math.Max(0, spacing)) / Math.Max(1, columns));

    private List<UIElement> VisibleChildren() =>
        InternalChildren.Cast<UIElement>().Where(child => child.Visibility != Visibility.Collapsed).ToList();
}
