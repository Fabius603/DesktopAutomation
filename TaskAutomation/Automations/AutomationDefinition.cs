using System.Text.Json.Serialization;
using TaskAutomation.Hotkeys;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Automations
{
    public sealed class AutomationDefinition
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("active")]
        public bool Active { get; set; } = true;

        [JsonPropertyName("trigger")]
        public AutomationTrigger Trigger { get; set; } = new HotkeyAutomationTrigger();

        [JsonPropertyName("action")]
        public AutomationAction Action { get; set; } = new();

        [JsonPropertyName("run_policy")]
        public AutomationRunPolicy RunPolicy { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

        [JsonPropertyName("last_run_at")]
        public DateTimeOffset? LastRunAt { get; set; }

        [JsonIgnore]
        public AutomationRuntimeInfo Runtime { get; set; } = new();
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AutomationTriggerKind
    {
        Hotkey,
        OnceAt,
        Schedule,
        Interval,
        ProcessStarted,
        ProcessExited,
        WindowEvent,
        FileSystemEvent,
        SystemEvent,
        WindowsEvent
    }

    [JsonDerivedType(typeof(HotkeyAutomationTrigger), "hotkey")]
    [JsonDerivedType(typeof(OnceAtAutomationTrigger), "once_at")]
    [JsonDerivedType(typeof(ScheduleAutomationTrigger), "schedule")]
    [JsonDerivedType(typeof(IntervalAutomationTrigger), "interval")]
    [JsonDerivedType(typeof(ProcessStartedAutomationTrigger), "process_started")]
    [JsonDerivedType(typeof(ProcessExitedAutomationTrigger), "process_exited")]
    [JsonDerivedType(typeof(WindowEventAutomationTrigger), "window_event")]
    [JsonDerivedType(typeof(FileSystemAutomationTrigger), "file_system_event")]
    [JsonDerivedType(typeof(SystemEventAutomationTrigger), "system_event")]
    [JsonDerivedType(typeof(WindowsEventAutomationTrigger), "windows_event")]
    public abstract class AutomationTrigger
    {
        [JsonPropertyName("kind")]
        public abstract AutomationTriggerKind Kind { get; }

        public abstract string GetDisplayText();
    }

    public sealed class HotkeyAutomationTrigger : AutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.Hotkey;

        [JsonPropertyName("modifiers")]
        public KeyModifiers Modifiers { get; set; }

        [JsonPropertyName("virtual_key_code")]
        public uint VirtualKeyCode { get; set; }

        [JsonPropertyName("debounce")]
        public TimeSpan Debounce { get; set; } = TimeSpan.Zero;

        [JsonPropertyName("delay_after_event")]
        public TimeSpan DelayAfterEvent { get; set; } = TimeSpan.Zero;

        public override string GetDisplayText()
            => VirtualKeyCode == 0
                ? "Hotkey nicht gesetzt"
                : HotkeyTextFormatter.Format(Modifiers, VirtualKeyCode)
                  + (Debounce > TimeSpan.Zero ? $" · Mindestabstand {Debounce.TotalSeconds:0}s" : string.Empty)
                  + (DelayAfterEvent > TimeSpan.Zero ? $" · nach {DelayAfterEvent.TotalSeconds:0}s" : string.Empty);
    }

    public sealed class OnceAtAutomationTrigger : AutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.OnceAt;

        [JsonPropertyName("run_at")]
        public DateTimeOffset RunAt { get; set; } = DateTimeOffset.Now.AddHours(1);

        public override string GetDisplayText() => $"Einmalig am {RunAt:dd.MM.yyyy HH:mm}";
    }

    public sealed class ScheduleAutomationTrigger : AutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.Schedule;

        [JsonPropertyName("time_of_day")]
        public TimeOnly TimeOfDay { get; set; } = new(8, 0);

        [JsonPropertyName("days")]
        public HashSet<DayOfWeek> Days { get; set; } =
            new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        public override string GetDisplayText()
        {
            var days = Days.Count == 7 ? "täglich" : string.Join(", ", Days.Select(FormatDay));
            return $"{days} um {TimeOfDay:HH:mm}";
        }

        private static string FormatDay(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => "Mo",
            DayOfWeek.Tuesday => "Di",
            DayOfWeek.Wednesday => "Mi",
            DayOfWeek.Thursday => "Do",
            DayOfWeek.Friday => "Fr",
            DayOfWeek.Saturday => "Sa",
            DayOfWeek.Sunday => "So",
            _ => day.ToString()
        };
    }

    public sealed class IntervalAutomationTrigger : AutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.Interval;

        [JsonPropertyName("interval")]
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

        [JsonPropertyName("start_immediately")]
        public bool StartImmediately { get; set; } = true;

        public override string GetDisplayText() => $"Alle {FormatInterval(Interval)}";

        private static string FormatInterval(TimeSpan interval)
        {
            if (interval.TotalHours >= 1 && interval.TotalHours % 1 == 0) return $"{interval.TotalHours:0} Stunden";
            if (interval.TotalMinutes >= 1 && interval.TotalMinutes % 1 == 0) return $"{interval.TotalMinutes:0} Minuten";
            return $"{Math.Max(1, interval.TotalSeconds):0} Sekunden";
        }
    }

    public abstract class ProcessAutomationTrigger : AutomationTrigger
    {
        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("window_title_contains")]
        public string WindowTitleContains { get; set; } = string.Empty;

        [JsonPropertyName("delay_after_event")]
        public TimeSpan DelayAfterEvent { get; set; } = TimeSpan.Zero;

        protected string FormatProcessText(string eventText)
        {
            var process = string.IsNullOrWhiteSpace(ProcessName) ? "App nicht gesetzt" : ProcessName;
            var title = string.IsNullOrWhiteSpace(WindowTitleContains) ? string.Empty : $" · Titel enthält \"{WindowTitleContains}\"";
            var delay = DelayAfterEvent > TimeSpan.Zero ? $" · nach {DelayAfterEvent.TotalSeconds:0}s" : string.Empty;
            return $"{eventText}: {process}{title}{delay}";
        }
    }

    public sealed class ProcessStartedAutomationTrigger : ProcessAutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.ProcessStarted;

        public override string GetDisplayText() => FormatProcessText("App startet");
    }

    public sealed class ProcessExitedAutomationTrigger : ProcessAutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.ProcessExited;

        public override string GetDisplayText() => FormatProcessText("App schließt");
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WindowAutomationEventKind
    {
        Opened,
        Closed,
        Focused,
        TitleChanged
    }

    public sealed class WindowEventAutomationTrigger : AutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.WindowEvent;

        [JsonPropertyName("event_kind")]
        public WindowAutomationEventKind EventKind { get; set; } = WindowAutomationEventKind.Opened;

        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("window_title_contains")]
        public string WindowTitleContains { get; set; } = string.Empty;

        [JsonPropertyName("delay_after_event")]
        public TimeSpan DelayAfterEvent { get; set; } = TimeSpan.Zero;

        public override string GetDisplayText() => $"Fenster {EventKind}: {ProcessName} {WindowTitleContains}".Trim();
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FileSystemAutomationEventKind
    {
        Created,
        Changed,
        Deleted,
        Renamed
    }

    public sealed class FileSystemAutomationTrigger : AutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.FileSystemEvent;

        [JsonPropertyName("event_kind")]
        public FileSystemAutomationEventKind EventKind { get; set; } = FileSystemAutomationEventKind.Created;

        [JsonPropertyName("directory_path")]
        public string DirectoryPath { get; set; } = string.Empty;

        [JsonPropertyName("filter")]
        public string Filter { get; set; } = "*.*";

        [JsonPropertyName("include_subdirectories")]
        public bool IncludeSubdirectories { get; set; }

        [JsonPropertyName("wait_until_ready")]
        public bool WaitUntilReady { get; set; } = true;

        public override string GetDisplayText() => $"{EventKind}: {DirectoryPath} ({Filter})";
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SystemAutomationEventKind
    {
        SessionLocked,
        SessionUnlocked,
        UserLogoff,
        SystemShutdown,
        Suspend,
        Resume
    }

    public sealed class SystemEventAutomationTrigger : AutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.SystemEvent;

        [JsonPropertyName("event_kind")]
        public SystemAutomationEventKind EventKind { get; set; } = SystemAutomationEventKind.SessionLocked;

        public override string GetDisplayText() => EventKind.ToString();
    }

    public sealed class WindowsEventAutomationTrigger : AutomationTrigger
    {
        public override AutomationTriggerKind Kind => AutomationTriggerKind.WindowsEvent;

        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = "network.availability.changed";

        [JsonPropertyName("filters")]
        public Dictionary<string, string?> Filters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("debounce")]
        public TimeSpan Debounce { get; set; } = TimeSpan.FromSeconds(1);

        [JsonPropertyName("delay_after_event")]
        public TimeSpan DelayAfterEvent { get; set; }

        public override string GetDisplayText() => EventType;
    }

    public sealed class AutomationAction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("job_id")]
        public Guid? JobId { get; set; }

        [JsonPropertyName("makro_id")]
        public Guid? MakroId { get; set; }

        [JsonPropertyName("action_type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AutomationActionTarget ActionType { get; set; } = AutomationActionTarget.Job;

        public string DisplayText
        {
            get
            {
                var type = ActionType == AutomationActionTarget.Makro ? "Makro" : "Job";
                var name = string.IsNullOrWhiteSpace(Name) ? "nicht gesetzt" : Name;
                return $"{type} \"{name}\"";
            }
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AutomationActionTarget
    {
        Job,
        Makro
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AutomationAlreadyRunningBehavior
    {
        StartParallel,
        Stop,
        Ignore,
        Restart
    }

    public sealed class AutomationRunPolicy
    {
        [JsonPropertyName("already_running_behavior")]
        public AutomationAlreadyRunningBehavior AlreadyRunningBehavior { get; set; } = AutomationAlreadyRunningBehavior.Ignore;

        [JsonPropertyName("cooldown")]
        public TimeSpan Cooldown { get; set; } = TimeSpan.Zero;

        [JsonPropertyName("enabled_from")]
        public TimeOnly? EnabledFrom { get; set; }

        [JsonPropertyName("enabled_until")]
        public TimeOnly? EnabledUntil { get; set; }
    }
}
