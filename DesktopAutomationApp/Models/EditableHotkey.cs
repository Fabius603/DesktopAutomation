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
    public sealed class EditableActionDefinition : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private ActionCommand _command = ActionCommand.Start;
        private Guid? _jobId;
        private Func<EditableActionDefinition, string>? _jobNameResolver;

        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } } }
        public ActionCommand Command { get => _command; set { if (_command != value) { _command = value; OnPropertyChanged(); } } }
        public Guid? JobId { get => _jobId; set { if (_jobId != value) { _jobId = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } } }

        /// <summary>
        /// Der aktuell anzuzeigende Job-Name. Basiert auf JobId falls vorhanden, andernfalls auf Name.
        /// </summary>
        public string DisplayName => _jobNameResolver?.Invoke(this) ?? Name;

        /// <summary>
        /// Setzt den Resolver für die Anzeige des aktuellen Job-Namens
        /// </summary>
        public void SetJobNameResolver(Func<EditableActionDefinition, string> resolver)
        {
            _jobNameResolver = resolver;
            OnPropertyChanged(nameof(DisplayName));
        }

        /// <summary>
        /// Kopiert den Job-Name-Resolver von einer anderen ActionDefinition
        /// </summary>
        public void CopyJobNameResolverFrom(EditableActionDefinition other)
        {
            if (other != null)
            {
                _jobNameResolver = other._jobNameResolver;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public ActionDefinition ToDomain() => new() { Name = Name, Command = Command, JobId = JobId };
        public static EditableActionDefinition FromDomain(ActionDefinition? d) =>
            new() { Name = d?.Name ?? "", Command = d?.Command ?? ActionCommand.Start, JobId = d?.JobId };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public sealed class EditableHotkey : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private KeyModifiers _modifiers;
        private uint _vk;
        private EditableActionDefinition _action = new();
        private bool _active = true;

        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }
        public KeyModifiers Modifiers { get => _modifiers; set { if (_modifiers != value) { _modifiers = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public uint VirtualKeyCode { get => _vk; set { if (_vk != value) { _vk = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTrigger)); } } }
        public EditableActionDefinition Action { get => _action; set { if (!ReferenceEquals(_action, value) && value != null) { _action = value; OnPropertyChanged(); } } }
        public bool Active { get => _active; set { if (_active != value) { _active = value; OnPropertyChanged(); } } }

        // Anzeige-Property: sofort aktualisiert ohne Converter
        public string DisplayTrigger => BuildTriggerText(Modifiers, VirtualKeyCode);

        public EditableHotkey Clone()
        {
            var cloned = new EditableHotkey
            {
                Name = Name,
                Modifiers = Modifiers,
                VirtualKeyCode = VirtualKeyCode,
                Action = new EditableActionDefinition 
                { 
                    Name = Action.Name, 
                    Command = Action.Command, 
                    JobId = Action.JobId
                },
                Active = Active
            };
            
            // Resolver kopieren
            cloned.Action.CopyJobNameResolverFrom(Action);
            return cloned;
        }

        public HotkeyDefinition ToDomain() => new()
        {
            Name = Name,
            Modifiers = Modifiers,
            VirtualKeyCode = VirtualKeyCode,
            Action = Action.ToDomain(),
            Active = Active
        };

        public static EditableHotkey FromDomain(HotkeyDefinition d) => new()
        {
            Name = d.Name,
            Modifiers = d.Modifiers,
            VirtualKeyCode = d.VirtualKeyCode,
            Action = EditableActionDefinition.FromDomain(d.Action),
            Active = d.Active
        };

        // Formatierung – analog zu deinem bisherigen Converter
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
            if (vk >= 0x70 && vk <= 0x7B) // F1..F12
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
