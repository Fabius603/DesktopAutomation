using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Controls.Jobs;

public partial class ValueReferencePicker : UserControl
{
    public static readonly DependencyProperty ShowClearButtonProperty = DependencyProperty.Register(
        nameof(ShowClearButton), typeof(bool), typeof(ValueReferencePicker),
        new PropertyMetadata(true));

    public ValueReferencePicker() => InitializeComponent();

    public bool ShowClearButton
    {
        get => (bool)GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ValueReferencePickerViewModel viewModel) return;
        if (e.Key == Key.Delete && viewModel.CanClear && viewModel.IsConfigured)
        {
            viewModel.ClearCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
                 && viewModel.CreateJobVariableCommand.CanExecute(null))
        {
            viewModel.CreateJobVariableCommand.Execute(null);
            e.Handled = true;
        }
    }
}
