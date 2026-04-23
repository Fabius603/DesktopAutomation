using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection; // ActivatorUtilities
using DesktopAutomationApp.Models;
using DesktopAutomationApp.Services;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;
using DesktopAutomationApp.Views;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private object? _currentContent;
        private string _currentContentName = "—";

        private readonly IServiceProvider _services;
        private readonly IJobDispatcher _jobDispatcher;
        private readonly IUpdateService _updateService;

        private bool _hasUpdate;
        private string _latestVersion = string.Empty;
        private string _updateUrl = string.Empty;
        private string _assetDownloadUrl = string.Empty;
        private bool _isUpdating;
        private bool _isUpdateReady;
        private int _updateProgress;
        private string _updateStatusText = string.Empty;

        public bool HasUpdate
        {
            get => _hasUpdate;
            private set { SetProperty(ref _hasUpdate, value); OnPropertyChanged(nameof(ShowDownloadButton)); }
        }

        public string LatestVersion
        {
            get => _latestVersion;
            private set => SetProperty(ref _latestVersion, value);
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            private set { SetProperty(ref _isUpdating, value); OnPropertyChanged(nameof(ShowDownloadButton)); }
        }

        /// <summary>True once the ZIP has been downloaded and the PS1 script is ready — shows the Restart button.</summary>
        public bool IsUpdateReady
        {
            get => _isUpdateReady;
            private set { SetProperty(ref _isUpdateReady, value); OnPropertyChanged(nameof(ShowDownloadButton)); }
        }

        /// <summary>Shows "Jetzt updaten" only when there is an update and it hasn't been downloaded yet.</summary>
        public bool ShowDownloadButton => HasUpdate && !IsUpdating && !IsUpdateReady;

        public int UpdateProgress
        {
            get => _updateProgress;
            private set => SetProperty(ref _updateProgress, value);
        }

        public string UpdateStatusText
        {
            get => _updateStatusText;
            private set => SetProperty(ref _updateStatusText, value);
        }

        public ICommand InstallUpdateCommand { get; }
        public ICommand RestartToUpdateCommand { get; }
        public ICommand OpenUpdateCommand { get; }  // fallback: browser

        private readonly StartViewModel _start;
        private readonly ListMakrosViewModel _listMakros;
        private readonly ListJobsViewModel _listJobs;
        private readonly ListHotkeysViewModel _listHotkeys;
        private readonly YoloDownloadsViewModel _yoloDownloads;

        public object? CurrentContent
        {
            get => _currentContent;
            set
            {
                SetProperty(ref _currentContent, value);
                CurrentContentName = value?.GetType().Name ?? "—";
            }
        }

        public string CurrentContentName
        {
            get => _currentContentName;
            private set => SetProperty(ref _currentContentName, value);
        }

        public ICommand ShowStart { get; }
        public ICommand ShowListMakros { get; }
        public ICommand ShowListJobs { get; }
        public ICommand ShowListHotkeys { get; }
        public ICommand ShowYoloDownloads { get; }

        public MainViewModel(
            IServiceProvider services,
            IJobDispatcher jobDispatcher,
            IUpdateService updateService,
            StartViewModel startViewModel,
            ListMakrosViewModel listMakrosViewModel,
            ListJobsViewModel listJobsViewModel,
            ListHotkeysViewModel listHotkeysViewModel,
            YoloDownloadsViewModel yoloDownloadsViewModel)
        {
            _services = services;
            _jobDispatcher = jobDispatcher;
            _updateService = updateService;
            _start = startViewModel;

            OpenUpdateCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(_updateUrl))
                    Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
            });

            InstallUpdateCommand = new RelayCommand(
                async () => await InstallUpdateAsync(),
                () => HasUpdate && !IsUpdating && !IsUpdateReady);
            RestartToUpdateCommand = new RelayCommand(
                () => Application.Current.Shutdown());
            _listMakros = listMakrosViewModel;
            _listJobs = listJobsViewModel;
            _listHotkeys = listHotkeysViewModel;
            _yoloDownloads = yoloDownloadsViewModel;

            // Events für Job-Fehler abonnieren
            _jobDispatcher.JobErrorOccurred += OnJobErrorOccurred;
            _jobDispatcher.JobStepErrorOccurred += OnJobStepErrorOccurred;

            // Navigation aus der Jobliste in die Details:
            _listJobs.RequestOpenJob += OpenJobDetails;

            // Navigation aus der Makroliste in die Details:
            _listMakros.RequestOpenMakro += OpenMakroDetails;

            // Navigation aus der Hotkeyliste in die Details:
            _listHotkeys.RequestOpenHotkey += OpenHotkeyDetails;

            ShowStart         = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) CurrentContent = _start; });
            ShowListMakros    = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) { CurrentContent = _listMakros; _listMakros.RefreshCommand.Execute(null); } });
            ShowListJobs      = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) { CurrentContent = _listJobs;   _listJobs.RefreshCommand.Execute(null);   } });
            ShowListHotkeys   = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) { CurrentContent = _listHotkeys; _listHotkeys.RefreshCommand.Execute(null); } });
            ShowYoloDownloads = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) CurrentContent = _yoloDownloads; });

            // Startseite
            CurrentContent = _start;

            // Update-Check im Hintergrund
            _ = CheckForUpdateAsync();
        }

        private async Task CheckForUpdateAsync()
        {
            try
            {
                var result = await _updateService.CheckForUpdateAsync();
                if (!result.HasUpdate) return;

                _updateUrl = result.HtmlUrl;
                _assetDownloadUrl = result.AssetDownloadUrl;
                LatestVersion = result.LatestTag;
                HasUpdate = true;
            }
            catch
            {
                // Update-Check darf niemals den App-Start blockieren oder abstürzen
            }
        }

        private async Task InstallUpdateAsync()
        {
            IsUpdating = true;
            IsUpdateReady = false;
            UpdateProgress = 0;
            UpdateStatusText = "Wird heruntergeladen…";
            (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();

            var progress = new Progress<int>(p =>
            {
                UpdateProgress = p;
                UpdateStatusText = $"Wird heruntergeladen… {p} %";
            });

            try
            {
                var ok = await _updateService.DownloadAndInstallAsync(_assetDownloadUrl, progress);
                if (!ok)
                {
                    UpdateStatusText = "Fehler beim Download.";
                    IsUpdating = false;
                    (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();

                    // Fallback: Browser öffnen
                    if (!string.IsNullOrEmpty(_updateUrl))
                        Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
                    return;
                }

                // Download + PS1 script ready — show the Restart button
                UpdateProgress = 100;
                UpdateStatusText = "Update bereit. Bitte neu starten.";
                IsUpdating = false;
                IsUpdateReady = true;
                (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            catch
            {
                UpdateStatusText = "Fehler beim Update.";
                IsUpdating = false;
                (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void OpenJobDetails(Job job)
        {
            var detailsVm = ActivatorUtilities.CreateInstance<JobStepsViewModel>(_services, job);
            detailsVm.RequestBack += async () =>
            {
                if (await CheckNavigationGuardAsync())
                {
                    CurrentContent = _listJobs;
                    _listJobs.RefreshCommand.Execute(null);
                }
            };
            CurrentContent = detailsVm;
        }

        private void OpenMakroDetails(Makro makro)
        {
            var detailsVm = ActivatorUtilities.CreateInstance<MakroStepsViewModel>(_services, makro);
            detailsVm.RequestBack += async () =>
            {
                if (await CheckNavigationGuardAsync())
                {
                    CurrentContent = _listMakros;
                    _listMakros.RefreshCommand.Execute(null);
                }
            };
            CurrentContent = detailsVm;
        }

        private void OpenHotkeyDetails(EditableHotkey hotkey)
        {
            var detailsVm = ActivatorUtilities.CreateInstance<HotkeyDetailViewModel>(_services, hotkey);
            detailsVm.RequestBack += async () =>
            {
                if (await CheckNavigationGuardAsync())
                {
                    CurrentContent = _listHotkeys;
                    _listHotkeys.RefreshCommand.Execute(null);
                }
            };
            CurrentContent = detailsVm;
        }

        private async Task<bool> CheckNavigationGuardAsync()
        {
            if (_currentContent is not INavigationGuard guard || !guard.HasUnsavedChanges)
                return true;

            var r = AppDialog.Show(
                "Es gibt ungespeicherte Änderungen. Möchten Sie speichern?",
                "Ungespeicherte Änderungen",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (r == MessageBoxResult.Yes)   { await guard.SaveAsync(); return true; }
            if (r == MessageBoxResult.No)    { guard.DiscardChanges(); return true; }
            return false; // Cancel
        }

        private void OnJobErrorOccurred(object? sender, JobErrorEventArgs e)
        {
            // Auf dem UI-Thread ausführen, da wir ein MessageBox anzeigen
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var message = $"Fehler beim Ausführen des Jobs '{e.JobName}':\n\n{e.ErrorMessage}";
                var title = "Job-Ausführungsfehler";
                
                AppDialog.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void OnJobStepErrorOccurred(object? sender, JobStepErrorEventArgs e)
        {
            // Auf dem UI-Thread ausführen, da wir ein AppDialog anzeigen
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var message = $"Fehler beim Ausführen des Job-Steps:\n\n" +
                             $"Job: {e.JobName}\n" +
                             $"Step: {e.StepType}\n\n" +
                             $"Fehlermeldung:\n{e.ErrorMessage}";
                var title = "Job-Step-Ausführungsfehler";

                AppDialog.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }
}
