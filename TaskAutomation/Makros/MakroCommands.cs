using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TaskAutomation.Makros
{
    public class Makro
    {
        public const int CurrentFormatVersion = 3;

        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; } = CurrentFormatVersion;

        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("commands")]
        public ObservableCollection<MakroBefehl> Befehle
        {
            get => _commands;
            set => _commands = value ?? new ObservableCollection<MakroBefehl>();
        }
        private ObservableCollection<MakroBefehl> _commands = new();

        [JsonPropertyName("groups")]
        public ObservableCollection<MakroGruppe> Gruppen
        {
            get => _groups;
            set => _groups = value ?? new ObservableCollection<MakroGruppe>();
        }
        private ObservableCollection<MakroGruppe> _groups = new();

        [JsonPropertyName("recordingSettings")]
        public MakroRecordingSettings RecordingSettings
        {
            get => _recordingSettings;
            set => _recordingSettings = value ?? new MakroRecordingSettings();
        }
        private MakroRecordingSettings _recordingSettings = new();

        [JsonPropertyName("recordedEnvironment")]
        public MakroRecordedEnvironment? RecordedEnvironment { get; set; }
    }


    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(MouseMoveAbsoluteBefehl), "mouse_move_absolute")]
    [JsonDerivedType(typeof(MouseMoveRelativeBefehl), "mouse_move_relative")]
    [JsonDerivedType(typeof(MouseDownBefehl), "mouse_down")]
    [JsonDerivedType(typeof(MouseUpBefehl), "mouse_up")]
    [JsonDerivedType(typeof(KeyDownBefehl), "key_down")]
    [JsonDerivedType(typeof(KeyUpBefehl), "key_up")]
    [JsonDerivedType(typeof(TimeoutBefehl), "timeout")]
    public abstract class MakroBefehl : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("groupId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? GroupId
        {
            get => _groupId;
            set { if (_groupId == value) return; _groupId = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasGroup)); }
        }
        private string? _groupId;

        [JsonIgnore] public bool HasGroup => !string.IsNullOrWhiteSpace(GroupId);
        [JsonIgnore] public string GroupTitleDisplay { get; private set; } = string.Empty;
        [JsonIgnore] public string GroupSummaryDisplay { get; private set; } = string.Empty;
        [JsonIgnore] public string ExecutionTimeDisplay { get; private set; } = string.Empty;

        public void SetDisplayMetadata(string executionTime, string groupTitle = "", string groupSummary = "")
        {
            ExecutionTimeDisplay = executionTime;
            GroupTitleDisplay = groupTitle;
            GroupSummaryDisplay = groupSummary;
            OnPropertyChanged(nameof(ExecutionTimeDisplay));
            OnPropertyChanged(nameof(GroupTitleDisplay));
            OnPropertyChanged(nameof(GroupSummaryDisplay));
        }

        /// <summary>
        /// Hochauflösende Wartezeit vor diesem Befehl. Null kennzeichnet manuell
        /// angelegte beziehungsweise alte Befehle. Die Zeit wird relativ zum
        /// vorherigen Befehl gespeichert, damit aufgenommene Blöcke verschiebbar bleiben.
        /// </summary>
        [JsonPropertyName("delayBeforeUs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? DelayBeforeMicroseconds
        {
            get => _delayBeforeMicroseconds;
            set
            {
                if (_delayBeforeMicroseconds == value) return;
                _delayBeforeMicroseconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasPreciseDelay));
                OnPropertyChanged(nameof(DelayBeforeDisplay));
            }
        }
        private long? _delayBeforeMicroseconds;

        [JsonIgnore] public bool HasPreciseDelay => DelayBeforeMicroseconds.HasValue;
        [JsonIgnore] public string DelayBeforeDisplay => DelayBeforeMicroseconds.HasValue
            ? MakroTimeFormatter.FormatMicroseconds(DelayBeforeMicroseconds.Value, includePrefix: true)
            : string.Empty;

        private bool _isValid = true;
        [JsonIgnore] public bool IsValid { get => _isValid; private set { if (_isValid != value) { _isValid = value; OnPropertyChanged(); } } }
        private MakroValidationError _validationError;
        [JsonIgnore] public MakroValidationError ValidationError { get => _validationError; private set { if (_validationError != value) { _validationError = value; OnPropertyChanged(); } } }
        [JsonIgnore] public string? ValidationMessage => IsValid ? null : MakroValidation.Describe(ValidationError);
        public void SetValidationResult(bool isValid, MakroValidationError error) { ValidationError = error; IsValid = isValid; OnPropertyChanged(nameof(ValidationMessage)); }
    }

    public enum MakroRecordingMode
    {
        ClicksOnly,
        ScreenAccurateAbsolute,
        MotionFaithfulRelative
    }

    public sealed class MakroRecordingSettings
    {
        [JsonPropertyName("mode")]
        public MakroRecordingMode Mode { get; set; } = MakroRecordingMode.ScreenAccurateAbsolute;

        [JsonPropertyName("minimumIntervalUs")]
        public int MinimumIntervalMicroseconds { get; set; } = 1_000;

        [JsonPropertyName("minimumDistancePixels")]
        public int MinimumDistancePixels { get; set; }

        [JsonPropertyName("recordKeyboard")]
        public bool RecordKeyboard { get; set; } = true;

        [JsonPropertyName("recordMouseButtons")]
        public bool RecordMouseButtons { get; set; } = true;

        [JsonPropertyName("removeStopGesture")]
        public bool RemoveStopGesture { get; set; } = true;

        [JsonPropertyName("automaticMovementGroups")]
        public bool AutomaticMovementGroups { get; set; } = true;

        [JsonPropertyName("recordingHotkeyModifiers")]
        public Hotkeys.KeyModifiers RecordingHotkeyModifiers { get; set; } = Hotkeys.KeyModifiers.None;

        [JsonPropertyName("recordingHotkeyVirtualKey")]
        public uint RecordingHotkeyVirtualKey { get; set; } = 0x78; // F9; F10 bleibt Notfall-Stopp.

        public MakroRecordingSettings Clone() => (MakroRecordingSettings)MemberwiseClone();
    }

    public sealed class MakroGruppe
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("automatic")]
        public bool IsAutomatic { get; set; }
    }

    public sealed class MakroRecordedEnvironment
    {
        [JsonPropertyName("virtualDesktopX")] public int VirtualDesktopX { get; set; }
        [JsonPropertyName("virtualDesktopY")] public int VirtualDesktopY { get; set; }
        [JsonPropertyName("virtualDesktopWidth")] public int VirtualDesktopWidth { get; set; }
        [JsonPropertyName("virtualDesktopHeight")] public int VirtualDesktopHeight { get; set; }
        [JsonPropertyName("recordedAtUtc")] public DateTime RecordedAtUtc { get; set; }
        [JsonPropertyName("startCursorX")] public int? StartCursorX { get; set; }
        [JsonPropertyName("startCursorY")] public int? StartCursorY { get; set; }

        public MakroRecordedEnvironment Clone() => (MakroRecordedEnvironment)MemberwiseClone();
    }

    public sealed class MouseMoveAbsoluteBefehl : MakroBefehl
    {
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }
    }

    public sealed class MouseMoveRelativeBefehl : MakroBefehl
    {
        [JsonPropertyName("deltaX")] public int DeltaX { get; set; }
        [JsonPropertyName("deltaY")] public int DeltaY { get; set; }
    }

    public sealed class MouseDownBefehl : MakroBefehl
    {
        [JsonPropertyName("button")] public string Button { get; set; } = string.Empty;
    }

    public sealed class MouseUpBefehl : MakroBefehl
    {
        [JsonPropertyName("button")] public string Button { get; set; } = string.Empty;
    }

    public sealed class KeyDownBefehl : MakroBefehl
    {
        [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;
    }

    public sealed class KeyUpBefehl : MakroBefehl
    {
        [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;
    }

    public sealed class TimeoutBefehl : MakroBefehl
    {
        [JsonPropertyName("duration")]
        public int Duration
        {
            get => _duration;
            set
            {
                if (_duration == value) return;
                _duration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationDisplay));
            }
        }
        private int _duration;
        [JsonIgnore] public string DurationDisplay => MakroTimeFormatter.FormatMilliseconds(Duration);
    }
}
