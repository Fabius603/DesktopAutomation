using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Infrastructure;
using DesktopAutomationApp.Models;
using DesktopAutomationApp.Services;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;
using DesktopAutomationApp.Views;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private object? _currentContent;
        private string _currentContentName = "—";

        private readonly IViewModelFactory _viewModelFactory;
        private readonly IJobDispatcher _jobDispatcher;
        private readonly IUpdateService _updateService;
        private readonly IDialogService _dialogService;

        private bool _hasUpdate;
        private string _latestVersion = string.Empty;
        private string _updateUrl = string.Empty;
        private bool _isUpdating;
        private bool _isUpdateReady;
        private int _updateProgress;
        private string _updateStatusText = string.Empty;

        public bool HasUpdate
        {
            get => _hasUpdate;
            private set
            {
                SetProperty(ref _hasUpdate, value);
                OnPropertyChanged(nameof(ShowDownloadButton));
                (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
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

        /// <summary>True once Velopack has downloaded and verified the update.</summary>
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
        public ICommand OpenUpdateCommand { get; }  // fallback: browser

        private readonly StartViewModel _start;
        private readonly ListMakrosViewModel _listMakros;
        private readonly ListJobsViewModel _listJobs;
        private readonly ListAutomationsViewModel _listAutomations;
        private readonly YoloDownloadsViewModel _yoloDownloads;
        private readonly ExecutionLogsViewModel _executionLogs;
        private readonly SettingsViewModel _settings;

        public object? CurrentContent
        {
            get => _currentContent;
            set
            {
                var old = _currentContent;
                SetProperty(ref _currentContent, value);
                CurrentContentName = value?.GetType().Name ?? "—";
                if (old is IDisposable disposable
                    && !ReferenceEquals(old, value)
                    && !IsPersistedViewModel(old))
                {
                    // Defer disposal so WPF can fully detach the old view before cleanup runs.
                    Application.Current?.Dispatcher?.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => disposable.Dispose()));
                }
            }
        }

        /// <summary>Returns true for singleton VMs that are kept alive across navigations.</summary>
            private bool IsPersistedViewModel(object vm)
            => ReferenceEquals(vm, _start)
            || ReferenceEquals(vm, _listMakros)
            || ReferenceEquals(vm, _listJobs)
            || ReferenceEquals(vm, _listAutomations)
            || ReferenceEquals(vm, _yoloDownloads)
            || ReferenceEquals(vm, _executionLogs)
            || ReferenceEquals(vm, _settings);

        public string CurrentContentName
        {
            get => _currentContentName;
            private set => SetProperty(ref _currentContentName, value);
        }

        public ICommand ShowStart { get; }
        public ICommand ShowListMakros { get; }
        public ICommand ShowListJobs { get; }
        public ICommand ShowListAutomations { get; }
        public ICommand ShowYoloDownloads { get; }
        public ICommand ShowExecutionLogs { get; }
        public ICommand ShowSettings { get; }
        public ICommand StopAllJobsCommand { get; }

        public MainViewModel(
            IViewModelFactory viewModelFactory,
            IJobDispatcher jobDispatcher,
            IUpdateService updateService,
            IDialogService dialogService,
            StartViewModel startViewModel,
            ListMakrosViewModel listMakrosViewModel,
            ListJobsViewModel listJobsViewModel,
            ListAutomationsViewModel listAutomationsViewModel,
            YoloDownloadsViewModel yoloDownloadsViewModel,
            ExecutionLogsViewModel executionLogsViewModel,
            SettingsViewModel settingsViewModel)
        {
            _viewModelFactory = viewModelFactory;
            _jobDispatcher = jobDispatcher;
            _updateService = updateService;
            _dialogService = dialogService;
            _start = startViewModel;

            OpenUpdateCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(_updateUrl))
                    Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
            });

            InstallUpdateCommand = new RelayCommand(
                async () => await InstallUpdateAsync(),
                () => HasUpdate && !IsUpdating && !IsUpdateReady);
            _listMakros = listMakrosViewModel;
            _listJobs = listJobsViewModel;
            _listAutomations = listAutomationsViewModel;
            _yoloDownloads = yoloDownloadsViewModel;
            _executionLogs = executionLogsViewModel;
            _settings = settingsViewModel;

            // Events für Job-Fehler abonnieren
            _jobDispatcher.JobErrorOccurred += OnJobErrorOccurred;
            _jobDispatcher.JobStepErrorOccurred += OnJobStepErrorOccurred;

            // Navigation aus der Jobliste in die Details:
            _listJobs.RequestOpenJob += OpenJobDetails;

            // Navigation aus der Makroliste in die Details:
            _listMakros.RequestOpenMakro += OpenMakroDetails;

            // Navigation aus der Automationsliste in die Details:
            _listAutomations.RequestOpenAutomation += OpenAutomationDetails;

            ShowStart         = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) { CurrentContent = _start; _start.RefreshCommand.Execute(null); } });
            ShowListMakros    = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) { CurrentContent = _listMakros; _listMakros.RefreshCommand.Execute(null); } });
            ShowListJobs      = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) { CurrentContent = _listJobs;   _listJobs.RefreshCommand.Execute(null);   } });
            ShowListAutomations = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) { CurrentContent = _listAutomations; _listAutomations.RefreshCommand.Execute(null); } });
            ShowYoloDownloads = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) CurrentContent = _yoloDownloads; });
            ShowExecutionLogs = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) { CurrentContent = _executionLogs; _executionLogs.RefreshCommand.Execute(null); } });
            ShowSettings = new RelayCommand(async () => { if (await CheckNavigationGuardAsync()) CurrentContent = _settings; });
            StopAllJobsCommand = new RelayCommand(() =>
            {
                _jobDispatcher.CancelAllJobs();
                foreach (var id in _jobDispatcher.RunningMakroIds.ToList())
                    _jobDispatcher.CancelMakro(id);
            });

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

                _updateUrl = result.ReleaseUrl;
                LatestVersion = result.LatestVersion;
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
            UpdateStatusText = Loc.Get("Update.Downloading");
            (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();

            var progress = new Progress<int>(p =>
            {
                UpdateProgress = p;
                UpdateStatusText = Loc.Format("Update.DownloadingProgress", p);
            });

            try
            {
                var ok = await _updateService.DownloadUpdateAsync(progress);
                if (!ok)
                {
                    UpdateStatusText = Loc.Get("Update.DownloadError");
                    IsUpdating = false;
                    (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    return;
                }

                // The verified package is staged; show the restart button.
                UpdateProgress = 100;
                UpdateStatusText = Loc.Get("Update.Ready");
                IsUpdating = false;
                IsUpdateReady = true;
                (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            catch
            {
                UpdateStatusText = Loc.Get("Update.Error");
                IsUpdating = false;
                (InstallUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool PrepareUpdateAndRestart()
        {
            try
            {
                return _updateService.PrepareUpdateAndRestart();
            }
            catch
            {
                UpdateStatusText = Loc.Get("Update.Error");
                return false;
            }
        }

        private void OpenJobDetails(Job job)
        {
            var detailsVm = _viewModelFactory.CreateJobStepsViewModel(job);
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
            var detailsVm = _viewModelFactory.CreateMakroStepsViewModel(makro);
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

        private void OpenAutomationDetails(EditableAutomation automation)
        {
            var detailsVm = _viewModelFactory.CreateAutomationDetailViewModel(automation);
            detailsVm.RequestBack += async () =>
            {
                if (await CheckNavigationGuardAsync())
                {
                    CurrentContent = _listAutomations;
                    _listAutomations.RefreshCommand.Execute(null);
                }
            };
            CurrentContent = detailsVm;
        }

        private async Task<bool> CheckNavigationGuardAsync()
        {
            if (_currentContent is not INavigationGuard guard || !guard.HasUnsavedChanges)
                return true;

            var r = await _dialogService.ConfirmWithCancelAsync(
                Loc.Get("Dialog.Unsaved.Message"),
                Loc.Get("Dialog.Unsaved.Title"));
            if (r == true)  { await guard.SaveAsync(); return true; }
            if (r == false) { guard.DiscardChanges(); return true; }
            return false; // Cancel
        }

        private void OnJobErrorOccurred(object? sender, JobErrorEventArgs e)
        {
            // Job-Limit-Überschreitung: kein Popup – der Aufrufer-Job zeigt ohnehin einen Step-Fehler an.
            if (e.Exception is JobLimitExceededException)
                return;

            var message = Loc.Format("Error.JobExecution.Message", e.JobName, e.ErrorMessage);
            var title = Loc.Get("Error.JobExecution.Title");
            _dialogService.ShowError(message, title);
        }

        private void OnJobStepErrorOccurred(object? sender, JobStepErrorEventArgs e)
        {
            var message = Loc.Format("Error.JobStepExecution.Message", e.JobName, e.StepType, e.ErrorMessage);
            var title = Loc.Get("Error.JobStepExecution.Title");
            _dialogService.ShowError(message, title);
        }
    }
}
