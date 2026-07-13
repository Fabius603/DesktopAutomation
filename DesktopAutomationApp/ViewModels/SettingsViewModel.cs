using System.Collections.ObjectModel;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Settings;
using DesktopAutomationApp.Theming;
using System.ComponentModel;

namespace DesktopAutomationApp.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IUserPreferencesService _preferences;
    private readonly ILocalizationService _localization;
    private readonly IThemeService _theme;
    private bool _isLoading = true;
    private LanguageOption? _selectedLanguage;
    private ThemeOption? _selectedTheme;
    private AccentOption? _selectedAccent;

    public ObservableCollection<LanguageOption> Languages { get; } =
    [
        new("de-DE", "Deutsch"),
        new("en-US", "English")
    ];

    public ObservableCollection<ThemeOption> Themes { get; } =
    [
        new(AppThemeMode.System, "Settings.Theme.System"),
        new(AppThemeMode.Light, "Settings.Theme.Light"),
        new(AppThemeMode.Dark, "Settings.Theme.Dark")
    ];

    public ObservableCollection<AccentOption> Accents { get; } =
    [
        new("Blue", "#FF2196F3"),
        new("Teal", "#FF00A6A6"),
        new("Green", "#FF2EAD63"),
        new("Purple", "#FF8E5BD9"),
        new("Orange", "#FFF28C28")
    ];

    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set { if (SetAndChanged(ref _selectedLanguage, value)) _ = ApplyAsync(); }
    }

    public ThemeOption? SelectedTheme
    {
        get => _selectedTheme;
        set { if (SetAndChanged(ref _selectedTheme, value)) _ = ApplyAsync(); }
    }

    public AccentOption? SelectedAccent
    {
        get => _selectedAccent;
        set { if (SetAndChanged(ref _selectedAccent, value)) _ = ApplyAsync(); }
    }

    public SettingsViewModel(
        IUserPreferencesService preferences,
        ILocalizationService localization,
        IThemeService theme)
    {
        _preferences = preferences;
        _localization = localization;
        _theme = theme;
        var current = preferences.Current;
        _selectedLanguage = Languages.FirstOrDefault(x => x.CultureName == current.Culture) ?? Languages[0];
        _selectedTheme = Themes.FirstOrDefault(x => x.Mode == current.ThemeMode) ?? Themes[0];
        _selectedAccent = Accents.FirstOrDefault(x => x.Name == current.Accent) ?? Accents[0];
        _localization.CultureChanged += (_, _) =>
        {
            foreach (var option in Themes) option.Refresh();
        };
        _isLoading = false;
    }

    private bool SetAndChanged<T>(ref T field, T value)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged();
        return true;
    }

    private async Task ApplyAsync()
    {
        if (_isLoading || SelectedLanguage == null || SelectedTheme == null || SelectedAccent == null) return;
        var current = _preferences.Current;
        current.Culture = SelectedLanguage.CultureName;
        current.ThemeMode = SelectedTheme.Mode;
        current.Accent = SelectedAccent.Name;
        _localization.SetCulture(current.Culture);
        _theme.Apply(current.ThemeMode, current.Accent);
        await _preferences.SaveAsync();
    }
}

public sealed record LanguageOption(string CultureName, string DisplayName);
public sealed class ThemeOption : INotifyPropertyChanged
{
    public AppThemeMode Mode { get; }
    public string ResourceKey { get; }
    public string DisplayName => LocalizationService.Instance[ResourceKey];
    public event PropertyChangedEventHandler? PropertyChanged;
    public ThemeOption(AppThemeMode mode, string resourceKey) { Mode = mode; ResourceKey = resourceKey; }
    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
}
public sealed record AccentOption(string Name, string Color);
