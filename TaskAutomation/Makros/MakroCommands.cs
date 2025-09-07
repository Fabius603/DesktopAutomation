using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ObservableCollection<MakroBefehl> Befehle { get; set; } = new();
    }


    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(MouseMoveAbsoluteBefehl), "mouse_move_absolute")]
    [JsonDerivedType(typeof(MouseMoveRelativeBefehl), "mouse_move_relative")]
    [JsonDerivedType(typeof(MouseDownBefehl), "mouse_down")]
    [JsonDerivedType(typeof(MouseUpBefehl), "mouse_up")]
    [JsonDerivedType(typeof(KeyDownBefehl), "key_down")]
    [JsonDerivedType(typeof(KeyUpBefehl), "key_up")]
    [JsonDerivedType(typeof(TimeoutBefehl), "timeout")]
    public abstract class MakroBefehl { }

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
        [JsonPropertyName("duration")] public int Duration { get; set; }
    }
}
