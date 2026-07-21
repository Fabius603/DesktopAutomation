using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls.Jobs.Shared;

public partial class PercentageInput : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(PercentageInput),
        new FrameworkPropertyMetadata(
            0d,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            null,
            (_, value) => Math.Clamp((double)value, 0d, 1d)));

    public PercentageInput() => InitializeComponent();

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
