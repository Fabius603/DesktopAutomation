using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCvSharp;

namespace TaskAutomation
{
    public class OpenCvRectJsonConverter : JsonConverter<Rect>
    {
        public override Rect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for Rect.");
            }

            int x = 0, y = 0, width = 0, height = 0;
            bool xSet = false, ySet = false, widthSet = false, heightSet = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (xSet && ySet && widthSet && heightSet)
                    {
                        return new Rect(x, y, width, height);
                    }
                    throw new JsonException("Incomplete Rect object.");
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read(); // Move to the value token

                    // Use case-insensitive comparison for property names from JSON
                    if (string.Equals(propertyName, "x", StringComparison.OrdinalIgnoreCase))
                    {
                        x = reader.GetInt32();
                        xSet = true;
                    }
                    else if (string.Equals(propertyName, "y", StringComparison.OrdinalIgnoreCase))
                    {
                        y = reader.GetInt32();
                        ySet = true;
                    }
                    else if (string.Equals(propertyName, "width", StringComparison.OrdinalIgnoreCase))
                    {
                        width = reader.GetInt32();
                        widthSet = true;
                    }
                    else if (string.Equals(propertyName, "height", StringComparison.OrdinalIgnoreCase))
                    {
                        height = reader.GetInt32();
                        heightSet = true;
                    }
                    // Optionally, you could skip or throw for unexpected properties
                }
            }
            throw new JsonException("Error reading Rect JSON: EndObject token not found.");
        }

        public override void Write(Utf8JsonWriter writer, Rect value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            // Write with lowercase to match typical JSON conventions if this converter is used for serialization
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteNumber("width", value.Width);
            writer.WriteNumber("height", value.Height);
            writer.WriteEndObject();
        }
    }

    // Falls du auch OpenCvSharp.Size deserialisieren musst (z.B. für VideoCreationStep)
    public class OpenCvSizeJsonConverter : JsonConverter<Size>
    {
        public override Size Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for Size.");
            }
            int width = 0, height = 0;
            bool widthSet = false, heightSet = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (widthSet && heightSet)
                    {
                        return new Size(width, height);
                    }
                    throw new JsonException("Incomplete Size object.");
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();
                    if (string.Equals(propertyName, "width", StringComparison.OrdinalIgnoreCase))
                    {
                        width = reader.GetInt32();
                        widthSet = true;
                    }
                    else if (string.Equals(propertyName, "height", StringComparison.OrdinalIgnoreCase))
                    {
                        height = reader.GetInt32();
                        heightSet = true;
                    }
                }
            }
            throw new JsonException("Error reading Size JSON: EndObject token not found.");
        }

        public override void Write(Utf8JsonWriter writer, Size value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("width", value.Width);
            writer.WriteNumber("height", value.Height);
            writer.WriteEndObject();
        }
    }
}