using System.Windows.Controls;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Controls.Jobs;

public partial class ValueReferencePicker : UserControl
{
    public ValueReferencePicker()
    {
        InitializeComponent();
        PathPicker.DropDownOpened += (_, _) =>
        {
            if (DataContext is ValueReferencePickerViewModel viewModel)
                viewModel.EnsureSelectionTree();
        };
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
