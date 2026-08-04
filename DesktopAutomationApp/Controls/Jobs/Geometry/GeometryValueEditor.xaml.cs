using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls.Jobs.Geometry;

public partial class GeometryValueEditor : UserControl
{
    public GeometryValueEditor() => InitializeComponent();

    public static readonly DependencyProperty XProperty = RegisterInt(nameof(X));
    public static readonly DependencyProperty YProperty = RegisterInt(nameof(Y));
    public static readonly DependencyProperty RegionWidthProperty = RegisterInt(nameof(RegionWidth));
    public static readonly DependencyProperty RegionHeightProperty = RegisterInt(nameof(RegionHeight));
    public static readonly DependencyProperty ShowSizeProperty = DependencyProperty.Register(
        nameof(ShowSize), typeof(bool), typeof(GeometryValueEditor), new PropertyMetadata(false));

    public int X { get => (int)GetValue(XProperty); set => SetValue(XProperty, value); }
    public int Y { get => (int)GetValue(YProperty); set => SetValue(YProperty, value); }
    public int RegionWidth { get => (int)GetValue(RegionWidthProperty); set => SetValue(RegionWidthProperty, value); }
    public int RegionHeight { get => (int)GetValue(RegionHeightProperty); set => SetValue(RegionHeightProperty, value); }
    public bool ShowSize { get => (bool)GetValue(ShowSizeProperty); set => SetValue(ShowSizeProperty, value); }

    private static DependencyProperty RegisterInt(string name) => DependencyProperty.Register(
        name, typeof(int), typeof(GeometryValueEditor),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
}
