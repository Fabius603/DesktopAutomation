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
                "mouseMove" => JsonSerializer.Deserialize<MouseMoveBefehl>(root.GetRawText(), options),
                "mouseDown" => JsonSerializer.Deserialize<MouseDownBefehl>(root.GetRawText(), options),
                "mouseUp" => JsonSerializer.Deserialize<MouseUpBefehl>(root.GetRawText(), options),
                "keyDown" => JsonSerializer.Deserialize<KeyDownBefehl>(root.GetRawText(), options),
                "keyUp" => JsonSerializer.Deserialize<KeyUpBefehl>(root.GetRawText(), options),
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
