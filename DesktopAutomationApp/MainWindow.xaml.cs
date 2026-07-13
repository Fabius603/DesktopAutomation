
using System.ComponentModel;
using System.Windows;
using MahApps.Metro.Controls;

namespace DesktopAutomationApp
{
    public partial class MainWindow : MetroWindow
    {
        private bool _allowClose;

        public MainWindow() => InitializeComponent();

        private void RestartToUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel viewModel
                || !viewModel.PrepareUpdateAndRestart())
                return;

            _allowClose = true;
            Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_allowClose) return;
            e.Cancel = true;
            Hide();
        }
    }
}
