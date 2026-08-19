using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services;
using DesktopAutomationApp.Settings;
using DesktopAutomationApp.Theming;
using Microsoft.Extensions.Logging;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.ViewModels;

public sealed class GeneralSettingsViewModel : ViewModelBase
{
    private readonly IUserPreferencesService _preferences;
    private readonly ILocalizationService _localization;
    private readonly IThemeService _theme;
    private readonly IWindowsStartupRegistrationService _startupRegistration;
    private readonly IGlobalHotkeyService _hotkeys;
    private readonly ILogger<GeneralSettingsViewModel> _log;
    private bool _isLoading = true;
    private LanguageOption? _selectedLanguage;
    private ThemeOption? _selectedTheme;
    private AccentOption? _selectedAccent;
    private bool _startWithWindows;
    private bool _startInBackgroundAtWindowsStartup;
    private uint _forceStopVirtualKey;
    private bool _isCapturingForceStopKey;

    public GeneralSettingsViewModel(
        IUserPreferencesService preferences,
        ILocalizationService localization,
        IThemeService theme,
        IWindowsStartupRegistrationService startupRegistration,
        IGlobalHotkeyService hotkeys,
        ILogger<GeneralSettingsViewModel> log)
    {
        _preferences = preferences;
        _localization = localization;
        _theme = theme;
        _startupRegistration = startupRegistration;
        _hotkeys = hotkeys;
        _log = log;
        CaptureForceStopKeyCommand = new AsyncRelayCommand(CaptureForceStopKeyAsync);

        var current = preferences.Current;
        current.ForceStopVirtualKey = ForceStopKeyConfiguration.Normalize(current.ForceStopVirtualKey);
        _hotkeys.SetForceStopKey(current.ForceStopVirtualKey);
        _selectedLanguage = Languages.FirstOrDefault(option => option.CultureName == current.Culture) ?? Languages[0];
        _selectedTheme = Themes.FirstOrDefault(option => option.Mode == current.ThemeMode) ?? Themes[0];
        _selectedAccent = Accents.FirstOrDefault(option => option.Name == current.Accent) ?? Accents[0];
        _startWithWindows = current.StartWithWindows;
        _startInBackgroundAtWindowsStartup = current.StartInBackgroundAtWindowsStartup;
        _forceStopVirtualKey = current.ForceStopVirtualKey;
        _localization.CultureChanged += (_, _) =>
        {
            foreach (var option in Themes)
                option.Refresh();
            OnPropertyChanged(nameof(ForceStopKeyButtonText));
        };
        _isLoading = false;
    }

    public ObservableCollection<LanguageOption> Languages { get; } =
    [
        new("de-DE", "Deutsch"),
        new("en-US", "English")
    ];

    public ObservableCollection<ThemeOption> Themes { get; } =
    [
        new(AppThemeMode.System, "Settings.Theme.System"),
        new(AppThemeMode.Light, "Settings.Theme.Light"),
        new(AppThemeMode.Dark, "Settings.Theme.Dark"),
        new(AppThemeMode.Black, "Settings.Theme.Black")
    ];

    public ObservableCollection<AccentOption> Accents { get; } =
    [
        new("Blue", "#FF2196F3"), new("Indigo", "#FF5C6BC0"), new("Purple", "#FF8E5BD9"),
        new("Pink", "#FFE91E63"), new("Red", "#FFE53935"), new("Orange", "#FFF28C28"),
        new("Amber", "#FFD18B00"), new("Green", "#FF2EAD63"), new("Teal", "#FF00A6A6"),
        new("Cyan", "#FF00ACC1"), new("Slate", "#FF607D8B"), new("Graphite", "#FF4B5563"),
        new("Brown", "#FF795548"), new("Navy", "#FF34495E")
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

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set { if (SetAndChanged(ref _startWithWindows, value)) _ = ApplyAsync(); }
    }

    public bool StartInBackgroundAtWindowsStartup
    {
        get => _startInBackgroundAtWindowsStartup;
        set { if (SetAndChanged(ref _startInBackgroundAtWindowsStartup, value)) _ = ApplyAsync(); }
    }

    public uint ForceStopVirtualKey
    {
        get => _forceStopVirtualKey;
        private set
        {
            if (!SetAndChanged(ref _forceStopVirtualKey, value)) return;
            _hotkeys.SetForceStopKey(value);
            OnPropertyChanged(nameof(ForceStopKeyDisplay));
            _ = ApplyAsync();
        }
    }

    public string ForceStopKeyDisplay => _hotkeys.FormatKey(KeyModifiers.None, ForceStopVirtualKey);

    public bool IsCapturingForceStopKey
    {
        get => _isCapturingForceStopKey;
        private set
        {
            if (!SetAndChanged(ref _isCapturingForceStopKey, value)) return;
            OnPropertyChanged(nameof(ForceStopKeyButtonText));
        }
    }

    public string ForceStopKeyButtonText => Loc.Get(IsCapturingForceStopKey
        ? "Settings.ForceStopKey.Capturing"
        : "Settings.ForceStopKey.Change");

    public AsyncRelayCommand CaptureForceStopKeyCommand { get; }

    private async Task CaptureForceStopKeyAsync()
    {
        try
        {
            IsCapturingForceStopKey = true;
            ForceStopVirtualKey = (await _hotkeys.CaptureNextAsync()).VirtualKeyCode;
        }
        catch (Exception exception)
        {
            _log.LogError(exception, "Die Force-Stop-Taste konnte nicht erfasst werden.");
        }
        finally
        {
            IsCapturingForceStopKey = false;
        }
    }

    private bool SetAndChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private async Task ApplyAsync()
    {
        if (_isLoading || SelectedLanguage is null || SelectedTheme is null || SelectedAccent is null)
            return;

        var current = _preferences.Current;
        current.Culture = SelectedLanguage.CultureName;
        current.ThemeMode = SelectedTheme.Mode;
        current.Accent = SelectedAccent.Name;
        current.StartWithWindows = StartWithWindows;
        current.StartInBackgroundAtWindowsStartup = StartInBackgroundAtWindowsStartup;
        current.ForceStopVirtualKey = ForceStopVirtualKey;
        _hotkeys.SetForceStopKey(current.ForceStopVirtualKey);
        _localization.SetCulture(current.Culture);
        _theme.Apply(current.ThemeMode, current.Accent);
        try
        {
            await _preferences.SaveAsync();
        }
        catch (Exception exception)
        {
            _log.LogError(exception, "Die Einstellungen konnten nicht gespeichert werden.");
        }

        try
        {
            _startupRegistration.Apply(current.StartWithWindows, current.StartInBackgroundAtWindowsStartup);
        }
        catch (Exception exception)
        {
            _log.LogError(exception, "Der Windows-Autostart konnte nicht aktualisiert werden.");
        }
    }
}

public sealed record LanguageOption(string CultureName, string DisplayName);

public sealed class ThemeOption(AppThemeMode mode, string resourceKey) : INotifyPropertyChanged
{
    public AppThemeMode Mode { get; } = mode;
    public string ResourceKey { get; } = resourceKey;
    public string DisplayName => LocalizationService.Instance[ResourceKey];
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
}

public sealed record AccentOption(string Name, string Color);
