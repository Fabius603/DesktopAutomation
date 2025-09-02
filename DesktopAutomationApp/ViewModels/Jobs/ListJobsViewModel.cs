using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging;
using SharpDX.Direct3D11;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using Common.JsonRepository;
using DesktopAutomationApp.Services;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListJobsViewModel : ViewModelBase
    {
        private readonly ILogger<ListJobsViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IRepositoryService _repositoryService;

        public string Title => "Jobs";
        public string Description => "Verfügbare Jobs";

        public ObservableCollection<Job> Items { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenJobCommand { get; } // Parameter: Job
        public ICommand DeleteJobCommand { get; }
        public ICommand CreateNewJobCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand SaveWithoutEditorCommand { get; }

        public event Action<Job>? RequestOpenJob; // Signal an Host (MainViewModel)

        private Job? _selectedJob;
        public Job? SelectedJob
        {
            get => _selectedJob;
            set { _selectedJob = value; OnPropertyChanged(); }
        }

        private bool _editing;
        public bool Editing
        {
            get => _editing;
            set => _editing = value;
        }

        public ListJobsViewModel(IJobExecutor executor, ILogger<ListJobsViewModel> log, IRepositoryService repositoryService)
        {
            _executor = executor;
            _log = log;
            _repositoryService = repositoryService;

            RefreshCommand = new RelayCommand(LoadJobs);
            OpenJobCommand = new RelayCommand<Job?>(job =>
            {
                if (job != null) RequestOpenJob?.Invoke(job);
            }, job => job != null);
            DeleteJobCommand = new RelayCommand(async () => await DeleteJobAsync());
            CreateNewJobCommand = new RelayCommand(CreateNewJob);
            SaveAllCommand = new RelayCommand(async () => await SaveAllAsync());
            SaveWithoutEditorCommand = new RelayCommand(async () => await SaveWithoutEditorAsync());
            LoadJobs();
        }

        private async void LoadJobs()
        {
            await _executor.ReloadJobsAsync();

            Items.Clear();
            foreach (var j in _executor.AllJobs.Values.OrderBy(j => j.Name))
                Items.Add(j);

            SelectedJob = Items.FirstOrDefault();
        }



        public async Task SaveAllAsync()
        {
            await _repositoryService.SaveAllAsync(Items);
            await _executor.ReloadJobsAsync();
        }

        public async Task SaveWithoutEditorAsync()
        {
            if (Editing) return;
            await SaveAllAsync();
        }

        public async Task DeleteJobAsync()
        {
            if (SelectedJob == null) return;

            var result = MessageBox.Show(
                $"Möchten Sie den Job „{SelectedJob.Name}“ wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            string name = SelectedJob.Name;
            var idx = Items.IndexOf(SelectedJob);
            Items.Remove(SelectedJob);
            SelectedJob = Items.ElementAtOrDefault(Math.Max(0, idx - 1));
            await _repositoryService.DeleteAsync<Job>(name);

            _log.LogInformation("Job gelöscht: {Name}", name);
        }

        public async void CreateNewJob()
        {
            try
            {
                var newJob = await _repositoryService.CreateNewAsync<Job>(
                    "NeuerJob", 
                    name => new Job { Name = name, Repeating = false, Steps = new() },
                    job => job.Name);
                
                Items.Add(newJob);
                SelectedJob = newJob;
                await _executor.ReloadJobsAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler beim Erstellen eines neuen Jobs");
            }
        }

        public async void EnsureUniqueNameFor(Job? j)
        {
            if (j == null) return;

            try
            {
                await _repositoryService.EnsureUniqueNameAsync(
                    j, 
                    job => job.Name ?? "", 
                    (job, name) => job.Name = name,
                    job => job.Name);
                await _executor.ReloadJobsAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler beim Eindeutig-Machen des Job-Namens");
            }
        }
    }
}
