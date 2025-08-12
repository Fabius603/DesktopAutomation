using System.Text.Json.Serialization;
using TaskAutomation.Jobs;

namespace TaskAutomation.Hotkeys
{
    /// <summary>Definition eines Hotkeys inkl. auszuführender Action.</summary>
    public class HotkeyDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("modifiers")]
        public KeyModifiers Modifiers { get; set; } = KeyModifiers.None;

        [JsonPropertyName("virtual_key_code")]
        public uint VirtualKeyCode { get; set; }

        [JsonPropertyName("action")]
        public ActionDefinition Action { get; set; } = new ActionDefinition();

        [JsonPropertyName("active")]
        public bool Active { get; set; } = true;

        // Optional: vorhandenen Ctor beibehalten – praktisch für Service-Erzeugung
        [JsonConstructor]
        public HotkeyDefinition(string name, KeyModifiers modifiers, uint virtualKeyCode, ActionDefinition action)
        {
            Name = name;
            Modifiers = modifiers;
            VirtualKeyCode = virtualKeyCode;
            Action = action ?? new ActionDefinition();
        }

        public HotkeyDefinition() { }

        public HotkeyDefinition Clone() => new HotkeyDefinition
        {
            Name = this.Name,
            Modifiers = this.Modifiers,
            VirtualKeyCode = this.VirtualKeyCode,
            Action = this.Action?.Clone() ?? new ActionDefinition()
        };
    }

    public enum ActionCommand { Start, Stop, Toggle }

    public class ActionDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("command")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ActionCommand Command { get; set; } = ActionCommand.Start;

        [JsonConstructor]
        public ActionDefinition(string name, ActionCommand command)
        {
            Name = name;
            Command = command;
        }

        public ActionDefinition() { }

        public ActionDefinition Clone() => new ActionDefinition
        {
            Name = this.Name,
            Command = this.Command
        };
    }
}
