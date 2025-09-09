using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using DesktopAutomationApp.Models;
using Microsoft.Extensions.Logging;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
using Common.JsonRepository;
using Xabe.FFmpeg.Downloader;
using DesktopAutomationApp.Services;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListHotkeysViewModel : ViewModelBase
    {
        private readonly IRepositoryService _repositoryService;
        private readonly IGlobalHotkeyService _capture;
        private readonly ILogger<ListHotkeysViewModel> _log;
        private readonly IJobExecutor _executor;

        public ObservableCollection<EditableHotkey> Items { get; } = new();
        public ObservableCollection<Job> Jobs { get; } = new();

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
        public ICommand ToggleActiveCommand { get; }

        public ListHotkeysViewModel(
            IRepositoryService repositoryService,
            IGlobalHotkeyService capture,
            IJobExecutor executor,
            ILogger<ListHotkeysViewModel> log)
        {
            _repositoryService = repositoryService;
            _capture = capture;
            _log = log;
            _executor = executor;

            // Demo-Actions – später aus Jobs/Makros aggregieren
            AvailableActions.Add("Action 1");
            AvailableActions.Add("Action 2");
            AvailableActions.Add("Action 3");
            AvailableActions.Add("Action 4");
            AvailableActions.Add("Action 5");
            foreach (var c in Enum.GetValues(typeof(ActionCommand)).Cast<ActionCommand>()) AvailableCommands.Add(c);

            ActionsView = CollectionViewSource.GetDefaultView(AvailableActions);
            ActionsView.Filter = o => o is string s && (string.IsNullOrWhiteSpace(ActionFilter) || s.Contains(ActionFilter, StringComparison.OrdinalIgnoreCase));

            CommandsView = CollectionViewSource.GetDefaultView(AvailableCommands);
            CommandsView.Filter = o => o is ActionCommand c && (string.IsNullOrWhiteSpace(CommandFilter) || c.ToString().Contains(CommandFilter, StringComparison.OrdinalIgnoreCase));

            RefreshCommand          = new RelayCommand(async () => await RefreshAllAsync());
            NewCommand              = new RelayCommand(NewHotkey, () => IsBrowse);
            EditCommand             = new RelayCommand<object?>(_ => EditHotkey(), _ => IsBrowse && Selected != null);
            DeleteCommand           = new RelayCommand(async () => await DeleteSelectedAsync(), () => IsBrowse && Selected != null);
            SaveAllCommand          = new RelayCommand(async () => await SaveAllAsync(), () => IsEditing && EditedHotkey != null);
            SaveEditCommand         = new RelayCommand(SaveEdit, () => IsEditing && EditedHotkey != null);
            CancelEditCommand       = new RelayCommand(CancelEdit, () => IsEditing);
            StartCaptureCommand     = new RelayCommand(async () => await CaptureAsync(), () => IsEditing && !IsCapturing && EditedHotkey != null);
            CancelCaptureCommand    = new RelayCommand(() => _captureCts?.Cancel(), () => IsEditing && IsCapturing);

            _ = InitialLoadAsync();
        }

        private async Task InitialLoadAsync()
        {
            // Beim ersten Laden nur einmal Jobs laden
            LoadJobs();
            
            var list = await _repositoryService.LoadAllAsync<HotkeyDefinition>();
            Items.Clear();
            foreach (var hk in list.OrderBy(h => h.Name))
            {
                var ehk = EditableHotkey.FromDomain(hk);
                // Job-Name-Resolver setzen für korrekte Anzeige
                ehk.Action.SetJobNameResolver(GetCurrentJobNameForAction);
                Items.Add(ehk);
                ehk.PropertyChanged += SaveActiveNoEditor;
            }
            Selected = Items.FirstOrDefault();
            IsEditing = false; IsCapturing = false; EditedHotkey = null; _snapshot = null; _isNew = false;
            _log.LogInformation("Hotkeys initial geladen: {HotkeyCount} Hotkeys, {JobCount} Jobs", Items.Count, Jobs.Count);
        }

        private async Task RefreshAllAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            // Jobs explizit neu laden (z.B. bei Refresh-Button)
            LoadJobs();
            
            var list = await _repositoryService.LoadAllAsync<HotkeyDefinition>();
            Items.Clear();
            foreach (var hk in list.OrderBy(h => h.Name))
            {
                var ehk = EditableHotkey.FromDomain(hk);
                // Job-Name-Resolver setzen für korrekte Anzeige
                ehk.Action.SetJobNameResolver(GetCurrentJobNameForAction);
                Items.Add(ehk);
                ehk.PropertyChanged += SaveActiveNoEditor;
            }
            Selected = Items.FirstOrDefault();
            IsEditing = false; IsCapturing = false; EditedHotkey = null; _snapshot = null; _isNew = false;
            _log.LogInformation("Hotkeys und Jobs neu geladen: {HotkeyCount} Hotkeys, {JobCount} Jobs", Items.Count, Jobs.Count);
        }

        private async void NewHotkey()
        {
            // Eindeutigen Namen generieren
            var uniqueName = await GenerateUniqueHotkeyNameAsync("Neuer Hotkey");
            
            // Jobs nur laden wenn noch keine vorhanden
            if (Jobs.Count == 0)
            {
                LoadJobs();
            }
            
            var e = new EditableHotkey
            {
                Name = uniqueName,
                Modifiers = KeyModifiers.None,
                VirtualKeyCode = 0,
                Action = new EditableActionDefinition { Name = "", Command = ActionCommand.Toggle },
                Active = true
            };
            // Job-Name-Resolver setzen
            e.Action.SetJobNameResolver(GetCurrentJobNameForAction);
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
            
            // Jobs nur beim ersten Edit neu laden, um aktuelle Daten zu haben
            if (Jobs.Count == 0)
            {
                LoadJobs();
            }
            
            // Beim Bearbeiten: Action.Name auf aktuellen Job-Namen setzen falls Job-ID vorhanden
            if (EditedHotkey.Action.JobId.HasValue)
            {
                var currentJobName = GetCurrentJobNameForAction(EditedHotkey.Action);
                if (!currentJobName.StartsWith("[Job nicht gefunden"))
                {
                    EditedHotkey.Action.Name = currentJobName;
                }
            }
            
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

        private async void SaveEdit()
        {
            if (EditedHotkey == null) return;
            var error = ValidateEdited(EditedHotkey);
            if (error != null) { _log.LogWarning("Hotkey ungültig: {Error}", error); return; }
            
            // Job-ID automatisch setzen basierend auf Job-Namen
            UpdateJobIdFromJobName(EditedHotkey);
            
            // Namen eindeutig machen, falls nötig
            await EnsureUniqueNameForEditedHotkeyAsync();
            
            // Nach dem Speichern: Sicherstellen, dass Action.Name der aktuelle Job-Name ist
            if (EditedHotkey.Action.JobId.HasValue)
            {
                var currentJobName = GetCurrentJobNameForAction(EditedHotkey.Action);
                if (!currentJobName.StartsWith("[Job nicht gefunden"))
                {
                    EditedHotkey.Action.Name = currentJobName;
                }
            }
            
            IsEditing = false; EditedHotkey = null; _snapshot = null; _isNew = false;
            await SaveAllAsync();
        }

        private void UpdateJobIdFromJobName(EditableHotkey hotkey)
        {
            if (hotkey?.Action == null || string.IsNullOrWhiteSpace(hotkey.Action.Name))
                return;

            var job = Jobs.FirstOrDefault(j => string.Equals(j.Name, hotkey.Action.Name, StringComparison.OrdinalIgnoreCase));
            if (job != null)
            {
                hotkey.Action.JobId = job.Id;
                _log.LogDebug("Job-ID für Hotkey '{HotkeyName}' gesetzt: {JobName} -> {JobId}", hotkey.Name, job.Name, job.Id);
            }
            else
            {
                // Wenn kein Job mit diesem Namen gefunden wird, Job-ID zurücksetzen
                hotkey.Action.JobId = null;
                _log.LogDebug("Kein Job für Hotkey '{HotkeyName}' mit Namen '{JobName}' gefunden, Job-ID zurückgesetzt", hotkey.Name, hotkey.Action.Name);
            }
        }

        /// <summary>
        /// Gibt den aktuellen Job-Namen für die angegebene Hotkey-Action zurück.
        /// Verwendet die Job-ID falls vorhanden, andernfalls den gespeicherten Namen.
        /// </summary>
        public string GetCurrentJobNameForAction(EditableActionDefinition action)
        {
            if (action?.JobId.HasValue == true)
            {
                var job = Jobs.FirstOrDefault(j => j.Id == action.JobId.Value);
                if (job != null)
                {
                    return job.Name;
                }
                // Job mit dieser ID existiert nicht mehr
                _log.LogWarning("Job mit ID {JobId} nicht gefunden", action.JobId);
                return $"[Job nicht gefunden: {action.JobId}]";
            }
            
            // Fallback auf gespeicherten Namen
            return action?.Name ?? string.Empty;
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
                    EditedHotkey.Active = _snapshot.Active;
                }
            }
            IsEditing = false; IsCapturing = false; EditedHotkey = null; _snapshot = null; _isNew = false;
        }

        private async Task DeleteSelectedAsync()
        {
            if (Selected == null) return;

            var result = MessageBox.Show(
                $"Möchten Sie den Hotkey „{Selected.Name}“ wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var name = Selected.Name;
            var idx = Items.IndexOf(Selected);
            
            // Hotkey-Registrierung aufheben falls vorhanden
            _capture.UnregisterHotkey(name);
            
            Items.Remove(Selected);
            Selected = Items.ElementAtOrDefault(Math.Max(0, idx - 1));
            await _repositoryService.DeleteAsync<HotkeyDefinition>(name);

            _log.LogInformation("Hotkey gelöscht und Registrierung aufgehoben: {Name}", name);
        }

        private async Task SaveAllAsync()
        {
            // Job-IDs für alle Hotkeys aktualisieren
            foreach (var hotkey in Items)
            {
                UpdateJobIdFromJobName(hotkey);
            }

            var domain = Items.Select(i => i.ToDomain()).ToList();
            await _repositoryService.SaveAllAsync(domain);
            _log.LogInformation("Hotkeys gespeichert: {Count}", domain.Count);

            await _capture.ReloadFromRepositoryAsync();
        }

        private void SaveActiveNoEditor(object? sender, PropertyChangedEventArgs e)
        {
            SaveActiveNoEditor();
        }

        private async Task SaveActiveNoEditor()
        {
            if(Selected == null) { return; }
            if(IsEditing == true) { return; }

            var error = ValidateEdited(Selected);
            if (error != null) { _log.LogWarning("Hotkey ungültig: {Error}", error); return; }

            // Job-ID automatisch setzen basierend auf Job-Namen
            UpdateJobIdFromJobName(Selected);
            
            // Nach dem Update: Sicherstellen, dass Action.Name der aktuelle Job-Name ist
            if (Selected.Action.JobId.HasValue)
            {
                var currentJobName = GetCurrentJobNameForAction(Selected.Action);
                if (!currentJobName.StartsWith("[Job nicht gefunden"))
                {
                    Selected.Action.Name = currentJobName;
                }
            }

            await _repositoryService.SaveAsync(Selected.ToDomain());
            _log.LogInformation("Hotkey gespeichert");
            await _capture.ReloadFromRepositoryAsync();
        }

        private void UpdateAvailableActionsFromJobs()
        {
            // Aktuelle Action merken, falls wir gerade editieren
            string? currentEditedAction = EditedHotkey?.Action?.Name;
            
            // Neue Job-Namen sammeln
            var newActions = new List<string>();
            
            // Jobs hinzufügen
            foreach (var job in Jobs.OrderBy(j => j.Name))
            {
                newActions.Add(job.Name);
            }
            
            // Aktuelle Action hinzufügen, falls sie nicht bereits in der Job-Liste ist
            if (!string.IsNullOrWhiteSpace(currentEditedAction) && 
                !newActions.Contains(currentEditedAction))
            {
                newActions.Add(currentEditedAction);
            }
            
            // Liste synchronisieren ohne Clear() zu verwenden
            // Zuerst alle entfernen, die nicht mehr da sein sollen
            for (int i = AvailableActions.Count - 1; i >= 0; i--)
            {
                if (!newActions.Contains(AvailableActions[i]))
                {
                    AvailableActions.RemoveAt(i);
                }
            }
            
            // Dann neue hinzufügen
            foreach (var action in newActions.OrderBy(a => a))
            {
                if (!AvailableActions.Contains(action))
                {
                    AvailableActions.Add(action);
                }
            }
        }

        private async void LoadJobs()
        {
            await _executor.ReloadJobsAsync();

            var previousJobCount = Jobs.Count;
            Jobs.Clear();
            foreach (var j in _executor.AllJobs.Values.OrderBy(j => j.Name))
                Jobs.Add(j);

            UpdateAvailableActionsFromJobs();
            
            // Alle Hotkey-Actions über geänderte Job-Namen benachrichtigen
            foreach (var hotkey in Items)
            {
                hotkey.Action.SetJobNameResolver(GetCurrentJobNameForAction);
            }
            
            // Nur loggen wenn sich etwas geändert hat
            if (Jobs.Count != previousJobCount)
            {
                _log.LogDebug("Jobs neu geladen: {Count} Jobs, {HotkeyCount} Hotkeys aktualisiert", Jobs.Count, Items.Count);
            }
        }

        private static string? ValidateEdited(EditableHotkey hk)
        {
            if (string.IsNullOrWhiteSpace(hk.Name)) return "Name ist erforderlich.";
            if (string.IsNullOrWhiteSpace(hk.Action?.Name)) return "Action ist erforderlich.";
            if (hk.VirtualKeyCode == 0) return "Bitte eine Tastenkombination erfassen.";
            return null;
        }

        private async Task<string> GenerateUniqueHotkeyNameAsync(string baseName)
        {
            var existingHotkeys = await _repositoryService.LoadAllAsync<HotkeyDefinition>();
            var existingNames = existingHotkeys.Select(h => h.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var uniqueName = baseName;
            var counter = 1;

            while (existingNames.Contains(uniqueName))
            {
                uniqueName = $"{baseName} ({counter})";
                counter++;
            }

            return uniqueName;
        }

        private async Task EnsureUniqueNameForEditedHotkeyAsync()
        {
            if (EditedHotkey == null) return;

            // Nur bei neuen Hotkeys Namen eindeutig machen
            if (!_isNew) return;

            var existingHotkeys = await _repositoryService.LoadAllAsync<HotkeyDefinition>();
            var existingNames = existingHotkeys
                .Select(h => h.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var originalName = EditedHotkey.Name;
            var uniqueName = originalName;
            var counter = 1;

            while (existingNames.Contains(uniqueName))
            {
                uniqueName = $"{originalName} ({counter})";
                counter++;
            }

            if (uniqueName != originalName)
            {
                EditedHotkey.Name = uniqueName;
                _log.LogInformation("Hotkey-Name wurde eindeutig gemacht: '{OriginalName}' -> '{UniqueName}'", originalName, uniqueName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
