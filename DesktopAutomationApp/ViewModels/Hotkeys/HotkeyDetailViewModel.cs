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
        // ── records ─────────────────────────────────────────────────────────────
        public record ActionItem(string Name, Guid Id, string Category);

        // ── DI ──────────────────────────────────────────────────────────────────
        private readonly IRepositoryService _repositoryService;
        private readonly IGlobalHotkeyService _capture;
        private readonly IJobExecutor _executor;
        private readonly ILogger<HotkeyDetailViewModel> _log;

        private readonly EditableHotkey _snapshot;
        private readonly bool _isNew;

        // ── public state ────────────────────────────────────────────────────────
        public EditableHotkey EditedHotkey { get; }
        public string Title => EditedHotkey.Name;

        public ObservableCollection<ActionItem> Actions { get; } = new();
        public ListCollectionView ActionsView { get; }

        private ActionItem? _selectedAction;
        public ActionItem? SelectedAction
        {
            get => _selectedAction;
            set
            {
                _selectedAction = value;
                if (EditedHotkey != null && value != null)
                {
                    if (value.Category == "Makro")
                    {
                        EditedHotkey.Job.ActionType = HotkeyActionType.Makro;
                        EditedHotkey.Job.MakroId   = value.Id;
                        EditedHotkey.Job.JobId      = null;
                        EditedHotkey.Job.Name       = value.Name;
                    }
                    else
                    {
                        EditedHotkey.Job.ActionType = HotkeyActionType.Job;
                        EditedHotkey.Job.JobId      = value.Id;
                        EditedHotkey.Job.MakroId    = null;
                        EditedHotkey.Job.Name       = value.Name;
                    }
                }
                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        /// <summary>Command-Zeile immer anzeigen — gilt für Jobs und Makros.</summary>
        public bool ShowCommand => true;

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

        // ── commands ────────────────────────────────────────────────────────────
        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand StartCaptureCommand { get; }
        public ICommand CancelCaptureCommand { get; }

        public event Action? RequestBack;

        // ── ctor ────────────────────────────────────────────────────────────────
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

            _snapshot = hotkey.Clone();

            EditedHotkey.PropertyChanged += (_, _) => HasUnsavedChanges = true;
            EditedHotkey.Job.PropertyChanged += (_, _) => HasUnsavedChanges = true;

            foreach (var c in Enum.GetValues(typeof(ActionCommand)).Cast<ActionCommand>())
                AvailableCommands.Add(c);

            ActionsView = new ListCollectionView(Actions);
            ActionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ActionItem.Category)));

            CommandsView = CollectionViewSource.GetDefaultView(AvailableCommands);

            BackCommand         = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand         = new RelayCommand(async () => await SaveAsync(), () => HasUnsavedChanges);
            CancelCommand       = new RelayCommand(() => { if (!_isNew) DiscardChanges(); }, () => HasUnsavedChanges);
            RenameCommand       = new RelayCommand(Rename);
            StartCaptureCommand = new RelayCommand(async () => await CaptureAsync(), () => !IsCapturing);
            CancelCaptureCommand = new RelayCommand(() => _captureCts?.Cancel(), () => IsCapturing);

            LoadActions();
            ResolveSelectedAction();

            HasUnsavedChanges = _isNew;
        }

        // ── loading ─────────────────────────────────────────────────────────────
        private void LoadActions()
        {
            _executor.ReloadJobsAsync().GetAwaiter().GetResult();
            Actions.Clear();

            foreach (var j in _executor.AllJobs.Values.OrderBy(j => j.Name))
                Actions.Add(new ActionItem(j.Name, j.Id, "Job"));

            foreach (var m in _executor.AllMakros.Values.OrderBy(m => m.Name))
                Actions.Add(new ActionItem(m.Name, m.Id, "Makro"));

            EditedHotkey.Job.SetJobNameResolver(GetCurrentActionName);
        }

        private void ResolveSelectedAction()
        {
            if (EditedHotkey.Job.ActionType == HotkeyActionType.Makro && EditedHotkey.Job.MakroId.HasValue)
            {
                _selectedAction = Actions.FirstOrDefault(a => a.Category == "Makro" && a.Id == EditedHotkey.Job.MakroId.Value);
            }
            else if (EditedHotkey.Job.JobId.HasValue)
            {
                _selectedAction = Actions.FirstOrDefault(a => a.Category == "Job" && a.Id == EditedHotkey.Job.JobId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(EditedHotkey.Job.Name))
            {
                _selectedAction = Actions.FirstOrDefault(a =>
                    string.Equals(a.Name, EditedHotkey.Job.Name, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                _selectedAction = null;
            }
            OnPropertyChanged(nameof(SelectedAction));
        }

        private string GetCurrentActionName(EditableJobReference jobRef)
        {
            if (jobRef.ActionType == HotkeyActionType.Makro && jobRef.MakroId.HasValue)
            {
                var m = Actions.FirstOrDefault(a => a.Category == "Makro" && a.Id == jobRef.MakroId.Value);
                return m?.Name ?? $"[Makro nicht gefunden: {jobRef.MakroId}]";
            }
            if (jobRef.JobId.HasValue)
            {
                var j = Actions.FirstOrDefault(a => a.Category == "Job" && a.Id == jobRef.JobId.Value);
                if (j != null) return j.Name;
                return $"[Job nicht gefunden: {jobRef.JobId}]";
            }
            return jobRef?.Name ?? string.Empty;
        }

        // ── capture ─────────────────────────────────────────────────────────────
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
            catch (OperationCanceledException) { _log.LogInformation("Hotkey-Erfassung abgebrochen."); }
            catch (Exception ex) { _log.LogError(ex, "Fehler bei der Hotkey-Erfassung."); }
            finally
            {
                _captureCts?.Dispose();
                _captureCts = null;
                IsCapturing = false;
            }
        }

        // ── rename ───────────────────────────────────────────────────────────────
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

        // ── INavigationGuard ─────────────────────────────────────────────────────
        public async Task SaveAsync()
        {
            var error = ValidateEdited();
            if (error != null)
            {
                _log.LogWarning("Hotkey ungueltig: {Error}", error);
                AppDialog.Show(error, "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ensure name is current
            if (EditedHotkey.Job.ActionType == HotkeyActionType.Job && EditedHotkey.Job.JobId.HasValue)
            {
                var name = GetCurrentActionName(EditedHotkey.Job);
                if (!name.StartsWith("[")) EditedHotkey.Job.Name = name;
            }

            await _repositoryService.SaveAsync(EditedHotkey.ToDomain());
            await _capture.ReloadFromRepositoryAsync();
            _log.LogInformation("Hotkey gespeichert: {Name}", EditedHotkey.Name);
            HasUnsavedChanges = false;
        }

        public void DiscardChanges()
        {
            EditedHotkey.Name           = _snapshot.Name;
            EditedHotkey.Modifiers      = _snapshot.Modifiers;
            EditedHotkey.VirtualKeyCode = _snapshot.VirtualKeyCode;
            EditedHotkey.Job.Name       = _snapshot.Job.Name;
            EditedHotkey.Job.Command    = _snapshot.Job.Command;
            EditedHotkey.Job.JobId      = _snapshot.Job.JobId;
            EditedHotkey.Job.ActionType = _snapshot.Job.ActionType;
            EditedHotkey.Job.MakroId    = _snapshot.Job.MakroId;
            EditedHotkey.Active         = _snapshot.Active;
            ResolveSelectedAction();
            HasUnsavedChanges = false;
        }

        private string? ValidateEdited()
        {
            if (string.IsNullOrWhiteSpace(EditedHotkey.Name)) return "Name ist erforderlich.";
            if (_selectedAction == null) return "Bitte eine Aktion (Job oder Makro) auswählen.";
            if (EditedHotkey.VirtualKeyCode == 0) return "Bitte eine Tastenkombination erfassen.";
            return null;
        }
    }
}
