using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TaskAutomation.Makros
{
    public class Makro
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("desktop_index")]
        public int DesktopIndex { get; set; }

        [JsonPropertyName("adapter_index")]
        public int AdapterIndex { get; set; }

        [JsonPropertyName("commands")]
        [JsonConverter(typeof(MakroBefehlListConverter))]
        public List<MakroBefehl> Befehle { get; set; }
    }

    public abstract class MakroBefehl
    {
        [JsonPropertyName("type")]
        public string Typ { get; set; }
    }

    public class MouseMoveBefehl : MakroBefehl
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class MouseDownBefehl : MakroBefehl
    {
        public string Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class MouseUpBefehl : MakroBefehl
    {
        public string Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class KeyDownBefehl : MakroBefehl
    {
        public string Key { get; set; }
    }

    public class KeyUpBefehl : MakroBefehl
    {
        public string Key { get; set; }
    }

    public class TimeoutBefehl : MakroBefehl
    {
        public int Duration { get; set; }
    }
}
