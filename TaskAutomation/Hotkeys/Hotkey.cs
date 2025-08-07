using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Hotkeys
{
    /// <summary>
    /// Definition eines Hotkeys einschließlich des auszuführenden Action-Namens.
    /// </summary>
    public class HotkeyDefinition
    {
        public string Name { get; }
        public KeyModifiers Modifiers { get; }
        public uint VirtualKeyCode { get; }
        public ActionDefinition Action { get; }

        [JsonConstructor]
        public HotkeyDefinition(string name, KeyModifiers modifiers, uint virtualKeyCode, ActionDefinition action)
        {
            Name = name;
            Modifiers = modifiers;
            VirtualKeyCode = virtualKeyCode;
            Action = action;
        }
    }

    public enum ActionCommand { Start, Stop, Toggle }

    public class ActionDefinition
    {
        public string Name { get; }
        public ActionCommand Command { get; }

        [JsonConstructor]
        public ActionDefinition(string name, ActionCommand command)
        {
            Name = name;
            Command = command;
        }
    }
}
