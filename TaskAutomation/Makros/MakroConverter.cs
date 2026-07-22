using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskAutomation.Makros
{
    public class MakroBefehlListConverter : JsonConverter<List<MakroBefehl>>
    {
        public override List<MakroBefehl> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new List<MakroBefehl>();
            using var jsonDoc = JsonDocument.ParseValue(ref reader);

            foreach (var element in jsonDoc.RootElement.EnumerateArray())
            {
                var raw = element.GetRawText();
                var befehl = JsonSerializer.Deserialize<MakroBefehl>(raw, options);
                result.Add(befehl);
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<MakroBefehl> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var befehl in value)
            {
                JsonSerializer.Serialize(writer, befehl, befehl.GetType(), options);
            }
            writer.WriteEndArray();
        }
    }

    public class MakroBefehlConverter : JsonConverter<MakroBefehl>
    {
        public override MakroBefehl Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                throw new JsonException("Makroeintrag enthält kein 'type'-Feld.");

            string type = typeProp.GetString();

            return type switch
            {
                "mouseMoveAbsolute" or "mouse_move_absolute" => JsonSerializer.Deserialize<MouseMoveAbsoluteBefehl>(root.GetRawText(), options),
                "mouseMoveRelative" or "mouse_move_relative" => JsonSerializer.Deserialize<MouseMoveRelativeBefehl>(root.GetRawText(), options),
                "mouseMove" => JsonSerializer.Deserialize<MouseMoveAbsoluteBefehl>(root.GetRawText(), options), // Legacy compatibility
                "mouseDown" or "mouse_down" => JsonSerializer.Deserialize<MouseDownBefehl>(root.GetRawText(), options),
                "mouseUp" or "mouse_up" => JsonSerializer.Deserialize<MouseUpBefehl>(root.GetRawText(), options),
                "keyDown" or "key_down" => JsonSerializer.Deserialize<KeyDownBefehl>(root.GetRawText(), options),
                "keyUp" or "key_up" => JsonSerializer.Deserialize<KeyUpBefehl>(root.GetRawText(), options),
                "timeout" => JsonSerializer.Deserialize<TimeoutBefehl>(root.GetRawText(), options),
                _ => throw new NotSupportedException($"Unbekannter Makro-Typ: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, MakroBefehl value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
