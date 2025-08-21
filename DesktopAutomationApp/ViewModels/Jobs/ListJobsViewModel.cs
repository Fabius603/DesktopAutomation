using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SharpDX.Direct3D11;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Persistence;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListJobsViewModel : ViewModelBase
    {
        private readonly ILogger<ListJobsViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IJsonRepository<Job> _jobRepo;

        public string Title => "Jobs";
        public string Description => "Verfügbare Jobs";

        public ObservableCollection<Job> Items { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenJobCommand { get; } // Parameter: Job
        public ICommand DeleteJobCommand { get; }
        public ICommand CreateNewJobCommand { get; }

        public event Action<Job>? RequestOpenJob; // Signal an Host (MainViewModel)

        private Job? _selectedJob;
        public Job? SelectedJob
        {
            get => _selectedJob;
            set { _selectedJob = value; OnPropertyChanged(); }
        }

        public ListJobsViewModel(IJobExecutor executor, ILogger<ListJobsViewModel> log, IJsonRepository<Job> jobRepo)
        {
            _executor = executor;
            _log = log;
            _jobRepo = jobRepo;

            RefreshCommand = new RelayCommand(LoadJobs);
            OpenJobCommand = new RelayCommand<Job?>(job =>
            {
                if (job != null) RequestOpenJob?.Invoke(job);
            }, job => job != null);
            DeleteJobCommand = new RelayCommand(async () => await DeleteJobAsync());
            CreateNewJobCommand = new RelayCommand(CreateNewJob);
            LoadJobs();
        }

        private async void LoadJobs()
        {
            await _executor.ReloadJobsAsync();

            Items.Clear();
            foreach (var j in _executor.AllJobs.Values.OrderBy(j => j.Name))
                Items.Add(j);

            SelectedJob = Items.FirstOrDefault();

            _log.LogInformation("Jobs geladen: {Count}", Items.Count);
        }

        public async Task SaveAllAsync()
        {
            await _jobRepo.SaveAllAsync(Items);
            await _executor.ReloadJobsAsync();
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
            await _jobRepo.DeleteAsync(name);

            _log.LogInformation("Job gelöscht: {Name}", name);
        }

        public void CreateNewJob()
        {
            var name = UniqueName("NeuerJob");
            var m = new Job { Name = name, Repeating = false, Steps = new() };
            Items.Add(m);
            SelectedJob = m;

            SaveAllAsync();
        }

        private string UniqueName(string baseName)
        {
            var i = 1;
            var n = baseName;
            while (Items.Any(x => string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase)))
                n = $"{baseName}_{i++}";
            return n;
        }

        public void EnsureUniqueNameFor(Job? j)
        {
            if (j == null) return;

            var proposed = j.Name?.Trim() ?? "";
            if (string.IsNullOrEmpty(proposed))
            {
                j.Name = UniqueName("Job");
                return;
            }

            // bereits eindeutig?
            bool collision = Items.Any(x =>
                !ReferenceEquals(x, j) &&
                string.Equals(x.Name, proposed, StringComparison.OrdinalIgnoreCase));

            if (!collision) return;

            // Kollision -> automatisch eindeutigen Namen erzeugen
            j.Name = UniqueName(proposed);
            _log.LogInformation("Makro-Name kollidierte, automatisch umbenannt in: {Name}", j.Name);
        }
    }
}
