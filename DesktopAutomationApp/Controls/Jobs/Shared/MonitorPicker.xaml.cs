using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopAutomationApp.Controls.Jobs.Shared;

public partial class MonitorPicker : UserControl
{
    public static readonly DependencyProperty MonitorIndexProperty = DependencyProperty.Register(
        nameof(MonitorIndex), typeof(int), typeof(MonitorPicker),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.Register(
        nameof(SelectCommand), typeof(ICommand), typeof(MonitorPicker));
    public static readonly DependencyProperty SelectCommandParameterProperty = DependencyProperty.Register(
        nameof(SelectCommandParameter), typeof(object), typeof(MonitorPicker));

    public MonitorPicker() => InitializeComponent();

    public int MonitorIndex { get => (int)GetValue(MonitorIndexProperty); set => SetValue(MonitorIndexProperty, value); }
    public ICommand? SelectCommand { get => (ICommand?)GetValue(SelectCommandProperty); set => SetValue(SelectCommandProperty, value); }
    public object? SelectCommandParameter { get => GetValue(SelectCommandParameterProperty); set => SetValue(SelectCommandParameterProperty, value); }
}
