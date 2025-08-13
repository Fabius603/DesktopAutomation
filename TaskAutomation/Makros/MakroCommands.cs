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

        [JsonPropertyName("commands")]
        public List<MakroBefehl> Befehle { get; set; }
    }


    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(MouseMoveBefehl), "mouse_move")]
    [JsonDerivedType(typeof(MouseDownBefehl), "mouse_down")]
    [JsonDerivedType(typeof(MouseUpBefehl), "mouse_up")]
    [JsonDerivedType(typeof(KeyDownBefehl), "key_down")]
    [JsonDerivedType(typeof(KeyUpBefehl), "key_up")]
    [JsonDerivedType(typeof(TimeoutBefehl), "timeout")]
    public abstract class MakroBefehl { }

    public sealed class MouseMoveBefehl : MakroBefehl
    {
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }
    }

    public sealed class MouseDownBefehl : MakroBefehl
    {
        [JsonPropertyName("button")] public string Button { get; set; } = string.Empty;
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }
    }

    public sealed class MouseUpBefehl : MakroBefehl
    {
        [JsonPropertyName("button")] public string Button { get; set; } = string.Empty;
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }
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
        [JsonPropertyName("duration")] public int Duration { get; set; }
    }
}
