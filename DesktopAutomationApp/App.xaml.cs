using System.Configuration;
using System.Data;
using System.Windows;
using ControlzEx.Theming;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var vm = new ViewModels.MainViewModel();
            var win = new MainWindow { DataContext = vm };
            MainWindow = win;
            win.Show();
        }
    }
}
