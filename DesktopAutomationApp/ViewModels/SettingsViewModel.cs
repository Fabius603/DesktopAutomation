using System.Collections.ObjectModel;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Settings;
using DesktopAutomationApp.Theming;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using DesktopAutomationApp.Services;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IUserPreferencesService _preferences;
    private readonly ILocalizationService _localization;
    private readonly IThemeService _theme;
    private readonly IWindowsStartupRegistrationService _startupRegistration;
    private readonly ILogger<SettingsViewModel> _log;
    private readonly IReleaseNotesService _releaseNotes;
    private readonly IUpdateService _updateService;
    private readonly IGlobalHotkeyService _hotkeys;
    private bool _isLoading = true;
    private LanguageOption? _selectedLanguage;
    private ThemeOption? _selectedTheme;
    private AccentOption? _selectedAccent;
    private bool _startWithWindows;
    private bool _startInBackgroundAtWindowsStartup;
    private bool _isCheckingForUpdates;
    private string _updateCheckStatus = string.Empty;
    private uint _forceStopVirtualKey;
    private bool _isCapturingForceStopKey;

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
        new("Blue", "#FF2196F3"),
        new("Indigo", "#FF5C6BC0"),
        new("Purple", "#FF8E5BD9"),
        new("Pink", "#FFE91E63"),
        new("Red", "#FFE53935"),
        new("Orange", "#FFF28C28"),
        new("Amber", "#FFD18B00"),
        new("Green", "#FF2EAD63"),
        new("Teal", "#FF00A6A6"),
        new("Cyan", "#FF00ACC1"),
        new("Slate", "#FF607D8B"),
        new("Graphite", "#FF4B5563"),
        new("Brown", "#FF795548"),
        new("Navy", "#FF34495E")
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

    public string ForceStopKeyDisplay =>
        _hotkeys.FormatKey(KeyModifiers.None, ForceStopVirtualKey);

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

    public RelayCommand ShowReleaseNotesCommand { get; }
    public RelayCommand CheckForUpdatesCommand { get; }
    public AsyncRelayCommand CaptureForceStopKeyCommand { get; }

    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        private set
        {
            SetProperty(ref _isCheckingForUpdates, value);
            CheckForUpdatesCommand?.RaiseCanExecuteChanged();
        }
    }

    public string UpdateCheckStatus
    {
        get => _updateCheckStatus;
        private set => SetProperty(ref _updateCheckStatus, value);
    }

    public SettingsViewModel(
        IUserPreferencesService preferences,
        ILocalizationService localization,
        IThemeService theme,
        IWindowsStartupRegistrationService startupRegistration,
        IReleaseNotesService releaseNotes,
        IUpdateService updateService,
        IGlobalHotkeyService hotkeys,
        ILogger<SettingsViewModel> log)
    {
        _preferences = preferences;
        _localization = localization;
        _theme = theme;
        _startupRegistration = startupRegistration;
        _releaseNotes = releaseNotes;
        _updateService = updateService;
        _hotkeys = hotkeys;
        _log = log;
        ShowReleaseNotesCommand = new RelayCommand(async () => await _releaseNotes.ShowAllAsync());
        CheckForUpdatesCommand = new RelayCommand(
            async () => await CheckForUpdatesAsync(),
            () => !IsCheckingForUpdates);
        CaptureForceStopKeyCommand = new AsyncRelayCommand(CaptureForceStopKeyAsync);
        var current = preferences.Current;
        current.ForceStopVirtualKey =
            ForceStopKeyConfiguration.Normalize(current.ForceStopVirtualKey);
        _hotkeys.SetForceStopKey(current.ForceStopVirtualKey);
        _selectedLanguage = Languages.FirstOrDefault(x => x.CultureName == current.Culture) ?? Languages[0];
        _selectedTheme = Themes.FirstOrDefault(x => x.Mode == current.ThemeMode) ?? Themes[0];
        _selectedAccent = Accents.FirstOrDefault(x => x.Name == current.Accent) ?? Accents[0];
        _startWithWindows = current.StartWithWindows;
        _startInBackgroundAtWindowsStartup = current.StartInBackgroundAtWindowsStartup;
        _forceStopVirtualKey = current.ForceStopVirtualKey;
        _localization.CultureChanged += (_, _) =>
        {
            foreach (var option in Themes) option.Refresh();
            OnPropertyChanged(nameof(ForceStopKeyButtonText));
        };
        _isLoading = false;
    }

    private async Task CaptureForceStopKeyAsync()
    {
        try
        {
            IsCapturingForceStopKey = true;
            var captured = await _hotkeys.CaptureNextAsync();
            ForceStopVirtualKey = captured.VirtualKeyCode;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Die Force-Stop-Taste konnte nicht erfasst werden.");
        }
        finally
        {
            IsCapturingForceStopKey = false;
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        UpdateCheckStatus = Loc.Get("Settings.Updates.Checking");
        try
        {
            var result = await _updateService.CheckForUpdateAsync();
            UpdateCheckStatus = result.HasUpdate
                ? Loc.Format("Settings.Updates.Available", result.LatestVersion)
                : Loc.Get("Settings.Updates.Current");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Die manuelle Update-Prüfung ist fehlgeschlagen.");
            UpdateCheckStatus = Loc.Get("Settings.Updates.Error");
        }
        finally
        {
            IsCheckingForUpdates = false;
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
        if (_isLoading || SelectedLanguage == null || SelectedTheme == null || SelectedAccent == null) return;
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
            _log.LogInformation("Einstellungen gespeichert: Sprache {Culture}, Theme {Theme}, Akzent {Accent}, Windows-Autostart {StartWithWindows}, Hintergrundstart {StartInBackground}, Force-Stop-Taste {ForceStopVirtualKey}.",
                current.Culture, current.ThemeMode, current.Accent, current.StartWithWindows, current.StartInBackgroundAtWindowsStartup, current.ForceStopVirtualKey);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Die Einstellungen konnten nicht gespeichert werden.");
        }

        try
        {
            _startupRegistration.Apply(current.StartWithWindows, current.StartInBackgroundAtWindowsStartup);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Der Windows-Autostart konnte nicht aktualisiert werden.");
        }
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
