using System.Windows.Controls;

namespace DesktopAutomationApp.Views;

public partial class ApplicationLogsView : UserControl
{
    public ApplicationLogsView() => InitializeComponent();
    private void PauseUpdates(object sender, System.Windows.Input.MouseEventArgs args) { if (DataContext is DesktopAutomationApp.ViewModels.LogsHomeViewModel vm) vm.Live = false; }
}
