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

        private bool _hasUnsavedChanges;
        private ActionItem? _selectedAction;

        public EditableAutomation EditedAutomation { get; }
        public string Title => EditedAutomation.Name;
        public ObservableCollection<AutomationTriggerKind> TriggerKinds { get; } = new();
        public ObservableCollection<AutomationAlreadyRunningBehavior> RunningBehaviors { get; } = new();
        public ObservableCollection<IntervalUnit> IntervalUnits { get; } = new();
        public ObservableCollection<WindowAutomationEventKind> WindowEventKinds { get; } = new();
        public ObservableCollection<FileSystemAutomationEventKind> FileSystemEventKinds { get; } = new();
        public ObservableCollection<SystemAutomationEventKind> SystemEventKinds { get; } = new();
        public ObservableCollection<ActionItem> Actions { get; } = new();
        public ListCollectionView ActionsView { get; }

        public ActionItem? SelectedAction
        {
            get => _selectedAction;
            set
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

                OnPropertyChanged();
                HasUnsavedChanges = true;
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set { _hasUnsavedChanges = value; OnPropertyChanged(); InvalidateAllCommands(); }
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
        public string TriggerDescription => Loc.Get($"Automation.Trigger.Description.{EditedAutomation.TriggerKind}");

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand CaptureHotkeyCommand { get; }
        public ICommand BrowseFileSystemFolderCommand { get; }

        public event Action? RequestBack;

        public AutomationDetailViewModel(
            EditableAutomation automation,
            IAutomationApplicationService automationAppService,
            IDialogService dialogService,
            IJobApplicationService jobAppService,
            IMakroApplicationService makroAppService,
            IGlobalHotkeyService hotkeyService,
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

            foreach (AutomationTriggerKind kind in Enum.GetValues(typeof(AutomationTriggerKind)))
                TriggerKinds.Add(kind);
            foreach (IntervalUnit unit in Enum.GetValues(typeof(IntervalUnit)))
                IntervalUnits.Add(unit);
            foreach (WindowAutomationEventKind kind in Enum.GetValues(typeof(WindowAutomationEventKind)))
                WindowEventKinds.Add(kind);
            foreach (FileSystemAutomationEventKind kind in Enum.GetValues(typeof(FileSystemAutomationEventKind)))
                FileSystemEventKinds.Add(kind);
            foreach (SystemAutomationEventKind kind in Enum.GetValues(typeof(SystemAutomationEventKind)))
                SystemEventKinds.Add(kind);

            foreach (AutomationAlreadyRunningBehavior behavior in Enum.GetValues(typeof(AutomationAlreadyRunningBehavior)))
                RunningBehaviors.Add(behavior);

            ActionsView = new ListCollectionView(Actions);
            ActionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ActionItem.DisplayCategory)));

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(() => { if (!_isNew) DiscardChanges(); }, () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(async () => await RenameAsync());
            OpenFileCommand = new RelayCommand(OpenFileInExplorer);
            CaptureHotkeyCommand = new RelayCommand(async () => await CaptureHotkeyAsync(), () => !IsCapturingHotkey);
            BrowseFileSystemFolderCommand = new RelayCommand(BrowseFileSystemFolder);

            EditedAutomation.PropertyChanged += OnEditedAutomationChanged;
            EditedAutomation.Action.PropertyChanged += OnEditedActionChanged;
            LocalizationService.Instance.CultureChanged += OnCultureChanged;

            LoadActions();
            ResolveSelectedAction();
            HasUnsavedChanges = _isNew;
        }

        private void OpenFileInExplorer()
        {
            var directory = _automationAppService.GetStoragePath();
            var path = Path.Combine(directory, $"{EditedAutomation.Id}.json");
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo(File.Exists(path) ? path : directory) { UseShellExecute = true });
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
                EditedAutomation.Modifiers = captured.Modifiers;
                EditedAutomation.VirtualKeyCode = captured.VirtualKeyCode;
                HasUnsavedChanges = true;
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
            HasUnsavedChanges = true;
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
            var automation = EditedAutomation.ToDomain();
            var validation = AutomationValidation.Validate(automation);
            if (!validation.IsValid)
            {
                _dialogService.ShowError(LocalizeValidationError(validation.Error), Loc.Get("Validation.Title"));
                return;
            }

            await _automationAppService.SaveAsync(automation);
            _log.LogInformation("Automation gespeichert: {Name}", EditedAutomation.Name);
            HasUnsavedChanges = false;
        }

        public void DiscardChanges()
        {
            CopyFrom(_snapshot, EditedAutomation);
            ResolveSelectedAction();
            HasUnsavedChanges = false;
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

            HasUnsavedChanges = true;
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
                OnPropertyChanged(nameof(TriggerDescription));
        }

        private void OnEditedActionChanged(object? sender, PropertyChangedEventArgs e)
        {
            HasUnsavedChanges = true;
            OnPropertyChanged(nameof(EditedAutomation.DisplayAction));
        }

        private static void CopyFrom(EditableAutomation source, EditableAutomation target)
        {
            target.Name = source.Name;
            target.Description = source.Description;
            target.Active = source.Active;
            target.TriggerKind = source.TriggerKind;
            target.Modifiers = source.Modifiers;
            target.VirtualKeyCode = source.VirtualKeyCode;
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
            }
            base.Dispose(disposing);
        }
    }
}
