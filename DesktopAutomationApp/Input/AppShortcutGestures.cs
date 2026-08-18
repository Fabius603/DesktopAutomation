using System.Windows.Input;

namespace DesktopAutomationApp.Input;

/// <summary>
/// Central source of truth for view-local keyboard shortcuts. Global runtime
/// hotkeys (macro recording and force stop) deliberately do not belong here.
/// </summary>
public static class AppShortcutGestures
{
    public static KeyGesture FocusSearch { get; } = new(Key.F, ModifierKeys.Control);
    public static KeyGesture NewItem { get; } = new(Key.N, ModifierKeys.Control);
    public static KeyGesture NewFolder { get; } = new(Key.N, ModifierKeys.Control | ModifierKeys.Shift);
    public static KeyGesture Open { get; } = new(Key.Enter);
    public static KeyGesture Rename { get; } = new(Key.F2);
    public static KeyGesture Delete { get; } = new(Key.Delete);
    public static KeyGesture Copy { get; } = new(Key.C, ModifierKeys.Control);
    public static KeyGesture Paste { get; } = new(Key.V, ModifierKeys.Control);
    public static KeyGesture Undo { get; } = new(Key.Z, ModifierKeys.Control);
    public static KeyGesture Redo { get; } = new(Key.Y, ModifierKeys.Control);
    public static KeyGesture RedoAlternate { get; } = new(Key.Z, ModifierKeys.Control | ModifierKeys.Shift);
    public static KeyGesture Execute { get; } = new(Key.F5);
    public static KeyGesture Stop { get; } = new(Key.F5, ModifierKeys.Shift);
    public static KeyGesture Back { get; } = new(Key.Left, ModifierKeys.Alt);
    public static KeyGesture OpenFile { get; } = new(Key.O, ModifierKeys.Control);
    public static KeyGesture Save { get; } = new(Key.S, ModifierKeys.Control);
    public static KeyGesture AddStep { get; } = new(Key.Insert);
    public static KeyGesture EditStep { get; } = new(Key.Enter);
    public static KeyGesture DuplicateStep { get; } = new(Key.D, ModifierKeys.Control);
    public static KeyGesture MoveUp { get; } = new(Key.Up, ModifierKeys.Alt);
    public static KeyGesture MoveDown { get; } = new(Key.Down, ModifierKeys.Alt);
    public static KeyGesture CreateGroup { get; } = new(Key.G, ModifierKeys.Control);
    public static KeyGesture RemoveFromGroup { get; } = new(Key.G, ModifierKeys.Control | ModifierKeys.Shift);
    public static KeyGesture DebugJob { get; } = new(Key.F6);
    public static KeyGesture DebugStep { get; } = new(Key.F7);
    public static KeyGesture DebugContinue { get; } = new(Key.F8);
    public static KeyGesture ToggleBreakpoint { get; } = new(Key.B, ModifierKeys.Control);
    public static KeyGesture ToggleEnabled { get; } = new(Key.Space);
    public static KeyGesture PreviewMacro { get; } = new(Key.P, ModifierKeys.Control);
    public static KeyGesture CaptureAutomationHotkey { get; } = new(Key.H, ModifierKeys.Control | ModifierKeys.Shift);
    public static KeyGesture NavigateJobs { get; } = new(Key.D1, ModifierKeys.Control);
    public static KeyGesture NavigateMacros { get; } = new(Key.D2, ModifierKeys.Control);
    public static KeyGesture NavigateAutomations { get; } = new(Key.D3, ModifierKeys.Control);
    public static KeyGesture ShowHelp { get; } = new(Key.F1);
    public static KeyGesture ShowHelpQuestion { get; } = new(Key.OemQuestion, ModifierKeys.Control);
    public static KeyGesture ShowHelpQuestionAlternate { get; } = new(Key.OemQuestion, ModifierKeys.Control | ModifierKeys.Shift);

    public static bool Matches(KeyEventArgs args, KeyGesture gesture)
        => Matches(args.Key, args.SystemKey, Keyboard.Modifiers, gesture);

    public static bool Matches(Key key, Key systemKey, ModifierKeys modifiers, KeyGesture gesture)
        // WPF reports Alt combinations as Key.System. Comparing Key directly
        // makes every Alt-based shortcut appear dead (for example Alt+Left and
        // Alt+Up/Down).
        => (key == Key.System ? systemKey : key) == gesture.Key
           && modifiers == gesture.Modifiers;
}
