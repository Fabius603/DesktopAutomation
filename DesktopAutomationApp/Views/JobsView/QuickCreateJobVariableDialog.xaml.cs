using System.Windows;
using MahApps.Metro.Controls;

namespace DesktopAutomationApp.Views;

public partial class QuickCreateJobVariableDialog : MetroWindow
{
    public QuickCreateJobVariableDialog() => InitializeComponent();
    private void Create_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
