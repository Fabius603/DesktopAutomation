namespace DesktopAutomationApp.Settings;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public sealed class UserPreferences
{
    public string Culture { get; set; } = "de-DE";
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    public string Accent { get; set; } = "Blue";
}

