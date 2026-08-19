using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services;
using Microsoft.Extensions.Logging;

namespace DesktopAutomationApp.ViewModels;

public sealed class UpdateSettingsViewModel : ViewModelBase
{
    private readonly IReleaseNotesService _releaseNotes;
    private readonly IUpdateService _updateService;
    private readonly ILogger<UpdateSettingsViewModel> _log;
    private bool _isCheckingForUpdates;
    private string _updateCheckStatus = string.Empty;

    public UpdateSettingsViewModel(
        IReleaseNotesService releaseNotes,
        IUpdateService updateService,
        ILogger<UpdateSettingsViewModel> log)
    {
        _releaseNotes = releaseNotes;
        _updateService = updateService;
        _log = log;
        ShowReleaseNotesCommand = new RelayCommand(async () => await _releaseNotes.ShowAllAsync());
        CheckForUpdatesCommand = new RelayCommand(
            async () => await CheckForUpdatesAsync(),
            () => !IsCheckingForUpdates);
    }

    public RelayCommand ShowReleaseNotesCommand { get; }
    public RelayCommand CheckForUpdatesCommand { get; }

    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        private set
        {
            SetProperty(ref _isCheckingForUpdates, value);
            CheckForUpdatesCommand.RaiseCanExecuteChanged();
        }
    }

    public string UpdateCheckStatus
    {
        get => _updateCheckStatus;
        private set => SetProperty(ref _updateCheckStatus, value);
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
        catch (Exception exception)
        {
            _log.LogWarning(exception, "Die manuelle Update-Prüfung ist fehlgeschlagen.");
            UpdateCheckStatus = Loc.Get("Settings.Updates.Error");
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }
}
