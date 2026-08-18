using System.Reflection;
using System.Windows.Input;
using DesktopAutomationApp.Input;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class AppShortcutGesturesTests
{
    [Fact]
    public void ShortcutOverviewUsesLayoutIndependentF1Key()
    {
        Assert.Equal(Key.F1, AppShortcutGestures.ShowHelp.Key);
        Assert.Equal(ModifierKeys.None, AppShortcutGestures.ShowHelp.Modifiers);
    }

    [Fact]
    public void Matches_UsesSystemKeyForAltGestures()
    {
        Assert.True(AppShortcutGestures.Matches(
            Key.System,
            Key.Left,
            ModifierKeys.Alt,
            AppShortcutGestures.Back));
        Assert.True(AppShortcutGestures.Matches(
            Key.System,
            Key.Up,
            ModifierKeys.Alt,
            AppShortcutGestures.MoveUp));
    }

    [Fact]
    public void ViewShortcutsDoNotClaimDefaultRecordingOrForceStopKeys()
    {
        var gestures = typeof(AppShortcutGestures)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(KeyGesture))
            .Select(property => Assert.IsType<KeyGesture>(property.GetValue(null)))
            .ToArray();

        Assert.DoesNotContain(gestures, gesture => gesture.Modifiers == ModifierKeys.None && gesture.Key is Key.F9 or Key.F10);
    }

    [Fact]
    public void CommonEditorShortcutsAreUniqueWithinTheirScope()
    {
        KeyGesture[] gestures =
        [
            AppShortcutGestures.Save,
            AppShortcutGestures.NewItem,
            AppShortcutGestures.Back,
            AppShortcutGestures.Rename,
            AppShortcutGestures.OpenFile,
            AppShortcutGestures.Execute,
            AppShortcutGestures.Stop
        ];

        Assert.Equal(gestures.Length, gestures.Select(Identity).Distinct().Count());
    }

    [Fact]
    public void JobDebugShortcutsAreUniqueWithinTheirScope()
    {
        KeyGesture[] gestures =
        [
            AppShortcutGestures.DebugJob,
            AppShortcutGestures.DebugStep,
            AppShortcutGestures.DebugContinue,
            AppShortcutGestures.ToggleBreakpoint
        ];

        Assert.Equal(gestures.Length, gestures.Select(Identity).Distinct().Count());
    }

    private static (Key Key, ModifierKeys Modifiers) Identity(KeyGesture gesture) => (gesture.Key, gesture.Modifiers);
}
