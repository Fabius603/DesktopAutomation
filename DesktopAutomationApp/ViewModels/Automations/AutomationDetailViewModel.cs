using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Models;
using Microsoft.Extensions.Logging;
using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class AutomationDetailViewModel : ViewModelBase, INavigationGuard
    {
        public record ActionItem(string Name, Guid Id, string Category);

        private readonly IAutomationApplicationService _automationAppService;
        private readonly IDialogService _dialogService;
        private readonly IJobApplicationService _jobAppService;
        private readonly IMakroApplicationService _makroAppService;
        private readonly ILogger<AutomationDetailViewModel> _log;
        private readonly EditableAutomation _snapshot;
        private readonly bool _isNew;

        private bool _hasUnsavedChanges;
        private ActionItem? _selectedAction;

        public EditableAutomation EditedAutomation { get; }
        public string Title => EditedAutomation.Name;
        public ObservableCollection<AutomationTriggerKind> TriggerKinds { get; } = new();
        public ObservableCollection<ActionCommand> AvailableCommands { get; } = new();
        public ObservableCollection<AutomationAlreadyRunningBehavior> RunningBehaviors { get; } = new();
        public ObservableCollection<IntervalUnit> IntervalUnits { get; } = new();
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
                        EditedAutomation.Action.ActionType = HotkeyActionType.Makro;
                        EditedAutomation.Action.MakroId = value.Id;
                        EditedAutomation.Action.JobId = null;
                    }
                    else
                    {
                        EditedAutomation.Action.ActionType = HotkeyActionType.Job;
                        EditedAutomation.Action.JobId = value.Id;
                        EditedAutomation.Action.MakroId = null;
                    }

                    EditedAutomation.Action.Name = value.Name;
                    OnPropertyChanged(nameof(EditedAutomation.DisplayAction));
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

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand CaptureHotkeyCommand { get; }

        public event Action? RequestBack;

        public AutomationDetailViewModel(
            EditableAutomation automation,
            IAutomationApplicationService automationAppService,
            IDialogService dialogService,
            IJobApplicationService jobAppService,
            IMakroApplicationService makroAppService,
            ILogger<AutomationDetailViewModel> log)
        {
            EditedAutomation = automation ?? throw new ArgumentNullException(nameof(automation));
            _automationAppService = automationAppService;
            _dialogService = dialogService;
            _jobAppService = jobAppService;
            _makroAppService = makroAppService;
            _log = log;
            _isNew = automation.CreatedAt == automation.UpdatedAt && string.IsNullOrWhiteSpace(automation.Action.Name);
            _snapshot = automation.Clone();

            foreach (AutomationTriggerKind kind in Enum.GetValues(typeof(AutomationTriggerKind)))
                TriggerKinds.Add(kind);
            foreach (ActionCommand command in Enum.GetValues(typeof(ActionCommand)))
                AvailableCommands.Add(command);
            foreach (AutomationAlreadyRunningBehavior behavior in Enum.GetValues(typeof(AutomationAlreadyRunningBehavior)))
                RunningBehaviors.Add(behavior);
            foreach (IntervalUnit unit in Enum.GetValues(typeof(IntervalUnit)))
                IntervalUnits.Add(unit);

            ActionsView = new ListCollectionView(Actions);
            ActionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ActionItem.Category)));

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(() => { if (!_isNew) DiscardChanges(); }, () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(async () => await RenameAsync());
            CaptureHotkeyCommand = new RelayCommand(CaptureHotkeyDummy);

            EditedAutomation.PropertyChanged += OnEditedAutomationChanged;
            EditedAutomation.Action.PropertyChanged += OnEditedActionChanged;

            LoadActions();
            ResolveSelectedAction();
            HasUnsavedChanges = _isNew;
        }

        private void LoadActions()
        {
            Actions.Clear();
            foreach (var job in _jobAppService.Jobs.Values.OrderBy(j => j.Name))
                Actions.Add(new ActionItem(job.Name, job.Id, "Job"));
            foreach (var makro in _makroAppService.Makros.Values.OrderBy(m => m.Name))
                Actions.Add(new ActionItem(makro.Name, makro.Id, "Makro"));

            EditedAutomation.Action.SetJobNameResolver(GetCurrentActionName);
        }

        private void ResolveSelectedAction()
        {
            if (EditedAutomation.Action.ActionType == HotkeyActionType.Makro && EditedAutomation.Action.MakroId.HasValue)
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

        private string GetCurrentActionName(EditableJobReference action)
        {
            if (action.ActionType == HotkeyActionType.Makro && action.MakroId.HasValue)
            {
                var makro = Actions.FirstOrDefault(a => a.Category == "Makro" && a.Id == action.MakroId.Value);
                return makro?.Name ?? $"[Makro nicht gefunden: {action.MakroId}]";
            }

            if (action.JobId.HasValue)
            {
                var job = Actions.FirstOrDefault(a => a.Category == "Job" && a.Id == action.JobId.Value);
                return job?.Name ?? $"[Job nicht gefunden: {action.JobId}]";
            }

            return action.Name;
        }

        private void CaptureHotkeyDummy()
        {
            // TODO Automation: Durch echte Hotkey-Erfassung über GlobalHotkeyService.CaptureNextAsync ersetzen.
            EditedAutomation.Modifiers = KeyModifiers.Control | KeyModifiers.Alt;
            EditedAutomation.VirtualKeyCode = 0x41;
            HasUnsavedChanges = true;
        }

        private async Task RenameAsync()
        {
            var newName = await _dialogService.AskForNameAsync("Umbenennen", "Neuer Name:", EditedAutomation.Name);
            if (newName == null) return;

            EditedAutomation.Name = newName.Trim();
            OnPropertyChanged(nameof(Title));
            HasUnsavedChanges = true;
        }

        public async Task SaveAsync()
        {
            var error = ValidateEdited();
            if (error != null)
            {
                _dialogService.ShowError(error, "Validierungsfehler");
                return;
            }

            // TODO Automation: Speichern ruft aktuell nur den Dummy-/Repository-Service auf, keine Runtime-Registrierung.
            await _automationAppService.SaveAsync(EditedAutomation.ToDomain());
            _log.LogInformation("Automation gespeichert: {Name}", EditedAutomation.Name);
            HasUnsavedChanges = false;
        }

        public void DiscardChanges()
        {
            CopyFrom(_snapshot, EditedAutomation);
            ResolveSelectedAction();
            HasUnsavedChanges = false;
        }

        private string? ValidateEdited()
        {
            if (string.IsNullOrWhiteSpace(EditedAutomation.Name)) return "Name ist erforderlich.";
            if (SelectedAction == null) return "Bitte eine Aktion (Job oder Makro) auswählen.";
            if (EditedAutomation.TriggerKind == AutomationTriggerKind.Hotkey && EditedAutomation.VirtualKeyCode == 0)
                return "Bitte einen Hotkey erfassen.";
            if (EditedAutomation.IsProcessTrigger && string.IsNullOrWhiteSpace(EditedAutomation.ProcessName))
                return "Bitte einen Prozessnamen angeben.";
            if ((EditedAutomation.IsOnceAtTrigger && string.IsNullOrWhiteSpace(EditedAutomation.RunAtTime))
                || (EditedAutomation.IsScheduleTrigger && string.IsNullOrWhiteSpace(EditedAutomation.ScheduleTime)))
                return "Bitte eine Uhrzeit angeben.";
            if (EditedAutomation.IsIntervalTrigger && EditedAutomation.IntervalValue <= 0)
                return "Das Intervall muss größer als 0 sein.";
            return null;
        }

        private void OnEditedAutomationChanged(object? sender, PropertyChangedEventArgs e)
        {
            HasUnsavedChanges = true;
            if (e.PropertyName is nameof(EditableAutomation.TriggerKind)
                or nameof(EditableAutomation.Modifiers)
                or nameof(EditableAutomation.VirtualKeyCode)
                or nameof(EditableAutomation.ProcessName)
                or nameof(EditableAutomation.WindowTitleContains)
                or nameof(EditableAutomation.IntervalValue)
                or nameof(EditableAutomation.IntervalUnit)
                or nameof(EditableAutomation.RunAtDate)
                or nameof(EditableAutomation.RunAtTime)
                or nameof(EditableAutomation.ScheduleTime))
            {
                OnPropertyChanged(nameof(EditedAutomation.DisplayTrigger));
            }
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
            target.RunAtDate = source.RunAtDate;
            target.RunAtTime = source.RunAtTime;
            target.ScheduleTime = source.ScheduleTime;
            target.IntervalValue = source.IntervalValue;
            target.IntervalUnit = source.IntervalUnit;
            target.StartImmediately = source.StartImmediately;
            target.ProcessName = source.ProcessName;
            target.WindowTitleContains = source.WindowTitleContains;
            target.DelayAfterEventSeconds = source.DelayAfterEventSeconds;
            target.Action.Name = source.Action.Name;
            target.Action.JobId = source.Action.JobId;
            target.Action.MakroId = source.Action.MakroId;
            target.Action.Command = source.Action.Command;
            target.Action.ActionType = source.Action.ActionType;
            target.AlreadyRunningBehavior = source.AlreadyRunningBehavior;
            target.CooldownSeconds = source.CooldownSeconds;
        }

        private void InvalidateAllCommands()
        {
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
