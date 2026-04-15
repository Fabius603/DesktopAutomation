
using System.ComponentModel;
using System.Windows;
using MahApps.Metro.Controls;

namespace DesktopAutomationApp
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow() => InitializeComponent();

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
