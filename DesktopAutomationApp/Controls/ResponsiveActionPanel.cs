using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls;

/// <summary>
/// Keeps a primary value beside a compact secondary action while enough width is available and
/// moves the action below the value before either child is compressed beyond its usable width.
/// </summary>
public class ResponsiveActionPanel : Panel
{
    public static readonly DependencyProperty MinPrimaryWidthProperty = DependencyProperty.Register(
        nameof(MinPrimaryWidth), typeof(double), typeof(ResponsiveActionPanel),
        new FrameworkPropertyMetadata(160d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(
        nameof(HorizontalSpacing), typeof(double), typeof(ResponsiveActionPanel),
        new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(
        nameof(VerticalSpacing), typeof(double), typeof(ResponsiveActionPanel),
        new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double MinPrimaryWidth
    {
        get => (double)GetValue(MinPrimaryWidthProperty);
        set => SetValue(MinPrimaryWidthProperty, value);
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
        if (children.Count == 1)
        {
            children[0].Measure(availableSize);
            return children[0].DesiredSize;
        }

        var width = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : Math.Max(0, availableSize.Width);
        var secondary = children[1];
        secondary.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var stacks = ShouldStack(width, MinPrimaryWidth, secondary.DesiredSize.Width, HorizontalSpacing);

        if (stacks)
        {
            children[0].Measure(new Size(width, double.PositiveInfinity));
            secondary.Measure(new Size(width, double.PositiveInfinity));
            return new Size(
                double.IsInfinity(width)
                    ? Math.Max(children[0].DesiredSize.Width, secondary.DesiredSize.Width)
                    : width,
                children[0].DesiredSize.Height + Math.Max(0, VerticalSpacing) + secondary.DesiredSize.Height);
        }

        var primaryWidth = double.IsInfinity(width)
            ? double.PositiveInfinity
            : Math.Max(0, width - secondary.DesiredSize.Width - Math.Max(0, HorizontalSpacing));
        children[0].Measure(new Size(primaryWidth, double.PositiveInfinity));
        return new Size(
            double.IsInfinity(width)
                ? children[0].DesiredSize.Width + Math.Max(0, HorizontalSpacing) + secondary.DesiredSize.Width
                : width,
            Math.Max(children[0].DesiredSize.Height, secondary.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = VisibleChildren();
        if (children.Count == 0) return finalSize;
        if (children.Count == 1)
        {
            children[0].Arrange(new Rect(finalSize));
            return finalSize;
        }

        var primary = children[0];
        var secondary = children[1];
        var stacks = ShouldStack(finalSize.Width, MinPrimaryWidth, secondary.DesiredSize.Width, HorizontalSpacing);
        if (stacks)
        {
            primary.Arrange(new Rect(0, 0, finalSize.Width, primary.DesiredSize.Height));
            secondary.Arrange(new Rect(
                0,
                primary.DesiredSize.Height + Math.Max(0, VerticalSpacing),
                finalSize.Width,
                secondary.DesiredSize.Height));
            return finalSize;
        }

        var secondaryWidth = Math.Min(finalSize.Width, secondary.DesiredSize.Width);
        var primaryWidth = Math.Max(0, finalSize.Width - secondaryWidth - Math.Max(0, HorizontalSpacing));
        primary.Arrange(new Rect(0, 0, primaryWidth, finalSize.Height));
        secondary.Arrange(new Rect(
            primaryWidth + Math.Max(0, HorizontalSpacing),
            0,
            secondaryWidth,
            finalSize.Height));
        return finalSize;
    }

    internal static bool ShouldStack(
        double availableWidth,
        double minPrimaryWidth,
        double secondaryWidth,
        double spacing) =>
        !double.IsInfinity(availableWidth)
        && Math.Max(0, availableWidth)
           < Math.Max(1, minPrimaryWidth) + Math.Max(0, spacing) + Math.Max(0, secondaryWidth);

    private List<UIElement> VisibleChildren() =>
        InternalChildren.Cast<UIElement>().Where(child => child.Visibility != Visibility.Collapsed).Take(2).ToList();
}
