using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
        private string _activeNavigationKey = "StartViewModel";

        private readonly IViewModelFactory _viewModelFactory;
        private readonly IJobDispatcher _jobDispatcher;
        private readonly IUpdateService _updateService;
        private readonly IDialogService _dialogService;
        private readonly UpdateCheckScheduler? _updateCheckScheduler;

        private bool _hasUpdate;
        private string _latestVersion = string.Empty;
        private string _updateUrl = string.Empty;
        private bool _isUpdating;
        private bool _isUpdateReady;
        private bool _isNavigating;
        private double _navigationProgress;
        private CancellationTokenSource? _navigationProgressCancellation;
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
        public string AppVersion { get; } = GetAppVersion();
        public string VersionLabel => $" v{AppVersion}";
        public string WindowTitle => $"DesktopAutomation{VersionLabel}";

        private readonly StartViewModel _start;
        private readonly ListMakrosViewModel _listMakros;
        private readonly ListJobsViewModel _listJobs;
        private readonly ListAutomationsViewModel _listAutomations;
        private readonly LogsHomeViewModel _logsHome;
        private readonly SettingsViewModel _settings;

        private static string GetAppVersion()
        {
            var assembly = typeof(MainViewModel).Assembly;
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
                return informationalVersion.Split('+')[0];

            var version = assembly.GetName().Version;
            return version == null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public object? CurrentContent
        {
            get => _currentContent;
            set
            {
                var old = _currentContent;
                SetProperty(ref _currentContent, value);
                CurrentContentName = value?.GetType().Name ?? "—";
                ActiveNavigationKey = GetNavigationKey(value);
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
        || ReferenceEquals(vm, _logsHome)
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
        public ICommand ShowExecutionLogs { get; }
        public ICommand ShowSettings { get; }
        public ICommand StopAllJobsCommand { get; }
        public ICommand ShowShortcutHelpCommand { get; }

        public MainViewModel(
            IViewModelFactory viewModelFactory,
            IJobDispatcher jobDispatcher,
            IUpdateService updateService,
            IDialogService dialogService,
            StartViewModel startViewModel,
            ListMakrosViewModel listMakrosViewModel,
            ListJobsViewModel listJobsViewModel,
            ListAutomationsViewModel listAutomationsViewModel,
            LogsHomeViewModel logsHomeViewModel,
            SettingsViewModel settingsViewModel)
        {
            _viewModelFactory = viewModelFactory;
            _jobDispatcher = jobDispatcher;
            _updateService = updateService;
            _dialogService = dialogService;
            _updateService.UpdateChecked += OnUpdateChecked;
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
            _logsHome = logsHomeViewModel;
            _settings = settingsViewModel;

            // Events für Job-Fehler abonnieren
            _jobDispatcher.JobErrorOccurred += OnJobErrorOccurred;
            _jobDispatcher.JobStepErrorOccurred += OnJobStepErrorOccurred;

            _start.RequestOpenDefinition += async (id, macro) =>
            {
                if (macro)
                {
                    await _listMakros.RefreshAsync();
                    if (_listMakros.Items.FirstOrDefault(item => item.Id == id) is { } definition) OpenMakroDetails(definition);
                }
                else
                {
                    await _listJobs.RefreshAsync();
                    if (_listJobs.Items.FirstOrDefault(item => item.Id == id) is { } job) OpenJobDetails(job);
                }
            };
            _start.RequestOpenAutomation += async id =>
            {
                await _listAutomations.RefreshAllAsync();
                if (_listAutomations.Items.FirstOrDefault(item => item.Id == id) is { } definition) OpenAutomationDetails(definition);
            };
            _start.RequestOpenLogs += async (id, macro) =>
            {
                _logsHome.Page = LogPageKind.Jobs;
                _logsHome.Source = macro ? TaskAutomation.Logging.LogSource.Makro : TaskAutomation.Logging.LogSource.Job;
                _logsHome.SourceId = id;
                await NavigateAsync(_logsHome);
            };

            // Navigation aus der Jobliste in die Details:
            _listJobs.RequestOpenJob += OpenJobDetails;

            // Navigation aus der Makroliste in die Details:
            _listMakros.RequestOpenMakro += OpenMakroDetails;

            // Navigation aus der Automationsliste in die Details:
            _listAutomations.RequestOpenAutomation += OpenAutomationDetails;

            ShowStart = new RelayCommand(async () => await NavigateAsync(_start, _start.RefreshAsync));
            ShowListMakros = new RelayCommand(async () => await NavigateAsync(_listMakros, _listMakros.RefreshAsync));
            ShowListJobs = new RelayCommand(async () => await NavigateAsync(_listJobs, _listJobs.RefreshAsync));
            ShowListAutomations = new RelayCommand(async () => await NavigateAsync(_listAutomations, _listAutomations.RefreshAllAsync));
            _logsHome.RequestOpenJob += (job, stepId) => _ = OpenJobDetailsAsync(job, stepId, true);
            _logsHome.RequestOpenAutomation += async id =>
            {
                await _listAutomations.RefreshAllAsync();
                var item = _listAutomations.Items.FirstOrDefault(value => value.Id == id);
                if (item is not null) await OpenAutomationDetailsAsync(item, true);
                else _dialogService.ShowError(Loc.Get("Logs.Ui.AutomationUnavailable"), Loc.Get("Logs.Title"));
            };
            ShowExecutionLogs = new RelayCommand(async () => await NavigateAsync(_logsHome));
            ShowSettings = new RelayCommand(async () => await NavigateAsync(_settings));
            ShowShortcutHelpCommand = new RelayCommand(() =>
                AppDialog.Show(
                    Loc.Get("Shortcut.Help.Message"),
                    Loc.Get("Shortcut.Help.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information));
            StopAllJobsCommand = new RelayCommand(() =>
            {
                _jobDispatcher.CancelAllJobs();
                foreach (var id in _jobDispatcher.RunningMakroIds.ToList())
                    _jobDispatcher.CancelMakro(id);
            });

            // Startseite
            CurrentContent = _start;

            // Sofort prüfen und anschließend auch bei langem Tray-Betrieb regelmäßig wiederholen.
            if (_updateService.Installation.CanUpdateInApp)
                _updateCheckScheduler = new UpdateCheckScheduler(CheckForUpdateAsync, TimeSpan.FromHours(2));
        }

        private async Task CheckForUpdateAsync()
        {
            try
            {
                await _updateService.CheckForUpdateAsync();
            }
            catch
            {
                // Update-Check darf niemals den App-Start blockieren oder abstürzen
            }
        }

        private void OnUpdateChecked(UpdateCheckResult result)
        {
            if (!result.HasUpdate) return;

            void ApplyResult()
            {
                _updateUrl = result.ReleaseUrl;
                LatestVersion = result.LatestVersion;
                HasUpdate = true;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is not null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(ApplyResult);
            else
                ApplyResult();
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

        public bool IsNavigating
        {
            get => _isNavigating;
            private set => SetProperty(ref _isNavigating, value);
        }

        public double NavigationProgress
        {
            get => _navigationProgress;
            private set => SetProperty(ref _navigationProgress, value);
        }

        private void StartNavigationProgress()
        {
            _navigationProgressCancellation?.Cancel();
            _navigationProgressCancellation?.Dispose();
            _navigationProgressCancellation = new CancellationTokenSource();

            NavigationProgress = 0;
            IsNavigating = true;
            _ = AnimateNavigationProgressAsync(_navigationProgressCancellation.Token);
        }

        private async Task AnimateNavigationProgressAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Anfangs schnell, danach zunehmend langsamer. Bei 92 % wird auf den
                // wirklichen Abschluss gewartet, damit der Balken niemals zu früh fertig ist.
                while (NavigationProgress < 92)
                {
                    await Task.Delay(70);
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    var remaining = 92 - NavigationProgress;
                    NavigationProgress = Math.Min(92, NavigationProgress + Math.Max(0.5, remaining * 0.09));
                }
            }
            catch (OperationCanceledException)
            {
                // Der echte Ladevorgang ist fertig; CompleteNavigationProgressAsync übernimmt.
            }
        }

        private async Task CompleteNavigationProgressAsync()
        {
            _navigationProgressCancellation?.Cancel();

            // Den Rest sichtbar und weich bis ganz nach rechts auffüllen.
            var start = NavigationProgress;
            for (var step = 1; step <= 5; step++)
            {
                NavigationProgress = start + (100 - start) * step / 5d;
                await Task.Delay(24);
            }

            IsNavigating = false;
        }

        /// <summary>Sidebar-Gruppe der aktuellen Seite. Detailseiten behalten die Gruppe ihrer übergeordneten Liste.</summary>
        public string ActiveNavigationKey
        {
            get => _activeNavigationKey;
            private set => SetProperty(ref _activeNavigationKey, value);
        }

        private static string GetNavigationKey(object? content) => content switch
        {
            StartViewModel => nameof(StartViewModel),
            ListMakrosViewModel or MakroStepsViewModel => nameof(ListMakrosViewModel),
            ListJobsViewModel or JobStepsViewModel => nameof(ListJobsViewModel),
            ListAutomationsViewModel or AutomationDetailViewModel => nameof(ListAutomationsViewModel),
            LogsHomeViewModel => nameof(LogsHomeViewModel),
            SettingsViewModel => nameof(SettingsViewModel),
            _ => string.Empty
        };

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateService.UpdateChecked -= OnUpdateChecked;
                _updateCheckScheduler?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OpenJobDetails(Job job)
            => _ = OpenJobDetailsAsync(job);

        private async Task OpenJobDetailsAsync(Job job, string? stepId = null, bool fromLogs = false)
        {
            if (IsNavigating || !await CheckNavigationGuardAsync()) return;
            StartNavigationProgress();
            try
            {
                await Dispatcher.Yield(DispatcherPriority.Render);
                var detailsVm = _viewModelFactory.CreateJobStepsViewModel(job);
                if (stepId is not null) detailsVm.SelectedStep = detailsVm.AllJobSteps.FirstOrDefault(step => step.Id == stepId);
                detailsVm.RequestBack += async () =>
                {
                    if (fromLogs) await NavigateAsync(_logsHome);
                    else await NavigateAsync(_listJobs, _listJobs.RefreshAsync);
                };
                CurrentContent = detailsVm;
            }
            finally
            {
                await CompleteNavigationProgressAsync();
            }
        }

        private void OpenMakroDetails(Makro makro)
            => _ = OpenMakroDetailsAsync(makro);

        private async Task OpenMakroDetailsAsync(Makro makro)
        {
            if (IsNavigating || !await CheckNavigationGuardAsync()) return;
            StartNavigationProgress();
            try
            {
                await Dispatcher.Yield(DispatcherPriority.Render);
                var detailsVm = _viewModelFactory.CreateMakroStepsViewModel(makro);
                detailsVm.RequestBack += async () =>
                {
                    await NavigateAsync(_listMakros, _listMakros.RefreshAsync);
                };
                CurrentContent = detailsVm;
            }
            finally
            {
                await CompleteNavigationProgressAsync();
            }
        }

        private void OpenAutomationDetails(EditableAutomation automation)
            => _ = OpenAutomationDetailsAsync(automation);

        private async Task OpenAutomationDetailsAsync(EditableAutomation automation, bool fromLogs = false)
        {
            if (IsNavigating || !await CheckNavigationGuardAsync()) return;
            StartNavigationProgress();
            try
            {
                await Dispatcher.Yield(DispatcherPriority.Render);
                var detailsVm = _viewModelFactory.CreateAutomationDetailViewModel(automation);
                detailsVm.RequestBack += async () =>
                {
                    if (fromLogs) await NavigateAsync(_logsHome);
                    else await NavigateAsync(_listAutomations, _listAutomations.RefreshAllAsync);
                };
                CurrentContent = detailsVm;
            }
            finally
            {
                await CompleteNavigationProgressAsync();
            }
        }

        private async Task NavigateAsync(object target, Func<Task>? prepareAsync = null)
        {
            if (IsNavigating || !await CheckNavigationGuardAsync()) return;

            // Detailseiten keep their backing list view models alive. Clear both single and
            // multi-selection when returning so the item that opened the editor is not still
            // selected, regardless of whether navigation came from Back or the sidebar.
            if (_currentContent is JobStepsViewModel && ReferenceEquals(target, _listJobs))
                _listJobs.ClearSelection();
            else if (_currentContent is MakroStepsViewModel && ReferenceEquals(target, _listMakros))
                _listMakros.ClearSelection();
            else if (_currentContent is AutomationDetailViewModel && ReferenceEquals(target, _listAutomations))
                _listAutomations.ClearSelection();

            StartNavigationProgress();
            try
            {
                await Dispatcher.Yield(DispatcherPriority.Render);
                if (prepareAsync != null) await prepareAsync();
                CurrentContent = target;
                await Dispatcher.Yield(DispatcherPriority.Render);
            }
            finally
            {
                await CompleteNavigationProgressAsync();
            }
        }

        private async Task<bool> CheckNavigationGuardAsync()
        {
            if (_currentContent is not INavigationGuard guard || !guard.HasUnsavedChanges)
                return true;

            var r = await _dialogService.ConfirmWithCancelAsync(
                Loc.Get("Dialog.Unsaved.Message"),
                Loc.Get("Dialog.Unsaved.Title"));
            if (r == true) { await guard.SaveAsync(); return true; }
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
            var presentation = StepErrorPresentation.Create(e);
            var message = Loc.Format(
                "Error.JobStepExecution.Message",
                e.JobName,
                presentation.StepName,
                presentation.Message,
                presentation.ErrorCode);
            var title = Loc.Get("Error.JobStepExecution.Title");
            _dialogService.ShowError(message, title);
        }
    }
}
