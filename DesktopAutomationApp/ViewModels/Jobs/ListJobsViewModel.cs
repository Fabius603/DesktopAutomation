using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DesktopAutomation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using DesktopAutomationApp.Services;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListJobsViewModel : ViewModelBase
    {
        private readonly ILogger<ListJobsViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IJobApplicationService _jobAppService;
        private readonly IDialogService _dialogService;
        private readonly IJobDispatcher _dispatcher;

        public string Title => "Jobs";
        public string Description => "Verfügbare Jobs";

        public ObservableCollection<Job> Items { get; } = new();
        private IReadOnlyCollection<Guid> _runningJobIds = Array.Empty<Guid>();
        public IReadOnlyCollection<Guid> RunningJobIds
        {
            get => _runningJobIds;
            private set { _runningJobIds = value; OnPropertyChanged(); }
        }

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
            IJobApplicationService jobAppService,
            IDialogService dialogService,
            IJobDispatcher dispatcher)
        {
            _executor = executor;
            _log = log;
            _jobAppService = jobAppService;
            _dialogService = dialogService;
            _dispatcher = dispatcher;

            RefreshCommand = new RelayCommand(async () => await RefreshAsync());
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
                if (param is Guid id)
                    try { _dispatcher.StartJob(id); }
                    catch (JobLimitExceededException) { /* kein Popup – wird still ignoriert */ }
            });
            StopJobCommand = new RelayCommand<object?>(param =>
            {
                if (param is not Guid id) return;
                _dispatcher.CancelJobsByDefinition(id);
            });
            OpenFolderCommand = new RelayCommand(() =>
                Process.Start(new ProcessStartInfo(_jobAppService.GetStoragePath()) { UseShellExecute = true }));

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;

            _ = RefreshAsync();
        }


        public async Task RefreshAsync()
        {
            await _jobAppService.ReloadAsync();

            Items.Clear();
            foreach (var j in _jobAppService.Jobs.Values.OrderBy(j => j.Name))
                Items.Add(j);


        }



        public async Task SaveAllAsync()
        {
            foreach (var j in Items) await _jobAppService.SaveJobAsync(j);
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
                ? Loc.Format("Job.Delete.One", _selectedItems[0].Name)
                : Loc.Format("Job.Delete.Many", _selectedItems.Count);

            var confirmed = await _dialogService.ConfirmAsync(message, Loc.Get("Dialog.Delete.Title"));
            if (!confirmed) return;

            var toDelete = _selectedItems.ToList();
            foreach (var job in toDelete)
            {
                Items.Remove(job);
                await _jobAppService.DeleteJobAsync(job.Id);
                _log.LogInformation("Job gelöscht: {Name}", job.Name);
            }
            SelectedJob = null;
        }

        public async void CreateNewJob()
        {
            var name = await _dialogService.AskForNameAsync(Loc.Get("Job.New.Title"), Loc.Get("Job.New.Prompt"));
            if (name == null) return;

            try
            {
                var newJob = await _jobAppService.CreateJobAsync(name);
                Items.Add(newJob);
                SelectedJob = newJob;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler beim Erstellen eines neuen Jobs");
            }
        }

        private void OnRunningJobsChanged()
        {
            // Snapshot op ThreadPool thread – als HashSet für O(1) Contains() im Converter.
            var ids = new HashSet<Guid>(_dispatcher.RunningJobIds);
            Application.Current?.Dispatcher?.InvokeAsync(() => RunningJobIds = ids);
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
