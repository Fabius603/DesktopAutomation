using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.Models
{
    public sealed class EditableJobReference : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private ActionCommand _command = ActionCommand.Start;
        private Guid? _jobId;
        private HotkeyActionType _actionType = HotkeyActionType.Job;
        private Guid? _makroId;
        private Func<EditableJobReference, string>? _jobNameResolver;

        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } } }
        public ActionCommand Command { get => _command; set { if (_command != value) { _command = value; OnPropertyChanged(); } } }
        public Guid? JobId { get => _jobId; set { if (_jobId != value) { _jobId = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } } }
        public HotkeyActionType ActionType { get => _actionType; set { if (_actionType != value) { _actionType = value; OnPropertyChanged(); } } }
        public Guid? MakroId { get => _makroId; set { if (_makroId != value) { _makroId = value; OnPropertyChanged(); } } }

        /// <summary>
        /// Der aktuell anzuzeigende Job-Name. Basiert auf JobId falls vorhanden, andernfalls auf Name.
        /// </summary>
        public string DisplayName => _jobNameResolver?.Invoke(this) ?? Name;

        /// <summary>
        /// Setzt den Resolver für die Anzeige des aktuellen Job-Namens
        /// </summary>
        public void SetJobNameResolver(Func<EditableJobReference, string> resolver)
        {
            _jobNameResolver = resolver;
            OnPropertyChanged(nameof(DisplayName));
        }

        /// <summary>
        /// Kopiert den Job-Name-Resolver von einer anderen JobReference
        /// </summary>
        public void CopyJobNameResolverFrom(EditableJobReference other)
        {
            if (other != null)
            {
                _jobNameResolver = other._jobNameResolver;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public JobReference ToDomain() => new() { Name = Name, Command = Command, JobId = JobId, ActionType = ActionType, MakroId = MakroId };
        public static EditableJobReference FromDomain(JobReference? d) =>
            new() { Name = d?.Name ?? "", Command = d?.Command ?? ActionCommand.Start, JobId = d?.JobId, ActionType = d?.ActionType ?? HotkeyActionType.Job, MakroId = d?.MakroId };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public sealed class EditableHotkey : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _name = string.Empty;
        private KeyModifiers _modifiers;
        private uint _vk;
        private EditableJobReference _job = new();
        private bool _active = true;

        public Guid Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }
        public KeyModifiers Modifiers { get => _modifiers; set { if (_modifiers != value) { _modifiers = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public uint VirtualKeyCode { get => _vk; set { if (_vk != value) { _vk = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public EditableJobReference Job { get => _job; set { if (!ReferenceEquals(_job, value) && value != null) { _job = value; OnPropertyChanged(); } } }
        public bool Active { get => _active; set { if (_active != value) { _active = value; OnPropertyChanged(); } } }

        // Anzeige-Property: sofort aktualisiert ohne Converter
        public string DisplayTrigger => BuildTriggerText(Modifiers, VirtualKeyCode);

        public EditableHotkey Clone()
        {
            var cloned = new EditableHotkey
            {
                Id = Id,
                Name = Name,
                Modifiers = Modifiers,
                VirtualKeyCode = VirtualKeyCode,
                Job = new EditableJobReference
                {
                    Name = Job.Name,
                    Command = Job.Command,
                    JobId = Job.JobId,
                    ActionType = Job.ActionType,
                    MakroId = Job.MakroId,
                },
                Active = Active
            };

            cloned.Job.CopyJobNameResolverFrom(Job);
            return cloned;
        }

        public HotkeyDefinition ToDomain() => new()
        {
            Id = Id,
            Name = Name,
            Modifiers = Modifiers,
            VirtualKeyCode = VirtualKeyCode,
            Job = Job.ToDomain(),
            Active = Active
        };

        public static EditableHotkey FromDomain(HotkeyDefinition d) => new()
        {
            Id = d.Id,
            Name = d.Name,
            Modifiers = d.Modifiers,
            VirtualKeyCode = d.VirtualKeyCode,
            Job = EditableJobReference.FromDomain(d.Job),
            Active = d.Active
        };

        private static string BuildTriggerText(KeyModifiers mods, uint vk)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (mods.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (mods.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
            if (mods.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
            if (mods.HasFlag(KeyModifiers.Windows)) parts.Add("Win");

            string keyText = VkToText(vk);
            if (!string.IsNullOrWhiteSpace(keyText))
                parts.Add(keyText);

            return string.Join(" + ", parts);
        }

        private static string VkToText(uint vk)
        {
            try
            {
                var key = System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)vk);
                if (key != System.Windows.Input.Key.None) return KeyToPrettyString(key);
            }
            catch { /* ignore */ }

            if ((vk >= 0x30 && vk <= 0x39) || (vk >= 0x41 && vk <= 0x5A))
                return ((char)vk).ToString().ToUpperInvariant();
            if (vk >= 0x70 && vk <= 0x7B)
                return $"F{vk - 0x6F}";
            return $"0x{vk:X2}";
        }

        private static string KeyToPrettyString(System.Windows.Input.Key key) => key switch
        {
            System.Windows.Input.Key.Space => "Space",
            System.Windows.Input.Key.Return => "Enter",
            System.Windows.Input.Key.Escape => "Esc",
            System.Windows.Input.Key.Tab => "Tab",
            System.Windows.Input.Key.Back => "Backspace",
            System.Windows.Input.Key.Delete => "Delete",
            System.Windows.Input.Key.Insert => "Insert",
            System.Windows.Input.Key.Left => "Left",
            System.Windows.Input.Key.Right => "Right",
            System.Windows.Input.Key.Up => "Up",
            System.Windows.Input.Key.Down => "Down",
            _ => key.ToString()
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
