using System.Runtime.InteropServices;
using System.Windows;
using ControlzEx.Theming;
using Microsoft.Win32;
using DesktopAutomationApp.Settings;

namespace DesktopAutomationApp.Theming;

public sealed class ThemeService : IThemeService, IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private AppThemeMode _requestedMode;
    private string _accent = "Blue";

    public AppThemeMode EffectiveMode { get; private set; } = AppThemeMode.Dark;
    public event EventHandler? ThemeChanged;

    public ThemeService() => SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

    public void Apply(AppThemeMode mode, string accent)
    {
        _requestedMode = mode;
        _accent = NormalizeAccent(accent);
        EffectiveMode = mode == AppThemeMode.System ? GetWindowsTheme() : mode;

        var application = Application.Current;
        if (application == null) return;

        var usesDarkBase = EffectiveMode is AppThemeMode.Dark or AppThemeMode.Black;
        ThemeManager.Current.ChangeTheme(application, usesDarkBase ? "Dark.Blue" : "Light.Blue");
        ReplaceDictionary(application, "AppThemePalette", $"Styles/Themes/{EffectiveMode}.xaml");
        ReplaceDictionary(application, "AppAccentPalette", $"Styles/Accents/{_accent}.xaml");
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_requestedMode == AppThemeMode.System)
            Application.Current?.Dispatcher.Invoke(() => Apply(_requestedMode, _accent));
    }

    private static AppThemeMode GetWindowsTheme()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return AppThemeMode.Dark;
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("AppsUseLightTheme") is int value && value != 0
            ? AppThemeMode.Light
            : AppThemeMode.Dark;
    }

    private static string NormalizeAccent(string accent) => accent is
        "Blue" or "Indigo" or "Purple" or "Pink" or "Red" or "Orange" or
        "Amber" or "Green" or "Teal" or "Cyan" or "Slate" or
        "Graphite" or "Brown" or "Navy"
            ? accent
            : "Blue";

    private static void ReplaceDictionary(Application application, string marker, string source)
    {
        var dictionaries = application.Resources.MergedDictionaries;
        var old = dictionaries.FirstOrDefault(d => d.Contains("App.Palette.Kind") && Equals(d["App.Palette.Kind"], marker));
        if (old != null) dictionaries.Remove(old);
        dictionaries.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
    }

    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
}
