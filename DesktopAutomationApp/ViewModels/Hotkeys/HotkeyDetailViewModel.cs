using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using DesktopAutomationApp.Models;
using DesktopAutomationApp.Services;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class HotkeyDetailViewModel : ViewModelBase, INavigationGuard
    {
        private readonly IRepositoryService _repositoryService;
        private readonly IGlobalHotkeyService _capture;
        private readonly IJobExecutor _executor;
        private readonly ILogger<HotkeyDetailViewModel> _log;

        private readonly EditableHotkey _snapshot;
        private readonly bool _isNew;

        public EditableHotkey EditedHotkey { get; }

        public string Title => EditedHotkey.Name;

        public ObservableCollection<Job> Jobs { get; } = new();

        private Job? _selectedJob;
        public Job? SelectedJob
        {
            get => _selectedJob;
            set
            {
                _selectedJob = value;
                if (EditedHotkey != null && value != null)
                {
                    EditedHotkey.Job.JobId = value.Id;
                    EditedHotkey.Job.Name = value.Name;
                }
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public ICollectionView JobsView { get; }

        public ObservableCollection<ActionCommand> AvailableCommands { get; } = new();
        public ICollectionView CommandsView { get; }

        private bool _isCapturing;
        public bool IsCapturing
        {
            get => _isCapturing;
            private set { _isCapturing = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }
        private CancellationTokenSource? _captureCts;

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set { _hasUnsavedChanges = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand StartCaptureCommand { get; }
        public ICommand CancelCaptureCommand { get; }

        public event Action? RequestBack;

        public HotkeyDetailViewModel(
            EditableHotkey hotkey,
            IRepositoryService repositoryService,
            IGlobalHotkeyService capture,
            IJobExecutor executor,
            ILogger<HotkeyDetailViewModel> log)
        {
            EditedHotkey = hotkey ?? throw new ArgumentNullException(nameof(hotkey));
            _repositoryService = repositoryService;
            _capture = capture;
            _executor = executor;
            _log = log;
            _isNew = hotkey.VirtualKeyCode == 0 && string.IsNullOrWhiteSpace(hotkey.Job?.Name);

            // Snapshot for cancel
            _snapshot = hotkey.Clone();

            // Track changes on the hotkey
            EditedHotkey.PropertyChanged += (_, _) => HasUnsavedChanges = true;
            EditedHotkey.Job.PropertyChanged += (_, _) => HasUnsavedChanges = true;

            foreach (var c in Enum.GetValues(typeof(ActionCommand)).Cast<ActionCommand>())
                AvailableCommands.Add(c);

            JobsView = new ListCollectionView(Jobs);
            CommandsView = CollectionViewSource.GetDefaultView(AvailableCommands);

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(() =>
            {
                if (!_isNew) DiscardChanges();
            }, () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(Rename);
            StartCaptureCommand = new RelayCommand(async () => await CaptureAsync(), () => !IsCapturing);
            CancelCaptureCommand = new RelayCommand(() => _captureCts?.Cancel(), () => IsCapturing);

            LoadJobs();
            ResolveSelectedJob();

            // New hotkeys need saving, existing ones start clean
            HasUnsavedChanges = _isNew;
        }

        private void LoadJobs()
        {
            _executor.ReloadJobsAsync().GetAwaiter().GetResult();
            Jobs.Clear();
            foreach (var j in _executor.AllJobs.Values.OrderBy(j => j.Name))
                Jobs.Add(j);

            EditedHotkey.Job.SetJobNameResolver(GetCurrentJobName);
        }

        private void ResolveSelectedJob()
        {
            if (EditedHotkey.Job.JobId.HasValue)
                _selectedJob = Jobs.FirstOrDefault(j => j.Id == EditedHotkey.Job.JobId.Value);
            else if (!string.IsNullOrWhiteSpace(EditedHotkey.Job.Name))
                _selectedJob = Jobs.FirstOrDefault(j => string.Equals(j.Name, EditedHotkey.Job.Name, StringComparison.OrdinalIgnoreCase));
            else
                _selectedJob = null;
            OnPropertyChanged(nameof(SelectedJob));
        }

        private string GetCurrentJobName(EditableJobReference jobRef)
        {
            if (jobRef?.JobId.HasValue == true)
            {
                var job = Jobs.FirstOrDefault(j => j.Id == jobRef.JobId.Value);
                if (job != null) return job.Name;
                return $"[Job nicht gefunden: {jobRef.JobId}]";
            }
            return jobRef?.Name ?? string.Empty;
        }

        private async Task CaptureAsync()
        {
            _captureCts = new CancellationTokenSource();
            IsCapturing = true;

            try
            {
                var (mods, vk) = await _capture.CaptureNextAsync(_captureCts.Token);
                EditedHotkey.Modifiers = mods;
                EditedHotkey.VirtualKeyCode = vk;
                HasUnsavedChanges = true;
            }
            catch (OperationCanceledException)
            {
                _log.LogInformation("Hotkey-Erfassung abgebrochen.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler bei der Hotkey-Erfassung.");
            }
            finally
            {
                _captureCts?.Dispose();
                _captureCts = null;
                IsCapturing = false;
            }
        }

        private async void Rename()
        {
            var dlg = new NewItemNameDialog("Umbenennen", "Neuer Name:", EditedHotkey.Name)
                { Owner = Application.Current?.MainWindow };
            if (dlg.ShowDialog() != true) return;

            var hadChanges = HasUnsavedChanges;
            EditedHotkey.Name = dlg.ResultName.Trim();
            OnPropertyChanged(nameof(Title));
            _snapshot.Name = EditedHotkey.Name;
            HasUnsavedChanges = hadChanges;

            if (!_isNew)
            {
                await _repositoryService.SaveAsync(EditedHotkey.ToDomain());
                await _capture.ReloadFromRepositoryAsync();
                _log.LogInformation("Hotkey umbenannt: {Name}", EditedHotkey.Name);
            }
        }

        // --- INavigationGuard ---

        public async Task SaveAsync()
        {
            var error = ValidateEdited();
            if (error != null)
            {
                _log.LogWarning("Hotkey ungueltig: {Error}", error);
                AppDialog.Show(error, "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditedHotkey.Job.JobId.HasValue)
            {
                var currentJobName = GetCurrentJobName(EditedHotkey.Job);
                if (!currentJobName.StartsWith("[Job nicht gefunden"))
                    EditedHotkey.Job.Name = currentJobName;
            }

            await _repositoryService.SaveAsync(EditedHotkey.ToDomain());
            await _capture.ReloadFromRepositoryAsync();
            _log.LogInformation("Hotkey gespeichert: {Name}", EditedHotkey.Name);
            HasUnsavedChanges = false;
        }

        public void DiscardChanges()
        {
            EditedHotkey.Name = _snapshot.Name;
            EditedHotkey.Modifiers = _snapshot.Modifiers;
            EditedHotkey.VirtualKeyCode = _snapshot.VirtualKeyCode;
            EditedHotkey.Job.Name = _snapshot.Job.Name;
            EditedHotkey.Job.Command = _snapshot.Job.Command;
            EditedHotkey.Job.JobId = _snapshot.Job.JobId;
            EditedHotkey.Active = _snapshot.Active;
            HasUnsavedChanges = false;
        }

        private string? ValidateEdited()
        {
            if (string.IsNullOrWhiteSpace(EditedHotkey.Name)) return "Name ist erforderlich.";
            if (string.IsNullOrWhiteSpace(EditedHotkey.Job?.Name)) return "Job ist erforderlich.";
            if (EditedHotkey.VirtualKeyCode == 0) return "Bitte eine Tastenkombination erfassen.";
            return null;
        }
    }
}
