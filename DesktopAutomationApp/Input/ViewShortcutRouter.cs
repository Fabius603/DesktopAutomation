using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DesktopAutomationApp.Input;

/// <summary>
/// Routes view shortcuts during the preview phase, before nested WPF controls
/// can consume the corresponding key event.
/// </summary>
public static class ViewShortcutRouter
{
    public static bool IsTextInputFocused
        => Keyboard.FocusedElement is TextBoxBase
            or PasswordBox
            or ComboBox { IsEditable: true };

    public static bool TryExecute(
        KeyEventArgs args,
        KeyGesture gesture,
        ICommand command,
        object? parameter = null)
    {
        if (args.Handled || !AppShortcutGestures.Matches(args, gesture) || !command.CanExecute(parameter))
            return false;

        command.Execute(parameter);
        args.Handled = true;
        return true;
    }
}
