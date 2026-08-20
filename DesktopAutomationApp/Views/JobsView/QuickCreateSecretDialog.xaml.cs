using System.Windows;
using System.Windows.Controls;
using DesktopAutomationApp.ViewModels;
using MahApps.Metro.Controls;

namespace DesktopAutomationApp.Views;

public partial class QuickCreateSecretDialog : MetroWindow
{
    public QuickCreateSecretDialog() => InitializeComponent();

    private void SecretValue_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is QuickCreateSecretViewModel viewModel && sender is PasswordBox passwordBox)
            viewModel.Value = passwordBox.Password;
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is QuickCreateSecretViewModel viewModel && await viewModel.CreateAsync())
            DialogResult = true;
    }
}
