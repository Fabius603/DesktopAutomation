using System.Windows;
using System.Windows.Input;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.Behaviors;

/// <summary>
/// Keeps global safety/recording hotkeys from also invoking a view-local shortcut.
/// The low-level hotkey service remains responsible for the global action.
/// </summary>
public static class ViewShortcutGuard
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(ViewShortcutGuard), new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        if (e.NewValue is true) element.PreviewKeyDown += OnPreviewKeyDown;
        else element.PreviewKeyDown -= OnPreviewKeyDown;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsReserved(e, (sender as FrameworkElement)?.DataContext))
            e.Handled = true;
    }

    public static bool IsReserved(KeyEventArgs e, object? activeContent)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var virtualKey = unchecked((uint)KeyInterop.VirtualKeyFromKey(key));
        if (Keyboard.Modifiers == ModifierKeys.None && ForceStopKeyConfiguration.Matches(virtualKey))
            return true;

        if (activeContent is MakroStepsViewModel viewModel
            && viewModel.Makro.RecordingSettings.RecordingHotkeyVirtualKey == virtualKey
            && ToHotkeyModifiers(Keyboard.Modifiers) == viewModel.Makro.RecordingSettings.RecordingHotkeyModifiers)
            return true;

        return false;
    }

    private static KeyModifiers ToHotkeyModifiers(ModifierKeys modifiers)
    {
        var result = KeyModifiers.None;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= KeyModifiers.Control;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= KeyModifiers.Alt;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= KeyModifiers.Shift;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= KeyModifiers.Windows;
        return result;
    }
}
