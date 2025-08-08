
using System.Windows;
using MahApps.Metro.Controls;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow() => InitializeComponent();

        public MainWindow(MainViewModel vm) : this()
        {
            DataContext = vm;
        }
    }
}
