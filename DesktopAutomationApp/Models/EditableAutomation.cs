using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.Models
{
    public sealed class EditableAutomation : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _name = string.Empty;
        private string _description = string.Empty;
        private bool _active = true;
        private AutomationTriggerKind _triggerKind = AutomationTriggerKind.Hotkey;
        private KeyModifiers _modifiers;
        private uint _virtualKeyCode;
        private DateTime _runAtDate = DateTime.Today;
        private string _runAtTime = "08:00";
        private string _scheduleTime = "08:00";
        private int _intervalValue = 15;
        private IntervalUnit _intervalUnit = IntervalUnit.Minutes;
        private bool _startImmediately = true;
        private string _processName = string.Empty;
        private string _windowTitleContains = string.Empty;
        private int _delayAfterEventSeconds;
        private EditableJobReference _action = new();
        private AutomationAlreadyRunningBehavior _alreadyRunningBehavior = AutomationAlreadyRunningBehavior.Ignore;
        private int _cooldownSeconds;
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
        public DateTime RunAtDate { get => _runAtDate; set { if (_runAtDate != value) { _runAtDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public string RunAtTime { get => _runAtTime; set { if (_runAtTime != value) { _runAtTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public string ScheduleTime { get => _scheduleTime; set { if (_scheduleTime != value) { _scheduleTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public int IntervalValue { get => _intervalValue; set { if (_intervalValue != value) { _intervalValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public IntervalUnit IntervalUnit { get => _intervalUnit; set { if (_intervalUnit != value) { _intervalUnit = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public bool StartImmediately { get => _startImmediately; set { if (_startImmediately != value) { _startImmediately = value; OnPropertyChanged(); } } }
        public string ProcessName { get => _processName; set { if (_processName != value) { _processName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public string WindowTitleContains { get => _windowTitleContains; set { if (_windowTitleContains != value) { _windowTitleContains = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public int DelayAfterEventSeconds { get => _delayAfterEventSeconds; set { if (_delayAfterEventSeconds != value) { _delayAfterEventSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }

        public EditableJobReference Action { get => _action; set { if (!ReferenceEquals(_action, value) && value != null) { _action = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayAction)); } } }
        public AutomationAlreadyRunningBehavior AlreadyRunningBehavior { get => _alreadyRunningBehavior; set { if (_alreadyRunningBehavior != value) { _alreadyRunningBehavior = value; OnPropertyChanged(); } } }
        public int CooldownSeconds { get => _cooldownSeconds; set { if (_cooldownSeconds != value) { _cooldownSeconds = value; OnPropertyChanged(); } } }
        public DateTimeOffset CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); } }
        public DateTimeOffset UpdatedAt { get => _updatedAt; set { _updatedAt = value; OnPropertyChanged(); } }

        public string DisplayTrigger => ToDomain().Trigger.GetDisplayText();
        public string DisplayAction => ToDomain().Action.DisplayText;
        public string NextRunDisplay => TriggerKind is AutomationTriggerKind.Hotkey or AutomationTriggerKind.ProcessStarted or AutomationTriggerKind.ProcessExited
            ? "Ereignisbasiert"
            : "Dummy: nicht berechnet";
        public string LastRunDisplay => "Dummy: nie";

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
                    AlreadyRunningBehavior = AlreadyRunningBehavior,
                    Cooldown = TimeSpan.FromSeconds(Math.Max(0, CooldownSeconds))
                },
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }

        public EditableAutomation Clone()
        {
            var clone = FromDomain(ToDomain());
            clone.Action.CopyJobNameResolverFrom(Action);
            return clone;
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
                Action = AutomationEditableJobReferenceExtensions.FromAutomationAction(definition.Action),
                AlreadyRunningBehavior = definition.RunPolicy.AlreadyRunningBehavior,
                CooldownSeconds = (int)Math.Max(0, definition.RunPolicy.Cooldown.TotalSeconds),
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
                    RunAt = BuildRunAtDateTime()
                },
                AutomationTriggerKind.Schedule => new ScheduleAutomationTrigger
                {
                    TimeOfDay = ParseTimeOrDefault(ScheduleTime, new TimeOnly(8, 0))
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
                    RunAtDate = once.RunAt.LocalDateTime.Date;
                    RunAtTime = once.RunAt.ToString("HH:mm");
                    break;
                case ScheduleAutomationTrigger schedule:
                    ScheduleTime = schedule.TimeOfDay.ToString("HH:mm");
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

        private DateTimeOffset BuildRunAtDateTime()
        {
            var time = ParseTimeOrDefault(RunAtTime, new TimeOnly(8, 0));
            return new DateTimeOffset(RunAtDate.Date + time.ToTimeSpan());
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

        private static TimeOnly ParseTimeOrDefault(string value, TimeOnly fallback)
            => TimeOnly.TryParse(value, out var parsed) ? parsed : fallback;

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

    public static class AutomationEditableJobReferenceExtensions
    {
        public static AutomationAction ToAutomationAction(this EditableJobReference reference)
        {
            return new AutomationAction
            {
                Name = reference.Name,
                JobId = reference.JobId,
                MakroId = reference.MakroId,
                Command = reference.Command,
                ActionType = reference.ActionType
            };
        }

        public static EditableJobReference FromAutomationAction(AutomationAction? action)
        {
            return new EditableJobReference
            {
                Name = action?.Name ?? string.Empty,
                JobId = action?.JobId,
                MakroId = action?.MakroId,
                Command = action?.Command ?? ActionCommand.Start,
                ActionType = action?.ActionType ?? HotkeyActionType.Job
            };
        }
    }
}
