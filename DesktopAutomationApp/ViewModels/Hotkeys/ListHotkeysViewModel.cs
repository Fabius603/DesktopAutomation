using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using DesktopAutomationApp.Models;
using Microsoft.Extensions.Logging;
using TaskAutomation.Hotkeys;
using TaskAutomation.Persistence;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListHotkeysViewModel : ViewModelBase
    {
        private readonly IJsonRepository<HotkeyDefinition> _repo;
        private readonly IGlobalHotkeyService _capture;
        private readonly ILogger<ListHotkeysViewModel> _log;

        public ObservableCollection<EditableHotkey> Items { get; } = new();

        private EditableHotkey? _selected;
        public EditableHotkey? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            private set { _isEditing = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsBrowse)); CommandManager.InvalidateRequerySuggested(); }
        }
        public bool IsBrowse => !IsEditing;

        private bool _isCapturing;
        public bool IsCapturing
        {
            get => _isCapturing;
            private set { _isCapturing = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }
        private CancellationTokenSource? _captureCts;

        private EditableHotkey? _edited;
        private EditableHotkey? _snapshot;
        private bool _isNew;

        public EditableHotkey? EditedHotkey
        {
            get => _edited;
            private set { _edited = value; OnPropertyChanged(); }
        }

        // Filterquellen wie gehabt …
        public ObservableCollection<string> AvailableActions { get; } = new();
        public ICollectionView ActionsView { get; }
        private string _actionFilter = "";
        public string ActionFilter { get => _actionFilter; set { _actionFilter = value; ActionsView.Refresh(); OnPropertyChanged(); } }

        public ObservableCollection<ActionCommand> AvailableCommands { get; } = new();
        public ICollectionView CommandsView { get; }
        private string _commandFilter = "";
        public string CommandFilter { get => _commandFilter; set { _commandFilter = value; CommandsView.Refresh(); OnPropertyChanged(); } }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand SaveEditCommand { get;  }
        public ICommand CancelEditCommand { get; }
        public ICommand StartCaptureCommand { get; }
        public ICommand CancelCaptureCommand { get; }

        public ListHotkeysViewModel(
            IJsonRepository<HotkeyDefinition> repo,
            IGlobalHotkeyService capture,
            ILogger<ListHotkeysViewModel> log)
        {
            _repo = repo;
            _capture = capture;
            _log = log;

            // Demo-Actions – später aus Jobs/Makros aggregieren
            AvailableActions.Add("Job:Screenshot");
            AvailableActions.Add("Job:NightlyRun");
            AvailableActions.Add("Makro:OpenMail");
            AvailableActions.Add("Makro:Build");
            AvailableActions.Add("Service:ToggleOverlay");
            foreach (var c in Enum.GetValues(typeof(ActionCommand)).Cast<ActionCommand>()) AvailableCommands.Add(c);

            ActionsView = CollectionViewSource.GetDefaultView(AvailableActions);
            ActionsView.Filter = o => o is string s && (string.IsNullOrWhiteSpace(ActionFilter) || s.Contains(ActionFilter, StringComparison.OrdinalIgnoreCase));

            CommandsView = CollectionViewSource.GetDefaultView(AvailableCommands);
            CommandsView.Filter = o => o is ActionCommand c && (string.IsNullOrWhiteSpace(CommandFilter) || c.ToString().Contains(CommandFilter, StringComparison.OrdinalIgnoreCase));

            RefreshCommand       = new RelayCommand(async () => await LoadAsync());
            NewCommand           = new RelayCommand(NewHotkey, () => IsBrowse);
            EditCommand          = new RelayCommand<object?>(_ => EditHotkey(), _ => IsBrowse && Selected != null);
            DeleteCommand        = new RelayCommand(async () => await DeleteSelectedAsync(), () => IsBrowse && Selected != null);
            SaveAllCommand       = new RelayCommand(async () => await SaveAllAsync(), () => IsEditing && EditedHotkey != null);
            SaveEditCommand      = new RelayCommand(SaveEdit, () => IsEditing && EditedHotkey != null);
            CancelEditCommand    = new RelayCommand(CancelEdit, () => IsEditing);
            StartCaptureCommand  = new RelayCommand(async () => await CaptureAsync(), () => IsEditing && !IsCapturing && EditedHotkey != null);
            CancelCaptureCommand = new RelayCommand(() => _captureCts?.Cancel(), () => IsEditing && IsCapturing);

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var list = await _repo.LoadAllAsync();
            Items.Clear();
            foreach (var hk in list.OrderBy(h => h.Name))
                Items.Add(EditableHotkey.FromDomain(hk));
            Selected = Items.FirstOrDefault();
            IsEditing = false; IsCapturing = false; EditedHotkey = null; _snapshot = null; _isNew = false;
            _log.LogInformation("Hotkeys geladen: {Count}", Items.Count);
        }

        private void NewHotkey()
        {
            var e = new EditableHotkey
            {
                Name = "Neuer Hotkey",
                Modifiers = KeyModifiers.None,
                VirtualKeyCode = 0,
                Action = new EditableActionDefinition { Name = "", Command = ActionCommand.Start }
            };
            Items.Add(e);
            Selected = e;
            _isNew = true;
            _snapshot = null;
            EditedHotkey = e;
            IsEditing = true;
        }

        private void EditHotkey()
        {
            if (Selected == null) return;
            _isNew = false;
            _snapshot = Selected.Clone();   // Snapshot für Cancel
            EditedHotkey = Selected;        // Edit am Original (Wrapper) → UI aktualisiert sofort
            IsEditing = true;
        }

        private async Task CaptureAsync()
        {
            if (EditedHotkey == null) return;

            // neue CTS anlegen und merken
            _captureCts = new CancellationTokenSource();
            IsCapturing = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                var (mods, vk) = await _capture.CaptureNextAsync(_captureCts.Token);
                EditedHotkey.Modifiers = mods;
                EditedHotkey.VirtualKeyCode = vk;
            }
            catch (OperationCanceledException)
            {
                // Benutzer hat abgebrochen – kein Fehler
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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void SaveEdit()
        {
            if (EditedHotkey == null) return;
            var error = ValidateEdited(EditedHotkey);
            if (error != null) { _log.LogWarning("Hotkey ungültig: {Error}", error); return; }
            IsEditing = false; EditedHotkey = null; _snapshot = null; _isNew = false;
            SaveAllAsync();
        }

        private void CancelEdit()
        {
            if (EditedHotkey != null)
            {
                if (_isNew)
                {
                    Items.Remove(EditedHotkey);
                }
                else if (_snapshot != null)
                {
                    EditedHotkey.Name = _snapshot.Name;
                    EditedHotkey.Modifiers = _snapshot.Modifiers;
                    EditedHotkey.VirtualKeyCode = _snapshot.VirtualKeyCode;
                    EditedHotkey.Action.Name = _snapshot.Action.Name;
                    EditedHotkey.Action.Command = _snapshot.Action.Command;
                }
            }
            IsEditing = false; IsCapturing = false; EditedHotkey = null; _snapshot = null; _isNew = false;
        }

        private async Task DeleteSelectedAsync()
        {
            if (Selected == null) return;
            var name = Selected.Name;
            var idx = Items.IndexOf(Selected);
            Items.Remove(Selected);
            Selected = Items.ElementAtOrDefault(Math.Max(0, idx - 1));
            await _repo.DeleteAsync(name);
        }

        private async Task SaveAllAsync()
        {
            var domain = Items.Select(i => i.ToDomain()).ToList();
            await _repo.SaveAllAsync(domain);
            _log.LogInformation("Hotkeys gespeichert: {Count}", domain.Count);

            await _capture.ReloadFromRepositoryAsync();
        }

        private static string? ValidateEdited(EditableHotkey hk)
        {
            if (string.IsNullOrWhiteSpace(hk.Name)) return "Name ist erforderlich.";
            if (string.IsNullOrWhiteSpace(hk.Action?.Name)) return "Action ist erforderlich.";
            if (hk.VirtualKeyCode == 0) return "Bitte eine Tastenkombination erfassen.";
            return null;
        }
    }
}
