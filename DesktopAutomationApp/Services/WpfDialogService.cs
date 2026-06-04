using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Views;
using System.Windows;

namespace DesktopAutomationApp.Services
{
    public sealed class WpfDialogService : IDialogService
    {
        public Task<bool> ConfirmAsync(string message, string title)
        {
            var result = AppDialog.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        public Task<bool?> ConfirmWithCancelAsync(string message, string title)
        {
            var result = AppDialog.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            bool? mapped = result switch
            {
                MessageBoxResult.Yes => true,
                MessageBoxResult.No  => false,
                _                    => null
            };
            return Task.FromResult(mapped);
        }

        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null)
        {
            var dlg = new NewItemNameDialog(title, prompt, defaultValue ?? string.Empty)
                { Owner = Application.Current?.MainWindow };
            return Task.FromResult(dlg.ShowDialog() == true ? dlg.ResultName : (string?)null);
        }

        public void ShowError(string message, string title)
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
                AppDialog.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }
}
