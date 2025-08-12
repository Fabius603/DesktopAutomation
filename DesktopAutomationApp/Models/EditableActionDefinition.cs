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

        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }
        public ActionCommand Command { get => _command; set { if (_command != value) { _command = value; OnPropertyChanged(); } } }

        public ActionDefinition ToDomain() => new() { Name = Name, Command = Command };
        public static EditableActionDefinition FromDomain(ActionDefinition? d) =>
            new() { Name = d?.Name ?? "", Command = d?.Command ?? ActionCommand.Start };

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

        public EditableHotkey Clone() => new()
        {
            Name = Name,
            Modifiers = Modifiers,
            VirtualKeyCode = VirtualKeyCode,
            Action = new EditableActionDefinition { Name = Action.Name, Command = Action.Command }
        };

        public HotkeyDefinition ToDomain() => new()
        {
            Name = Name,
            Modifiers = Modifiers,
            VirtualKeyCode = VirtualKeyCode,
            Action = Action.ToDomain()
        };

        public static EditableHotkey FromDomain(HotkeyDefinition d) => new()
        {
            Name = d.Name,
            Modifiers = d.Modifiers,
            VirtualKeyCode = d.VirtualKeyCode,
            Action = EditableActionDefinition.FromDomain(d.Action)
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
