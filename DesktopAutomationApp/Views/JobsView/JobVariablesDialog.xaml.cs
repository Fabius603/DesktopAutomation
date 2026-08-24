using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace DesktopAutomationApp.Views;

public partial class JobVariablesDialog : MetroWindow
{
    public JobVariablesDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => CenterOnOwnerOnce();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }

    private void CenterOnOwnerOnce()
    {
        if (Owner == null) return;
        UpdateLayout();
        Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
        Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2;
    }
}
