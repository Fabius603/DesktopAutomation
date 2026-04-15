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
                    EditedHotkey.Action.JobId = value.Id;
                    EditedHotkey.Action.Name = value.Name;
                }
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public ICollectionView ActionsView { get; }

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
            _isNew = hotkey.VirtualKeyCode == 0 && string.IsNullOrWhiteSpace(hotkey.Action?.Name);

            // Snapshot for cancel
            _snapshot = hotkey.Clone();

            // Track changes on the hotkey
            EditedHotkey.PropertyChanged += (_, _) => HasUnsavedChanges = true;
            EditedHotkey.Action.PropertyChanged += (_, _) => HasUnsavedChanges = true;

            foreach (var c in Enum.GetValues(typeof(ActionCommand)).Cast<ActionCommand>())
                AvailableCommands.Add(c);

            ActionsView = new ListCollectionView(Jobs);
            CommandsView = CollectionViewSource.GetDefaultView(AvailableCommands);

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(() =>
            {
                if (!_isNew) DiscardChanges();
                RequestBack?.Invoke();
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

            EditedHotkey.Action.SetJobNameResolver(GetCurrentJobNameForAction);
        }

        private void ResolveSelectedJob()
        {
            if (EditedHotkey.Action.JobId.HasValue)
                _selectedJob = Jobs.FirstOrDefault(j => j.Id == EditedHotkey.Action.JobId.Value);
            else if (!string.IsNullOrWhiteSpace(EditedHotkey.Action.Name))
                _selectedJob = Jobs.FirstOrDefault(j => string.Equals(j.Name, EditedHotkey.Action.Name, StringComparison.OrdinalIgnoreCase));
            else
                _selectedJob = null;
            OnPropertyChanged(nameof(SelectedJob));
        }

        private string GetCurrentJobNameForAction(EditableActionDefinition action)
        {
            if (action?.JobId.HasValue == true)
            {
                var job = Jobs.FirstOrDefault(j => j.Id == action.JobId.Value);
                if (job != null) return job.Name;
                return $"[Job nicht gefunden: {action.JobId}]";
            }
            return action?.Name ?? string.Empty;
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

            EditedHotkey.Name = dlg.ResultName.Trim();
            OnPropertyChanged(nameof(Title));
            HasUnsavedChanges = true;
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

            // Update JobId if needed
            if (EditedHotkey.Action.JobId.HasValue)
            {
                var currentJobName = GetCurrentJobNameForAction(EditedHotkey.Action);
                if (!currentJobName.StartsWith("[Job nicht gefunden"))
                    EditedHotkey.Action.Name = currentJobName;
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
            EditedHotkey.Action.Name = _snapshot.Action.Name;
            EditedHotkey.Action.Command = _snapshot.Action.Command;
            EditedHotkey.Action.JobId = _snapshot.Action.JobId;
            EditedHotkey.Active = _snapshot.Active;
            HasUnsavedChanges = false;
        }

        private string? ValidateEdited()
        {
            if (string.IsNullOrWhiteSpace(EditedHotkey.Name)) return "Name ist erforderlich.";
            if (string.IsNullOrWhiteSpace(EditedHotkey.Action?.Name)) return "Action ist erforderlich.";
            if (EditedHotkey.VirtualKeyCode == 0) return "Bitte eine Tastenkombination erfassen.";
            return null;
        }
    }
}
