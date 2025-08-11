using System.Configuration;
using System.Data;
using System.Windows;
using ControlzEx.Theming;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var vm = new DesktopAutomationApp.ViewModels.MainViewModel();
            var win = new DesktopAutomationApp.MainWindow { DataContext = vm };
            win.Show();
        }
    }
}
