using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection; // ActivatorUtilities
using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;

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

            ShowStart = new RelayCommand(() => CurrentContent = _start);
            ShowListMakros = new RelayCommand(() => CurrentContent = _listMakros);
            ShowListJobs = new RelayCommand(() => CurrentContent = _listJobs);
            ShowListHotkeys = new RelayCommand(() => CurrentContent = _listHotkeys);
            ShowYoloDownloads = new RelayCommand(() => CurrentContent = _yoloDownloads);

            // Startseite
            CurrentContent = _start;
        }

        private void OpenJobDetails(Job job)
        {
            // ViewModel mit Laufzeit-Argument 'job' aus DI erstellen
            var detailsVm = ActivatorUtilities.CreateInstance<JobStepsViewModel>(_services, job);

            // Rücknavigation: zurück zur Jobliste
            detailsVm.RequestBack += () => CurrentContent = _listJobs;

            CurrentContent = detailsVm;
        }

        private void OnJobErrorOccurred(object? sender, JobErrorEventArgs e)
        {
            // Auf dem UI-Thread ausführen, da wir ein MessageBox anzeigen
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var message = $"Fehler beim Ausführen des Jobs '{e.JobName}':\n\n{e.ErrorMessage}";
                var title = "Job-Ausführungsfehler";
                
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void OnJobStepErrorOccurred(object? sender, JobStepErrorEventArgs e)
        {
            // Auf dem UI-Thread ausführen, da wir ein MessageBox anzeigen
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var message = $"Fehler beim Ausführen des Job-Steps:\n\n" +
                             $"Job: {e.JobName}\n" +
                             $"Step: {e.StepType}\n\n" +
                             $"Fehlermeldung:\n{e.ErrorMessage}";
                var title = "Job-Step-Ausführungsfehler";
                
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }
}
