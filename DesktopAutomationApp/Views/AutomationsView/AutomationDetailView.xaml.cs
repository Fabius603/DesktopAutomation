using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Input;
using System.Windows.Input;

namespace DesktopAutomationApp.Views
{
    public partial class AutomationDetailView : UserControl
    {
        public AutomationDetailView()
        {
            InitializeComponent();
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || DataContext is not AutomationDetailViewModel viewModel) return;

            if (ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Save, viewModel.SaveCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Back, viewModel.BackCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Rename, viewModel.RenameCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.OpenFile, viewModel.OpenFileCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Execute, viewModel.TriggerCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Undo, viewModel.UndoCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Redo, viewModel.RedoCommand)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.RedoAlternate, viewModel.RedoCommand))
                return;

            ViewShortcutRouter.TryExecute(
                e,
                AppShortcutGestures.CaptureAutomationHotkey,
                viewModel.CaptureHotkeyCommand);
        }

        private void Picker_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (sender is not Control picker) return;
            var text = FindVisualChild<TextBox>(picker)?.Text?.Trim() ?? string.Empty;
            var parts = (picker.Tag?.ToString() ?? string.Empty).Split('|');
            var inputId = parts[0];
            var required = parts.Length > 1 && parts[1] == "Required";
            var isValid = (!required && string.IsNullOrEmpty(text))
                          || DateTime.TryParse(text, Localization.LocalizationService.Instance.CurrentCulture,
                              DateTimeStyles.AllowWhiteSpaces, out _);

            if (isValid)
            {
                picker.ClearValue(Control.BorderBrushProperty);
                picker.ClearValue(Control.ToolTipProperty);
            }
            else
            {
                picker.BorderBrush = (Brush)Application.Current.FindResource("App.Brush.Danger");
                picker.ToolTip = Localization.Loc.Get("Validation.DateTimeFormat");
            }

            (DataContext as AutomationDetailViewModel)?.ReportDateTimeInputValidity(inputId, isValid);
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T match) return match;
                var nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
