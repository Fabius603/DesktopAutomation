using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls.Jobs;

public partial class ResultBindingPicker : UserControl
{
    public ResultBindingPicker() => InitializeComponent();

    private void OpenPicker_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }
}
