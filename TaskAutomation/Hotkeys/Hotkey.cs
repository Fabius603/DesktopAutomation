using System.Text.Json.Serialization;
using TaskAutomation.Jobs;

namespace TaskAutomation.Hotkeys
{
    /// <summary>Definition eines Hotkeys inkl. auszuführendem Job.</summary>
    public class HotkeyDefinition
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("modifiers")]
        public KeyModifiers Modifiers { get; set; } = KeyModifiers.None;

        [JsonPropertyName("virtual_key_code")]
        public uint VirtualKeyCode { get; set; }

        [JsonPropertyName("action")]
        public JobReference Job { get; set; } = new JobReference();

        [JsonPropertyName("active")]
        public bool Active { get; set; } = true;

        [JsonConstructor]
        public HotkeyDefinition(string name, KeyModifiers modifiers, uint virtualKeyCode, JobReference job)
        {
            Name = name;
            Modifiers = modifiers;
            VirtualKeyCode = virtualKeyCode;
            Job = job ?? new JobReference();
        }

        public HotkeyDefinition() { }

        public HotkeyDefinition Clone() => new HotkeyDefinition
        {
            Name = this.Name,
            Modifiers = this.Modifiers,
            VirtualKeyCode = this.VirtualKeyCode,
            Job = this.Job?.Clone() ?? new JobReference()
        };
    }

    public enum ActionCommand { Start, Stop, Toggle }

    public class JobReference
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("job_id")]
        public Guid? JobId { get; set; }

        [JsonPropertyName("command")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ActionCommand Command { get; set; } = ActionCommand.Start;

        [JsonConstructor]
        public JobReference(string name, ActionCommand command)
        {
            Name = name;
            Command = command;
        }

        public JobReference() { }

        public JobReference Clone() => new JobReference
        {
            Name = this.Name,
            JobId = this.JobId,
            Command = this.Command
        };
    }
}
