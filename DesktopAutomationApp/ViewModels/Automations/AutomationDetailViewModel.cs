using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Models;
using Microsoft.Extensions.Logging;
using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services;
using DesktopAutomationApp.ViewModels.WindowsIntegration;
using TaskAutomation.WindowsIntegration;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class AutomationDetailViewModel : ViewModelBase, INavigationGuard
    {
        public record ActionItem(string Name, Guid Id, string Category)
        {
            public string DisplayCategory => Category == "Makro" ? Loc.Get("Common.Macro") : Loc.Get("Common.Job");
        }

        private readonly IAutomationApplicationService _automationAppService;
        private readonly IDialogService _dialogService;
        private readonly IJobApplicationService _jobAppService;
        private readonly IMakroApplicationService _makroAppService;
        private readonly IGlobalHotkeyService _hotkeyService;
        private readonly ILogger<AutomationDetailViewModel> _log;
        private readonly EditableAutomation _snapshot;
        private readonly bool _isNew;
        private readonly EditorChangeTracker<AutomationEditState> _changeTracker;
        private readonly Stack<EditableAutomation> _undoStack = new();
        private readonly Stack<EditableAutomation> _redoStack = new();
        private EditableAutomation _historyCurrent;
        private bool _baselineAccepted;
        private bool _suppressDirtyTracking;
        private bool _suppressHistoryTracking;

        private bool _hasUnsavedChanges;
        private readonly HashSet<string> _invalidDateTimeInputs = new();
        private ActionItem? _selectedAction;

        public EditableAutomation EditedAutomation { get; }
        public string Title => EditedAutomation.Name;
        public ObservableCollection<AutomationTriggerKind> TriggerKinds { get; } = new();
        public ObservableCollection<AutomationAlreadyRunningBehavior> RunningBehaviors { get; } = new();
        public ObservableCollection<IntervalUnit> IntervalUnits { get; } = new();
        public ObservableCollection<WindowAutomationEventKind> WindowEventKinds { get; } = new();
        public ObservableCollection<FileSystemAutomationEventKind> FileSystemEventKinds { get; } = new();
        public ObservableCollection<SystemAutomationEventKind> SystemEventKinds { get; } = new();
        public ObservableCollection<WebhookNetworkMode> WebhookNetworkModes { get; } = new();
        public ObservableCollection<ActionItem> Actions { get; } = new();
        public ObservableCollection<string> AvailableProcessNames { get; } = new();
        public WindowsCapabilityPickerViewModel WindowsEventPicker { get; }
        public ListCollectionView ActionsView { get; }

        public ActionItem? SelectedAction
        {
            get => _selectedAction;
            set
            {
                RunHistoryTransaction(() =>
                {
                    _selectedAction = value;
                    if (value != null)
                    {
                        if (value.Category == "Makro")
                        {
                            EditedAutomation.Action.ActionType = AutomationActionTarget.Makro;
                            EditedAutomation.Action.MakroId = value.Id;
                            EditedAutomation.Action.JobId = null;
                        }
                        else
                        {
                            EditedAutomation.Action.ActionType = AutomationActionTarget.Job;
                            EditedAutomation.Action.JobId = value.Id;
                            EditedAutomation.Action.MakroId = null;
                        }

                        EditedAutomation.Action.Name = value.Name;
                        OnPropertyChanged(nameof(EditedAutomation.DisplayAction));
                    }
                    else
                    {
                        EditedAutomation.Action.JobId = null;
                        EditedAutomation.Action.MakroId = null;
                        EditedAutomation.Action.Name = string.Empty;
                    }
                });

                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                if (_hasUnsavedChanges == value) return;
                _hasUnsavedChanges = value;
                OnPropertyChanged();
                InvalidateAllCommands();
            }
        }

        private bool _isCapturingHotkey;
        public bool IsCapturingHotkey
        {
            get => _isCapturingHotkey;
            private set
            {
                if (_isCapturingHotkey == value) return;
                SetProperty(ref _isCapturingHotkey, value);
                OnPropertyChanged(nameof(HotkeyCaptureStatus));
                (CaptureHotkeyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        public string HotkeyCaptureStatus => IsCapturingHotkey ? Loc.Get("Automation.Hotkey.CapturePrompt") : string.Empty;
        public string TriggerDescription => EditedAutomation.IsWindowsEventTrigger
            ? WindowsEventPicker.SelectedCapability?.DisplayName ?? "Windows-Ereignis auswählen"
            : Loc.Get($"Automation.Trigger.Description.{EditedAutomation.TriggerKind}");

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand TriggerCommand { get; }
        public ICommand CaptureHotkeyCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand BrowseFileSystemFolderCommand { get; }
        public ICommand ClearActiveWindowCommand { get; }
        public ICommand CopyWebhookUrlCommand { get; }
        public ICommand CopyWebhookSecretCommand { get; }
        public ICommand CopyWebhookPowerShellCommand { get; }
        public ICommand RegenerateWebhookSecretCommand { get; }

        public event Action? RequestBack;

        public AutomationDetailViewModel(
            EditableAutomation automation,
            IAutomationApplicationService automationAppService,
            IDialogService dialogService,
            IJobApplicationService jobAppService,
            IMakroApplicationService makroAppService,
            IGlobalHotkeyService hotkeyService,
            IWindowsCapabilityCatalog windowsCatalog,
            ILogger<AutomationDetailViewModel> log)
        {
            EditedAutomation = automation ?? throw new ArgumentNullException(nameof(automation));
            _automationAppService = automationAppService;
            _dialogService = dialogService;
            _jobAppService = jobAppService;
            _makroAppService = makroAppService;
            _hotkeyService = hotkeyService;
            _log = log;
            _isNew = automation.CreatedAt == automation.UpdatedAt && string.IsNullOrWhiteSpace(automation.Action.Name);
            _snapshot = automation.Clone();
            _historyCurrent = automation.Clone();
            _baselineAccepted = !_isNew;
            _changeTracker = new EditorChangeTracker<AutomationEditState>(
                CaptureEditState(EditedAutomation),
                static (baseline, current, _) => Task.FromResult(baseline == current),
                isDirty => HasUnsavedChanges = isDirty);

            WindowsEventPicker = new WindowsCapabilityPickerViewModel(windowsCatalog, WindowsCapabilityPickerMode.Event,
                automation.WindowsEventType, automation.WindowsEventFilters);
            WindowsEventPicker.Changed += OnWindowsEventPickerChanged;

            foreach (var kind in new[] { AutomationTriggerKind.Hotkey, AutomationTriggerKind.OnceAt,
                         AutomationTriggerKind.Schedule, AutomationTriggerKind.Interval, AutomationTriggerKind.WindowsEvent,
                         AutomationTriggerKind.Webhook })
                TriggerKinds.Add(kind);
            foreach (IntervalUnit unit in Enum.GetValues(typeof(IntervalUnit)))
                IntervalUnits.Add(unit);
            foreach (WindowAutomationEventKind kind in Enum.GetValues(typeof(WindowAutomationEventKind)))
                WindowEventKinds.Add(kind);
            foreach (FileSystemAutomationEventKind kind in Enum.GetValues(typeof(FileSystemAutomationEventKind)))
                FileSystemEventKinds.Add(kind);
            foreach (SystemAutomationEventKind kind in Enum.GetValues(typeof(SystemAutomationEventKind)))
                SystemEventKinds.Add(kind);
            foreach (WebhookNetworkMode mode in Enum.GetValues(typeof(WebhookNetworkMode)))
                WebhookNetworkModes.Add(mode);

            foreach (AutomationAlreadyRunningBehavior behavior in Enum.GetValues(typeof(AutomationAlreadyRunningBehavior)))
                RunningBehaviors.Add(behavior);

            ActionsView = new ListCollectionView(Actions);
            ActionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ActionItem.DisplayCategory)));

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => HasUnsavedChanges && _invalidDateTimeInputs.Count == 0);
            CancelCommand = new RelayCommand(async () => await ConfirmDiscardChangesAsync(), () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(async () => await RenameAsync());
            OpenFileCommand = new RelayCommand(OpenFileInExplorer);
            TriggerCommand = new RelayCommand(async () => await _automationAppService.TriggerAsync(EditedAutomation.Id),
                () => !_isNew && EditedAutomation.Active && !HasUnsavedChanges);
            CaptureHotkeyCommand = new RelayCommand(
                async () => await CaptureHotkeyAsync(),
                () => EditedAutomation.TriggerKind == AutomationTriggerKind.Hotkey && !IsCapturingHotkey);
            UndoCommand = new RelayCommand(Undo, () => _undoStack.Count > 0);
            RedoCommand = new RelayCommand(Redo, () => _redoStack.Count > 0);
            BrowseFileSystemFolderCommand = new RelayCommand(BrowseFileSystemFolder);
            ClearActiveWindowCommand = new RelayCommand(() =>
            {
                RunHistoryTransaction(() =>
                {
                    EditedAutomation.EnabledFrom = null;
                    EditedAutomation.EnabledUntil = null;
                });
                ReportDateTimeInputValidity("EnabledFrom", true);
                ReportDateTimeInputValidity("EnabledUntil", true);
            });
            CopyWebhookUrlCommand = new RelayCommand(() => CopyToClipboard(EditedAutomation.WebhookUrl));
            CopyWebhookSecretCommand = new RelayCommand(() => CopyToClipboard(EditedAutomation.WebhookSecret));
            CopyWebhookPowerShellCommand = new RelayCommand(() => CopyToClipboard(EditedAutomation.WebhookPowerShellCode));
            RegenerateWebhookSecretCommand = new RelayCommand(() =>
            {
                EditedAutomation.WebhookSecret = WebhookAutomationTrigger.GenerateSecret();
                UpdateDirtyState();
            });

            EditedAutomation.PropertyChanged += OnEditedAutomationChanged;
            EditedAutomation.Action.PropertyChanged += OnEditedActionChanged;
            LocalizationService.Instance.CultureChanged += OnCultureChanged;

            LoadActions();
            _ = LoadInstalledProgramsAsync();
            ResolveSelectedAction();
            HasUnsavedChanges = _isNew;
        }

        private void OpenFileInExplorer()
        {
            var directory = _automationAppService.GetStoragePath();
            var path = Common.JsonRepository.JsonRepositoryPath.ForKey(directory, EditedAutomation.Id.ToString());
            Directory.CreateDirectory(directory);
            if (!File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
                return;
            }

            var startInfo = new ProcessStartInfo("notepad.exe") { UseShellExecute = true };
            startInfo.ArgumentList.Add(path);
            Process.Start(startInfo);
        }

        private async Task LoadInstalledProgramsAsync()
        {
            try
            {
                var programs = await InstalledProgramDiscovery.DiscoverAsync();
                foreach (var processName in programs
                             .Select(program => program.ProcessName)
                             .Where(name => !string.IsNullOrWhiteSpace(name))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase))
                    AvailableProcessNames.Add(processName);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
        }

        public void ReportDateTimeInputValidity(string inputId, bool isValid)
        {
            if (isValid) _invalidDateTimeInputs.Remove(inputId);
            else _invalidDateTimeInputs.Add(inputId);
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void LoadActions()
        {
            Actions.Clear();
            foreach (var job in _jobAppService.Jobs.Values.OrderBy(j => j.Name))
                Actions.Add(new ActionItem(job.Name, job.Id, "Job"));
            foreach (var makro in _makroAppService.Makros.Values.OrderBy(m => m.Name))
                Actions.Add(new ActionItem(makro.Name, makro.Id, "Makro"));

        }

        private void ResolveSelectedAction()
        {
            if (EditedAutomation.Action.ActionType == AutomationActionTarget.Makro && EditedAutomation.Action.MakroId.HasValue)
            {
                _selectedAction = Actions.FirstOrDefault(a => a.Category == "Makro" && a.Id == EditedAutomation.Action.MakroId.Value);
            }
            else if (EditedAutomation.Action.JobId.HasValue)
            {
                _selectedAction = Actions.FirstOrDefault(a => a.Category == "Job" && a.Id == EditedAutomation.Action.JobId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(EditedAutomation.Action.Name))
            {
                _selectedAction = Actions.FirstOrDefault(a =>
                    string.Equals(a.Name, EditedAutomation.Action.Name, StringComparison.OrdinalIgnoreCase));
            }

            OnPropertyChanged(nameof(SelectedAction));
        }

        private async Task CaptureHotkeyAsync()
        {
            try
            {
                IsCapturingHotkey = true;
                var captured = await _hotkeyService.CaptureNextAsync();
                RunHistoryTransaction(() =>
                {
                    EditedAutomation.Modifiers = captured.Modifiers;
                    EditedAutomation.VirtualKeyCode = captured.VirtualKeyCode;
                });
                UpdateDirtyState();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Hotkey konnte nicht erfasst werden.");
                _dialogService.ShowError(Loc.Get("Automation.Hotkey.CaptureError"), Loc.Get("Automation.Hotkey.CaptureTitle"));
            }
            finally
            {
                IsCapturingHotkey = false;
            }
        }

        private async Task RenameAsync()
        {
            var newName = await _dialogService.AskForNameAsync(Loc.Get("Common.Rename"), Loc.Get("Dialog.NewName"), EditedAutomation.Name);
            if (newName == null) return;

            EditedAutomation.Name = newName.Trim();
            OnPropertyChanged(nameof(Title));
            UpdateDirtyState();
        }

        private void BrowseFileSystemFolder()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = Loc.Get("Automation.FileSystem.SelectFolder"),
                UseDescriptionForTitle = true,
                SelectedPath = Environment.ExpandEnvironmentVariables(EditedAutomation.FileSystemPath)
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                EditedAutomation.FileSystemPath = dialog.SelectedPath;
        }

        public async Task SaveAsync()
        {
            SyncWindowsEvent();
            if (EditedAutomation.IsWindowsEventTrigger && !WindowsEventPicker.IsValid)
            {
                _dialogService.ShowError("Bitte wähle ein Windows-Ereignis und fülle alle Pflichtfelder aus.", Loc.Get("Validation.Title"));
                return;
            }
            var automation = EditedAutomation.ToDomain();
            var validation = AutomationValidation.Validate(automation);
            if (!validation.IsValid)
            {
                _dialogService.ShowError(LocalizeValidationError(validation.Error), Loc.Get("Validation.Title"));
                return;
            }

            await _automationAppService.SaveAsync(automation);
            _log.LogInformation("Automation gespeichert: {Name}", EditedAutomation.Name);
            CopyFrom(EditedAutomation, _snapshot);
            _baselineAccepted = true;
            _changeTracker.Accept(CaptureEditState(EditedAutomation));
        }

        public void DiscardChanges()
        {
            _suppressDirtyTracking = true;
            try
            {
                CopyFrom(_snapshot, EditedAutomation);
                WindowsEventPicker.Load(_snapshot.WindowsEventType, _snapshot.WindowsEventFilters);
                ResolveSelectedAction();
            }
            finally
            {
                _suppressDirtyTracking = false;
            }

            _changeTracker.Accept(CaptureEditState(EditedAutomation));
            _historyCurrent = EditedAutomation.Clone();
            _undoStack.Clear();
            _redoStack.Clear();
            InvalidateHistoryCommands();
        }

        private async Task ConfirmDiscardChangesAsync()
        {
            if (await _dialogService.ConfirmAsync(
                    Loc.Get("Dialog.Discard.Message"),
                    Loc.Get("Dialog.Discard.Title")))
                DiscardChanges();
        }

        private static string LocalizeValidationError(AutomationValidationError error) => error switch
        {
            AutomationValidationError.NameRequired => Loc.Get("Validation.NameRequired"),
            AutomationValidationError.ActionRequired => Loc.Get("Validation.ActionRequired"),
            AutomationValidationError.HotkeyRequired => Loc.Get("Validation.HotkeyRequired"),
            AutomationValidationError.ProcessNameRequired => Loc.Get("Validation.ProcessNameRequired"),
            AutomationValidationError.WindowFilterRequired => Loc.Get("Validation.WindowFilterRequired"),
            AutomationValidationError.FolderRequired => Loc.Get("Validation.FolderRequired"),
            AutomationValidationError.FolderNotFound => Loc.Get("Validation.FolderNotFound"),
            AutomationValidationError.FileFilterRequired => Loc.Get("Validation.FileFilterRequired"),
            AutomationValidationError.WeekdayRequired => Loc.Get("Validation.WeekdayRequired"),
            AutomationValidationError.IntervalPositive => Loc.Get("Validation.IntervalPositive"),
            AutomationValidationError.ActiveWindowPair => Loc.Get("Validation.ActiveWindowPair"),
            AutomationValidationError.WindowsEventRequired => "Bitte wähle ein Windows-Ereignis aus.",
            AutomationValidationError.WebhookConfigurationInvalid => Loc.Get("Validation.WebhookConfigurationInvalid"),
            _ => Loc.Get("Validation.Title")
        };

#if false // Fachregeln liegen in TaskAutomation.AutomationValidation.
        private string? ValidateEdited()
        {
            if (string.IsNullOrWhiteSpace(EditedAutomation.Name)) return Loc.Get("Validation.NameRequired");
            if (SelectedAction == null) return Loc.Get("Validation.ActionRequired");
            if (EditedAutomation.TriggerKind == AutomationTriggerKind.Hotkey && EditedAutomation.VirtualKeyCode == 0)
                return Loc.Get("Validation.HotkeyRequired");
            if (EditedAutomation.IsProcessTrigger && string.IsNullOrWhiteSpace(EditedAutomation.ProcessName))
                return Loc.Get("Validation.ProcessNameRequired");
            if (EditedAutomation.IsWindowEventTrigger
                && string.IsNullOrWhiteSpace(EditedAutomation.ProcessName)
                && string.IsNullOrWhiteSpace(EditedAutomation.WindowTitleContains))
                return Loc.Get("Validation.WindowFilterRequired");
            if (EditedAutomation.IsFileSystemEventTrigger)
            {
                if (string.IsNullOrWhiteSpace(EditedAutomation.FileSystemPath))
                    return Loc.Get("Validation.FolderRequired");
                var path = Environment.ExpandEnvironmentVariables(EditedAutomation.FileSystemPath.Trim());
                if (!Directory.Exists(path))
                    return Loc.Get("Validation.FolderNotFound");
                if (string.IsNullOrWhiteSpace(EditedAutomation.FileSystemFilter))
                    return Loc.Get("Validation.FileFilterRequired");
            }
            if (EditedAutomation.IsScheduleTrigger && !(EditedAutomation.Monday || EditedAutomation.Tuesday
                || EditedAutomation.Wednesday || EditedAutomation.Thursday || EditedAutomation.Friday
                || EditedAutomation.Saturday || EditedAutomation.Sunday))
                return Loc.Get("Validation.WeekdayRequired");
            if (EditedAutomation.IsIntervalTrigger && EditedAutomation.IntervalValue <= 0)
                return Loc.Get("Validation.IntervalPositive");
            if (EditedAutomation.EnabledFrom.HasValue != EditedAutomation.EnabledUntil.HasValue)
                return Loc.Get("Validation.ActiveWindowPair");
            return null;
        }
#endif

        private void OnEditedAutomationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(EditableAutomation.DisplayTrigger)
                or nameof(EditableAutomation.DisplayAction)
                or nameof(EditableAutomation.NextRunDisplay)
                or nameof(EditableAutomation.LastRunDisplay))
                return;

            RecordHistoryChange();
            UpdateDirtyState();
            if (e.PropertyName is nameof(EditableAutomation.TriggerKind)
                or nameof(EditableAutomation.Modifiers)
                or nameof(EditableAutomation.VirtualKeyCode)
                or nameof(EditableAutomation.ProcessName)
                or nameof(EditableAutomation.WindowTitleContains)
                or nameof(EditableAutomation.IntervalValue)
                or nameof(EditableAutomation.IntervalUnit)
                or nameof(EditableAutomation.RunAt)
                or nameof(EditableAutomation.ScheduleTime))
            {
                OnPropertyChanged(nameof(EditedAutomation.DisplayTrigger));
            }
            if (e.PropertyName == nameof(EditableAutomation.TriggerKind))
            {
                OnPropertyChanged(nameof(TriggerDescription));
                (CaptureHotkeyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void OnEditedActionChanged(object? sender, PropertyChangedEventArgs e)
        {
            RecordHistoryChange();
            UpdateDirtyState();
            OnPropertyChanged(nameof(EditedAutomation.DisplayAction));
        }

        private void OnWindowsEventPickerChanged()
        {
            RunHistoryTransaction(SyncWindowsEvent);
            UpdateDirtyState();
            OnPropertyChanged(nameof(TriggerDescription));
        }

        private void UpdateDirtyState()
        {
            if (_suppressDirtyTracking)
                return;

            if (!_baselineAccepted)
            {
                HasUnsavedChanges = true;
                return;
            }

            _changeTracker.Evaluate(CaptureEditState(EditedAutomation));
        }

        internal Task WaitForDirtyStateAsync() => _changeTracker.WhenIdleAsync();

        private void RecordHistoryChange()
        {
            if (_suppressHistoryTracking)
                return;

            var currentState = CaptureEditState(EditedAutomation);
            if (currentState == CaptureEditState(_historyCurrent))
                return;

            _undoStack.Push(_historyCurrent.Clone());
            _historyCurrent = EditedAutomation.Clone();
            _redoStack.Clear();
            InvalidateHistoryCommands();
        }

        private void RunHistoryTransaction(Action change)
        {
            var wasSuppressed = _suppressHistoryTracking;
            _suppressHistoryTracking = true;
            try
            {
                change();
            }
            finally
            {
                _suppressHistoryTracking = wasSuppressed;
            }

            if (!wasSuppressed)
                RecordHistoryChange();
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;
            _redoStack.Push(EditedAutomation.Clone());
            RestoreHistorySnapshot(_undoStack.Pop());
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Push(EditedAutomation.Clone());
            RestoreHistorySnapshot(_redoStack.Pop());
        }

        private void RestoreHistorySnapshot(EditableAutomation snapshot)
        {
            _suppressHistoryTracking = true;
            _suppressDirtyTracking = true;
            try
            {
                CopyFrom(snapshot, EditedAutomation);
                WindowsEventPicker.Load(snapshot.WindowsEventType, snapshot.WindowsEventFilters);
                ResolveSelectedAction();
                _historyCurrent = EditedAutomation.Clone();
            }
            finally
            {
                _suppressDirtyTracking = false;
                _suppressHistoryTracking = false;
            }

            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(EditedAutomation.DisplayTrigger));
            OnPropertyChanged(nameof(EditedAutomation.DisplayAction));
            OnPropertyChanged(nameof(TriggerDescription));
            UpdateDirtyState();
            InvalidateHistoryCommands();
        }

        private void InvalidateHistoryCommands()
        {
            (UndoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RedoCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private static AutomationEditState CaptureEditState(EditableAutomation automation)
        {
            var definition = automation.ToDomain();
            return new AutomationEditState(
                definition.Id,
                definition.Name,
                definition.Description,
                definition.Active,
                CaptureTriggerState(definition.Trigger),
                definition.Action.Name,
                definition.Action.JobId,
                definition.Action.MakroId,
                definition.Action.ActionType,
                definition.RunPolicy.AlreadyRunningBehavior,
                definition.RunPolicy.Cooldown,
                definition.RunPolicy.EnabledFrom,
                definition.RunPolicy.EnabledUntil,
                definition.CreatedAt,
                definition.UpdatedAt,
                definition.LastRunAt);
        }

        private static string CaptureTriggerState(AutomationTrigger trigger) => trigger switch
        {
            ScheduleAutomationTrigger schedule =>
                $"{schedule.Kind}|{schedule.TimeOfDay:O}|{string.Join(',', schedule.Days.OrderBy(day => day))}",
            WindowsEventAutomationTrigger windowsEvent =>
                $"{windowsEvent.Kind}|{windowsEvent.EventType}|{windowsEvent.Debounce:c}|{windowsEvent.DelayAfterEvent:c}|"
                + string.Join('|', windowsEvent.Filters
                    .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(pair => $"{pair.Key.Length}:{pair.Key}={pair.Value?.Length ?? -1}:{pair.Value}")),
            _ => System.Text.Json.JsonSerializer.Serialize(trigger, trigger.GetType())
        };

        private sealed record AutomationEditState(
            Guid Id,
            string Name,
            string Description,
            bool Active,
            string Trigger,
            string ActionName,
            Guid? JobId,
            Guid? MakroId,
            AutomationActionTarget ActionType,
            AutomationAlreadyRunningBehavior AlreadyRunningBehavior,
            TimeSpan Cooldown,
            TimeOnly? EnabledFrom,
            TimeOnly? EnabledUntil,
            DateTimeOffset CreatedAt,
            DateTimeOffset UpdatedAt,
            DateTimeOffset? LastRunAt);

        private void SyncWindowsEvent()
        {
            EditedAutomation.WindowsEventType = WindowsEventPicker.SelectedCapability?.Id ?? string.Empty;
            EditedAutomation.WindowsEventFilters = WindowsEventPicker.ToDictionary();
        }

        private static void CopyFrom(EditableAutomation source, EditableAutomation target)
        {
            target.Name = source.Name;
            target.Description = source.Description;
            target.Active = source.Active;
            target.TriggerKind = source.TriggerKind;
            target.Modifiers = source.Modifiers;
            target.VirtualKeyCode = source.VirtualKeyCode;
            target.HotkeyDebounceSeconds = source.HotkeyDebounceSeconds;
            target.RunAt = source.RunAt;
            target.ScheduleTime = source.ScheduleTime;
            target.Monday = source.Monday; target.Tuesday = source.Tuesday; target.Wednesday = source.Wednesday;
            target.Thursday = source.Thursday; target.Friday = source.Friday;
            target.Saturday = source.Saturday; target.Sunday = source.Sunday;
            target.IntervalValue = source.IntervalValue;
            target.IntervalUnit = source.IntervalUnit;
            target.StartImmediately = source.StartImmediately;
            target.ProcessName = source.ProcessName;
            target.WindowTitleContains = source.WindowTitleContains;
            target.DelayAfterEventSeconds = source.DelayAfterEventSeconds;
            target.WindowEventKind = source.WindowEventKind;
            target.FileSystemEventKind = source.FileSystemEventKind;
            target.FileSystemPath = source.FileSystemPath;
            target.FileSystemFilter = source.FileSystemFilter;
            target.IncludeSubdirectories = source.IncludeSubdirectories;
            target.WaitUntilFileReady = source.WaitUntilFileReady;
            target.SystemEventKind = source.SystemEventKind;
            target.WindowsEventType = source.WindowsEventType;
            target.WindowsEventFilters = new Dictionary<string, string?>(source.WindowsEventFilters, StringComparer.OrdinalIgnoreCase);
            target.WindowsEventDebounceSeconds = source.WindowsEventDebounceSeconds;
            target.WebhookId = source.WebhookId;
            target.WebhookNetworkMode = source.WebhookNetworkMode;
            target.WebhookPort = source.WebhookPort;
            target.WebhookSecret = source.WebhookSecret;
            target.WebhookOnlineBaseUrl = source.WebhookOnlineBaseUrl;
            target.Action.Name = source.Action.Name;
            target.Action.JobId = source.Action.JobId;
            target.Action.MakroId = source.Action.MakroId;
            target.Action.ActionType = source.Action.ActionType;
            target.AlreadyRunningBehavior = source.AlreadyRunningBehavior;
            target.CooldownSeconds = source.CooldownSeconds;
            target.EnabledFrom = source.EnabledFrom;
            target.EnabledUntil = source.EnabledUntil;
        }

        private void InvalidateAllCommands()
        {
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (TriggerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void CopyToClipboard(string value)
        {
            try
            {
                System.Windows.Clipboard.SetText(value);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Webhook-Wert konnte nicht in die Zwischenablage kopiert werden.");
                _dialogService.ShowError(Loc.Get("Webhook.CopyFailed"), Loc.Get("Validation.Title"));
            }
        }

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            EditedAutomation.RefreshLocalizedDisplayProperties();
            ActionsView.Refresh();
            OnPropertyChanged(nameof(HotkeyCaptureStatus));
            OnPropertyChanged(nameof(TriggerDescription));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LocalizationService.Instance.CultureChanged -= OnCultureChanged;
                EditedAutomation.PropertyChanged -= OnEditedAutomationChanged;
                EditedAutomation.Action.PropertyChanged -= OnEditedActionChanged;
                _changeTracker.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
