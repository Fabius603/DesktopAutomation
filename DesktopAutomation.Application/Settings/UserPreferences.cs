namespace DesktopAutomation.Application.Settings;

public enum AppThemeMode
{
    System,
    Light,
    Dark,
    Black
}

public sealed class UserPreferences
{
    public const uint DefaultForceStopVirtualKey = 0x79;

    public string Culture { get; set; } = "de-DE";
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    public string Accent { get; set; } = "Blue";
    public bool StartWithWindows { get; set; } = true;
    public bool StartInBackgroundAtWindowsStartup { get; set; } = true;
    public uint ForceStopVirtualKey { get; set; } = DefaultForceStopVirtualKey;
    public string LastSeenReleaseNotesVersion { get; set; } = string.Empty;
    public Dictionary<string, List<Guid>> ExpandedLibraryFolders { get; set; } = new();
    public bool NavigationCollapsed { get; set; }
    public bool StepInspectorCollapsed { get; set; }
}
