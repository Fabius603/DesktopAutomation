using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection; // ActivatorUtilities
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private object? _currentContent;
        private string _currentContentName = "—";

        private readonly IServiceProvider _services;

        private readonly StartViewModel _start;
        private readonly ListMakrosViewModel _listMakros;
        private readonly ListJobsViewModel _listJobs;
        private readonly ListHotkeysViewModel _listHotkeys;

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

        public MainViewModel(
            IServiceProvider services,
            StartViewModel startViewModel,
            ListMakrosViewModel listMakrosViewModel,
            ListJobsViewModel listJobsViewModel,
            ListHotkeysViewModel listHotkeysViewModel)
        {
            _services = services;
            _start = startViewModel;
            _listMakros = listMakrosViewModel;
            _listJobs = listJobsViewModel;
            _listHotkeys = listHotkeysViewModel;

            // Navigation aus der Jobliste in die Details:
            _listJobs.RequestOpenJob += OpenJobDetails;

            ShowStart = new RelayCommand(() => CurrentContent = _start);
            ShowListMakros = new RelayCommand(() => CurrentContent = _listMakros);
            ShowListJobs = new RelayCommand(() => CurrentContent = _listJobs);
            ShowListHotkeys = new RelayCommand(() => CurrentContent = _listHotkeys);

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
    }
}
