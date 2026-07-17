using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopAutomationApp.Controls.Jobs.Roi;

public partial class RoiEditor : UserControl
{
    public RoiEditor() => InitializeComponent();

    public static readonly DependencyProperty IsRoiEnabledProperty = DependencyProperty.Register(
        nameof(IsRoiEnabled), typeof(bool), typeof(RoiEditor),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty XProperty = RegisterInt(nameof(X));
    public static readonly DependencyProperty YProperty = RegisterInt(nameof(Y));
    public static readonly DependencyProperty RoiWidthProperty = RegisterInt(nameof(RoiWidth));
    public static readonly DependencyProperty RoiHeightProperty = RegisterInt(nameof(RoiHeight));
    public static readonly DependencyProperty CaptureCommandProperty = DependencyProperty.Register(
        nameof(CaptureCommand), typeof(ICommand), typeof(RoiEditor));

    public bool IsRoiEnabled { get => (bool)GetValue(IsRoiEnabledProperty); set => SetValue(IsRoiEnabledProperty, value); }
    public int X { get => (int)GetValue(XProperty); set => SetValue(XProperty, value); }
    public int Y { get => (int)GetValue(YProperty); set => SetValue(YProperty, value); }
    public int RoiWidth { get => (int)GetValue(RoiWidthProperty); set => SetValue(RoiWidthProperty, value); }
    public int RoiHeight { get => (int)GetValue(RoiHeightProperty); set => SetValue(RoiHeightProperty, value); }
    public ICommand? CaptureCommand { get => (ICommand?)GetValue(CaptureCommandProperty); set => SetValue(CaptureCommandProperty, value); }

    private static DependencyProperty RegisterInt(string name) => DependencyProperty.Register(
        name, typeof(int), typeof(RoiEditor),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
}
