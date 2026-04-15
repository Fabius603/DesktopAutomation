using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection; // ActivatorUtilities
using DesktopAutomationApp.Models;
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
            StartViewModel startViewModel,
            ListMakrosViewModel listMakrosViewModel,
            ListJobsViewModel listJobsViewModel,
            ListHotkeysViewModel listHotkeysViewModel,
            YoloDownloadsViewModel yoloDownloadsViewModel)
        {
            _services = services;
            _jobDispatcher = jobDispatcher;
            _start = startViewModel;
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
