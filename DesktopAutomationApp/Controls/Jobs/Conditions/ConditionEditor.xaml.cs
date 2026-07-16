using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopAutomationApp.Controls.Jobs.Conditions;

public partial class ConditionEditor : UserControl
{
    public ConditionEditor() => InitializeComponent();

    private void PathSelector_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    public static readonly DependencyProperty ConditionsProperty = DependencyProperty.Register(nameof(Conditions), typeof(IEnumerable), typeof(ConditionEditor));
    public static readonly DependencyProperty IsAllProperty = DependencyProperty.Register(nameof(IsAll), typeof(bool), typeof(ConditionEditor), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty IsAnyProperty = DependencyProperty.Register(nameof(IsAny), typeof(bool), typeof(ConditionEditor), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty AddCommandProperty = DependencyProperty.Register(nameof(AddCommand), typeof(ICommand), typeof(ConditionEditor));
    public IEnumerable? Conditions { get => (IEnumerable?)GetValue(ConditionsProperty); set => SetValue(ConditionsProperty, value); }
    public bool IsAll { get => (bool)GetValue(IsAllProperty); set => SetValue(IsAllProperty, value); }
    public bool IsAny { get => (bool)GetValue(IsAnyProperty); set => SetValue(IsAnyProperty, value); }
    public ICommand? AddCommand { get => (ICommand?)GetValue(AddCommandProperty); set => SetValue(AddCommandProperty, value); }
}
