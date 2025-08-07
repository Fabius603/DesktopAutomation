using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TaskAutomation.Hotkeys
{
    /// <summary>
    /// Struktur für JSON-Mapping der Konfigurationsdatei.
    /// </summary>
    public class HotkeyConfigEntry
    {
        public string Name { get; set; } = string.Empty;
        public KeyModifiers Modifiers { get; set; }
        public uint VirtualKeyCode { get; set; }
        [JsonPropertyName("Job")]
        public string JobName { get; set; } = string.Empty;
    }
}
