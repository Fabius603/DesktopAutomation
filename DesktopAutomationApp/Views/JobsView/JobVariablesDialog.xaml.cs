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

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }

    private void VariableList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox || e.OriginalSource is not DependencyObject source) return;
        if (ItemsControl.ContainerFromElement(listBox, source) is ListBoxItem item)
        {
            item.IsSelected = true;
            item.Focus();
            return;
        }

        e.Handled = true;
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
