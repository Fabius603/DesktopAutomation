using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using DesktopAutomationApp.Services;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListJobsViewModel : ViewModelBase
    {
        private readonly ILogger<ListJobsViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IRepositoryService _repositoryService;
        private readonly IJobDispatcher _dispatcher;

        public string Title => "Jobs";
        public string Description => "Verfügbare Jobs";

        public ObservableCollection<Job> Items { get; } = new();
        public ObservableCollection<Guid> RunningJobIds { get; } = new();

        private readonly List<Job> _selectedItems = new();
        public IReadOnlyList<Job> SelectedItems => _selectedItems;

        public void SetSelectedItems(IEnumerable<Job> items)
        {
            _selectedItems.Clear();
            _selectedItems.AddRange(items);
            InvalidateAllCommands();
        }

        public ICommand RefreshCommand { get; }
        public ICommand OpenJobCommand { get; } // Parameter: Job
        public ICommand DeleteJobCommand { get; }
        public ICommand CreateNewJobCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand SaveWithoutEditorCommand { get; }
        public ICommand StartJobCommand { get; }
        public ICommand StopJobCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public event Action<Job>? RequestOpenJob; // Signal an Host (MainViewModel)

        private Job? _selectedJob;
        public Job? SelectedJob
        {
            get => _selectedJob;
            set { _selectedJob = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        private bool _editing;
        public bool Editing
        {
            get => _editing;
            set => _editing = value;
        }

        public ListJobsViewModel(
            IJobExecutor executor,
            ILogger<ListJobsViewModel> log,
            IRepositoryService repositoryService,
            IJobDispatcher dispatcher)
        {
            _executor = executor;
            _log = log;
            _repositoryService = repositoryService;
            _dispatcher = dispatcher;

            RefreshCommand = new RelayCommand(LoadJobs);
            OpenJobCommand = new RelayCommand<Job?>(job =>
            {
                if (job != null) RequestOpenJob?.Invoke(job);
            }, job => job != null);
            DeleteJobCommand = new RelayCommand(async () => await DeleteJobAsync(), () => _selectedItems.Count > 0);
            CreateNewJobCommand = new RelayCommand(CreateNewJob);
            SaveAllCommand = new RelayCommand(async () => await SaveAllAsync());
            SaveWithoutEditorCommand = new RelayCommand(async () => await SaveWithoutEditorAsync());
            StartJobCommand = new RelayCommand<object?>(param =>
            {
                if (param is Guid id) _dispatcher.StartJob(id);
            });
            StopJobCommand = new RelayCommand<object?>(param =>
            {
                if (param is Guid id) _dispatcher.CancelJob(id);
            });
            OpenFolderCommand = new RelayCommand(() =>
                Process.Start(new ProcessStartInfo(_repositoryService.GetDirectoryPath<Job>()) { UseShellExecute = true }));

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;

            LoadJobs();
        }

        private async void LoadJobs()
        {
            await _executor.ReloadJobsAsync();

            Items.Clear();
            foreach (var j in _executor.AllJobs.Values.OrderBy(j => j.Name))
                Items.Add(j);


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
            if (_selectedItems.Count == 0) return;

            var message = _selectedItems.Count == 1
                ? $"Möchten Sie den Job '{_selectedItems[0].Name}' wirklich löschen?"
                : $"Möchten Sie die {_selectedItems.Count} ausgewählten Jobs wirklich löschen?";

            var result = AppDialog.Show(message, "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var toDelete = _selectedItems.ToList();
            foreach (var job in toDelete)
            {
                Items.Remove(job);
                await _repositoryService.DeleteAsync<Job>(job.Id.ToString());
                _log.LogInformation("Job gelöscht: {Name}", job.Name);
            }
            SelectedJob = null;
            await _executor.ReloadJobsAsync();
        }

        public async void CreateNewJob()
        {
            var dlg = new NewItemNameDialog("Neuer Job", "Name des neuen Jobs:")
                { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var newJob = new Job { Name = dlg.ResultName, Repeating = false, Steps = new() };
                await _repositoryService.SaveAsync(newJob);
                Items.Add(newJob);
                SelectedJob = newJob;
                await _executor.ReloadJobsAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler beim Erstellen eines neuen Jobs");
            }
        }

        private void OnRunningJobsChanged()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                RunningJobIds.Clear();
                foreach (var id in _dispatcher.RunningJobIds)
                    RunningJobIds.Add(id);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _dispatcher.RunningJobsChanged -= OnRunningJobsChanged;
            base.Dispose(disposing);
        }

        // ---------- Command invalidation helper ----------
        private void InvalidateAllCommands()
        {
            (DeleteJobCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenJobCommand   as RelayCommand<Job?>)?.RaiseCanExecuteChanged();
        }

    }
}
