using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.Models
{
    public sealed class EditableAutomationAction : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private Guid? _jobId;
        private AutomationActionTarget _actionType = AutomationActionTarget.Job;
        private Guid? _makroId;

        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }
        public Guid? JobId { get => _jobId; set { if (_jobId != value) { _jobId = value; OnPropertyChanged(); } } }
        public AutomationActionTarget ActionType { get => _actionType; set { if (_actionType != value) { _actionType = value; OnPropertyChanged(); } } }
        public Guid? MakroId { get => _makroId; set { if (_makroId != value) { _makroId = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class EditableAutomation : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _name = string.Empty;
        private string _description = string.Empty;
        private bool _active = true;
        private AutomationTriggerKind _triggerKind = AutomationTriggerKind.Hotkey;
        private KeyModifiers _modifiers;
        private uint _virtualKeyCode;
        private DateTime _runAt = DateTime.Today.AddHours(8);
        private DateTime _scheduleTime = DateTime.Today.AddHours(8);
        private HashSet<DayOfWeek> _scheduleDays =
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
        private int _intervalValue = 15;
        private IntervalUnit _intervalUnit = IntervalUnit.Minutes;
        private bool _startImmediately = true;
        private string _processName = string.Empty;
        private string _windowTitleContains = string.Empty;
        private int _delayAfterEventSeconds;
        private EditableAutomationAction _action = new();
        private AutomationAlreadyRunningBehavior _alreadyRunningBehavior = AutomationAlreadyRunningBehavior.Ignore;
        private int _cooldownSeconds;
        private DateTime? _enabledFrom;
        private DateTime? _enabledUntil;
        private DateTimeOffset? _lastRunAt;
        private DateTimeOffset? _nextRunAt;
        private string? _lastError;
        private DateTimeOffset _createdAt = DateTimeOffset.Now;
        private DateTimeOffset _updatedAt = DateTimeOffset.Now;

        public Guid Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }
        public string Description { get => _description; set { if (_description != value) { _description = value; OnPropertyChanged(); } } }
        public bool Active { get => _active; set { if (_active != value) { _active = value; OnPropertyChanged(); } } }

        public AutomationTriggerKind TriggerKind
        {
            get => _triggerKind;
            set
            {
                if (_triggerKind == value) return;
                _triggerKind = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHotkeyTrigger));
                OnPropertyChanged(nameof(IsOnceAtTrigger));
                OnPropertyChanged(nameof(IsScheduleTrigger));
                OnPropertyChanged(nameof(IsIntervalTrigger));
                OnPropertyChanged(nameof(IsProcessTrigger));
                OnPropertyChanged(nameof(DisplayTrigger));
            }
        }

        public bool IsHotkeyTrigger => TriggerKind == AutomationTriggerKind.Hotkey;
        public bool IsOnceAtTrigger => TriggerKind == AutomationTriggerKind.OnceAt;
        public bool IsScheduleTrigger => TriggerKind == AutomationTriggerKind.Schedule;
        public bool IsIntervalTrigger => TriggerKind == AutomationTriggerKind.Interval;
        public bool IsProcessTrigger => TriggerKind is AutomationTriggerKind.ProcessStarted or AutomationTriggerKind.ProcessExited;

        public KeyModifiers Modifiers { get => _modifiers; set { if (_modifiers != value) { _modifiers = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public uint VirtualKeyCode { get => _virtualKeyCode; set { if (_virtualKeyCode != value) { _virtualKeyCode = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public DateTime RunAt { get => _runAt; set { if (_runAt != value) { _runAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public DateTime ScheduleTime { get => _scheduleTime; set { if (_scheduleTime != value) { _scheduleTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public bool Monday { get => HasDay(DayOfWeek.Monday); set => SetDay(DayOfWeek.Monday, value); }
        public bool Tuesday { get => HasDay(DayOfWeek.Tuesday); set => SetDay(DayOfWeek.Tuesday, value); }
        public bool Wednesday { get => HasDay(DayOfWeek.Wednesday); set => SetDay(DayOfWeek.Wednesday, value); }
        public bool Thursday { get => HasDay(DayOfWeek.Thursday); set => SetDay(DayOfWeek.Thursday, value); }
        public bool Friday { get => HasDay(DayOfWeek.Friday); set => SetDay(DayOfWeek.Friday, value); }
        public bool Saturday { get => HasDay(DayOfWeek.Saturday); set => SetDay(DayOfWeek.Saturday, value); }
        public bool Sunday { get => HasDay(DayOfWeek.Sunday); set => SetDay(DayOfWeek.Sunday, value); }
        public int IntervalValue { get => _intervalValue; set { if (_intervalValue != value) { _intervalValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public IntervalUnit IntervalUnit { get => _intervalUnit; set { if (_intervalUnit != value) { _intervalUnit = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public bool StartImmediately { get => _startImmediately; set { if (_startImmediately != value) { _startImmediately = value; OnPropertyChanged(); } } }
        public string ProcessName { get => _processName; set { if (_processName != value) { _processName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public string WindowTitleContains { get => _windowTitleContains; set { if (_windowTitleContains != value) { _windowTitleContains = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public int DelayAfterEventSeconds { get => _delayAfterEventSeconds; set { if (_delayAfterEventSeconds != value) { _delayAfterEventSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }

        public EditableAutomationAction Action { get => _action; set { if (!ReferenceEquals(_action, value) && value != null) { _action = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayAction)); } } }
        public AutomationAlreadyRunningBehavior AlreadyRunningBehavior { get => _alreadyRunningBehavior; set { if (_alreadyRunningBehavior != value) { _alreadyRunningBehavior = value; OnPropertyChanged(); } } }
        public int CooldownSeconds { get => _cooldownSeconds; set { if (_cooldownSeconds != value) { _cooldownSeconds = value; OnPropertyChanged(); } } }
        public DateTime? EnabledFrom { get => _enabledFrom; set { if (_enabledFrom != value) { _enabledFrom = value; OnPropertyChanged(); } } }
        public DateTime? EnabledUntil { get => _enabledUntil; set { if (_enabledUntil != value) { _enabledUntil = value; OnPropertyChanged(); } } }
        public DateTimeOffset CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); } }
        public DateTimeOffset UpdatedAt { get => _updatedAt; set { _updatedAt = value; OnPropertyChanged(); } }

        public string DisplayTrigger => AutomationDisplayFormatter.Trigger(ToDomain().Trigger);
        public string DisplayAction => AutomationDisplayFormatter.Action(ToDomain().Action);
        public string NextRunDisplay => _nextRunAt?.LocalDateTime.ToString("G", LocalizationService.Instance.CurrentCulture)
            ?? (TriggerKind is AutomationTriggerKind.Hotkey or AutomationTriggerKind.ProcessStarted or AutomationTriggerKind.ProcessExited
                ? Loc.Get("Automation.EventBased") : Loc.Get("Automation.NotScheduled"));
        public string LastRunDisplay => AutomationDisplayFormatter.LastRun(_lastRunAt);
        public string RuntimeError => _lastError ?? string.Empty;

        public void RefreshLocalizedDisplayProperties()
        {
            OnPropertyChanged(nameof(DisplayTrigger));
            OnPropertyChanged(nameof(DisplayAction));
            OnPropertyChanged(nameof(NextRunDisplay));
            OnPropertyChanged(nameof(LastRunDisplay));
        }

        public AutomationDefinition ToDomain()
        {
            return new AutomationDefinition
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Active = Active,
                Trigger = CreateTrigger(),
                Action = Action.ToAutomationAction(),
                RunPolicy = new AutomationRunPolicy
                {
                    Cooldown = TimeSpan.FromSeconds(Math.Max(0, CooldownSeconds)),
                    AlreadyRunningBehavior = AlreadyRunningBehavior,
                    EnabledFrom = EnabledFrom.HasValue ? TimeOnly.FromDateTime(EnabledFrom.Value) : null,
                    EnabledUntil = EnabledUntil.HasValue ? TimeOnly.FromDateTime(EnabledUntil.Value) : null
                },
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                LastRunAt = _lastRunAt
            };
        }

        public EditableAutomation Clone()
        {
            return FromDomain(ToDomain());
        }

        public static EditableAutomation FromDomain(AutomationDefinition definition)
        {
            var editable = new EditableAutomation
            {
                Id = definition.Id,
                Name = definition.Name,
                Description = definition.Description,
                Active = definition.Active,
                TriggerKind = definition.Trigger.Kind,
                Action = AutomationEditableActionExtensions.FromAutomationAction(definition.Action),
                AlreadyRunningBehavior = definition.RunPolicy.AlreadyRunningBehavior,
                CooldownSeconds = (int)Math.Max(0, definition.RunPolicy.Cooldown.TotalSeconds),
                EnabledFrom = definition.RunPolicy.EnabledFrom.HasValue ? DateTime.Today + definition.RunPolicy.EnabledFrom.Value.ToTimeSpan() : null,
                EnabledUntil = definition.RunPolicy.EnabledUntil.HasValue ? DateTime.Today + definition.RunPolicy.EnabledUntil.Value.ToTimeSpan() : null,
                _lastRunAt = definition.LastRunAt,
                _nextRunAt = definition.Runtime.NextRunAt,
                _lastError = definition.Runtime.LastError,
                CreatedAt = definition.CreatedAt,
                UpdatedAt = definition.UpdatedAt
            };

            editable.ApplyTrigger(definition.Trigger);
            return editable;
        }

        private AutomationTrigger CreateTrigger()
        {
            return TriggerKind switch
            {
                AutomationTriggerKind.Hotkey => new HotkeyAutomationTrigger
                {
                    Modifiers = Modifiers,
                    VirtualKeyCode = VirtualKeyCode
                },
                AutomationTriggerKind.OnceAt => new OnceAtAutomationTrigger
                {
                    RunAt = new DateTimeOffset(RunAt)
                },
                AutomationTriggerKind.Schedule => new ScheduleAutomationTrigger
                {
                    TimeOfDay = TimeOnly.FromDateTime(ScheduleTime),
                    Days = new HashSet<DayOfWeek>(_scheduleDays)
                },
                AutomationTriggerKind.Interval => new IntervalAutomationTrigger
                {
                    Interval = BuildInterval(),
                    StartImmediately = StartImmediately
                },
                AutomationTriggerKind.ProcessExited => new ProcessExitedAutomationTrigger
                {
                    ProcessName = ProcessName,
                    WindowTitleContains = WindowTitleContains,
                    DelayAfterEvent = TimeSpan.FromSeconds(Math.Max(0, DelayAfterEventSeconds))
                },
                _ => new ProcessStartedAutomationTrigger
                {
                    ProcessName = ProcessName,
                    WindowTitleContains = WindowTitleContains,
                    DelayAfterEvent = TimeSpan.FromSeconds(Math.Max(0, DelayAfterEventSeconds))
                }
            };
        }

        private void ApplyTrigger(AutomationTrigger trigger)
        {
            switch (trigger)
            {
                case HotkeyAutomationTrigger hotkey:
                    Modifiers = hotkey.Modifiers;
                    VirtualKeyCode = hotkey.VirtualKeyCode;
                    break;
                case OnceAtAutomationTrigger once:
                    RunAt = once.RunAt.LocalDateTime;
                    break;
                case ScheduleAutomationTrigger schedule:
                    ScheduleTime = DateTime.Today + schedule.TimeOfDay.ToTimeSpan();
                    _scheduleDays = new HashSet<DayOfWeek>(schedule.Days);
                    NotifyDayProperties();
                    break;
                case IntervalAutomationTrigger interval:
                    ApplyInterval(interval.Interval);
                    StartImmediately = interval.StartImmediately;
                    break;
                case ProcessAutomationTrigger process:
                    ProcessName = process.ProcessName;
                    WindowTitleContains = process.WindowTitleContains;
                    DelayAfterEventSeconds = (int)process.DelayAfterEvent.TotalSeconds;
                    break;
            }
        }

        private TimeSpan BuildInterval()
        {
            var value = Math.Max(1, IntervalValue);
            return IntervalUnit switch
            {
                IntervalUnit.Seconds => TimeSpan.FromSeconds(value),
                IntervalUnit.Hours => TimeSpan.FromHours(value),
                _ => TimeSpan.FromMinutes(value)
            };
        }

        private void ApplyInterval(TimeSpan interval)
        {
            if (interval.TotalHours >= 1 && interval.TotalHours % 1 == 0)
            {
                IntervalValue = (int)interval.TotalHours;
                IntervalUnit = IntervalUnit.Hours;
            }
            else if (interval.TotalMinutes >= 1 && interval.TotalMinutes % 1 == 0)
            {
                IntervalValue = (int)interval.TotalMinutes;
                IntervalUnit = IntervalUnit.Minutes;
            }
            else
            {
                IntervalValue = Math.Max(1, (int)interval.TotalSeconds);
                IntervalUnit = IntervalUnit.Seconds;
            }
        }

        private bool HasDay(DayOfWeek day) => _scheduleDays.Contains(day);

        private void SetDay(DayOfWeek day, bool enabled)
        {
            var changed = enabled ? _scheduleDays.Add(day) : _scheduleDays.Remove(day);
            if (!changed) return;
            NotifyDayProperties();
            OnPropertyChanged(nameof(DisplayTrigger));
        }

        private void NotifyDayProperties()
        {
            OnPropertyChanged(nameof(Monday)); OnPropertyChanged(nameof(Tuesday));
            OnPropertyChanged(nameof(Wednesday)); OnPropertyChanged(nameof(Thursday));
            OnPropertyChanged(nameof(Friday)); OnPropertyChanged(nameof(Saturday)); OnPropertyChanged(nameof(Sunday));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum IntervalUnit
    {
        Seconds,
        Minutes,
        Hours
    }

    public static class AutomationEditableActionExtensions
    {
        public static AutomationAction ToAutomationAction(this EditableAutomationAction reference)
        {
            return new AutomationAction
            {
                Name = reference.Name,
                JobId = reference.JobId,
                MakroId = reference.MakroId,
                ActionType = reference.ActionType
            };
        }

        public static EditableAutomationAction FromAutomationAction(AutomationAction? action)
        {
            return new EditableAutomationAction
            {
                Name = action?.Name ?? string.Empty,
                JobId = action?.JobId,
                MakroId = action?.MakroId,
                ActionType = action?.ActionType ?? AutomationActionTarget.Job
            };
        }
    }
}
