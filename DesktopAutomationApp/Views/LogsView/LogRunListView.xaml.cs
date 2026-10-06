using System.Windows.Controls;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Views;

public partial class LogRunListView : UserControl
{
    public LogRunListView() => InitializeComponent();
    private void OpenRun(object sender, MouseButtonEventArgs e) { if (e.OriginalSource is System.Windows.DependencyObject source && ItemsControl.ContainerFromElement(RunList, source) is ListBoxItem) OpenSelected(); }
    private void OnKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { OpenSelected(); e.Handled = true; } }
    private void OpenSelected() { if (DataContext is LogsHomeViewModel vm && RunList.SelectedItem is LogRunRow row) vm.OpenRunCommand.Execute(row); }
}
