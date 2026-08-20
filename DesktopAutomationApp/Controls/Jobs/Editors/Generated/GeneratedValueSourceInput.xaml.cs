using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Controls.Jobs.Editors.Generated;

public partial class GeneratedValueSourceInput : UserControl
{
    public GeneratedValueSourceInput() => InitializeComponent();

    private void OpenValueSourceMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }
}
