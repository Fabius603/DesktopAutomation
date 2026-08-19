using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Views;

public partial class CredentialsSettingsView : UserControl
{
    private CredentialsSettingsViewModel? _viewModel;

    public CredentialsSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
            PropertyChangedEventManager.RemoveHandler(_viewModel, OnViewModelPropertyChanged, string.Empty);
        _viewModel = e.NewValue as CredentialsSettingsViewModel;
        if (_viewModel is not null)
            PropertyChangedEventManager.AddHandler(_viewModel, OnViewModelPropertyChanged, string.Empty);
        SecretPasswordBox.Clear();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CredentialsSettingsViewModel.EditorSessionVersion))
            SecretPasswordBox.Clear();
    }

    private void SecretPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is CredentialsSettingsViewModel viewModel)
            viewModel.SecretValue = ((PasswordBox)sender).Password;
    }
}
