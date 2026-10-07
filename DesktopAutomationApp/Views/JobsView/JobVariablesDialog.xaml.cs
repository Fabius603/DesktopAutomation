using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.ComponentModel;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Views;

public partial class JobVariablesDialog : MetroWindow
{
    private bool _closeAccepted;

    public JobVariablesDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => CenterOnOwnerOnce();
    }

    private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        VariableSearchBox.Focus();
        VariableSearchBox.SelectAll();
        e.Handled = true;
    }

    private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (VariableList.IsKeyboardFocusWithin && Keyboard.FocusedElement is not TextBoxBase
            && DataContext is JobStepsViewModel selectionOwner)
        {
            var targets = VariableList.SelectedItems.OfType<JobVariableEditorViewModel>().ToArray();
            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (targets.All(item => selectionOwner.DuplicateVariableCommand.CanExecute(item))) selectionOwner.DuplicateVariableSelection(targets);
                e.Handled = true; return;
            }
            if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
            {
                new AsyncRelayCommand(() => selectionOwner.DeleteVariableSelectionAsync(targets)).Execute(null);
                e.Handled = true; return;
            }
        }
        if (e.Key != Key.Delete || Keyboard.Modifiers != ModifierKeys.None
            || Keyboard.FocusedElement is TextBoxBase or ComboBox
            || DataContext is not JobStepsViewModel viewModel
            || viewModel.SelectedJobVariable is not { } variable
            || !viewModel.DeleteVariableCommand.CanExecute(variable))
            return;

        viewModel.DeleteVariableCommand.Execute(variable);
        e.Handled = true;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_closeAccepted || DataContext is not JobStepsViewModel viewModel) return;
        if (!viewModel.TryCloseVariableDraftSession())
        {
            e.Cancel = true;
            return;
        }

        _closeAccepted = true;
    }

    private void CenterOnOwnerOnce()
    {
        if (Owner == null) return;
        UpdateLayout();
        Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
        Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2;
    }
}
