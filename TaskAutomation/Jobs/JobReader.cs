using System;
using System.IO; // Für File
using System.Collections.Generic; // Für List
using System.Text.Json;
// using OpenCvSharp; // Nicht direkt hier benötigt, aber in den Step-Klassen

namespace TaskAutomation.Jobs
{
    public class JobReader
    {
        public static Job ReadSteps(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Die JSON-Datei wurde nicht gefunden: {jsonPath}");

            var json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;
            var job = new Job
            {
                Name = root.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                    ? nameProp.GetString()
                    : throw new FormatException("Fehlender oder ungültiger 'name'-Wert."),
                Repeating = root.TryGetProperty("repeating", out var repProp) && repProp.ValueKind == JsonValueKind.True || repProp.ValueKind == JsonValueKind.False
                    ? repProp.GetBoolean()
                    : throw new FormatException("Fehlender oder ungültiger 'repeating'-Wert."),
                Steps = new List<object>()
            };

            if (!root.TryGetProperty("steps", out var stepsProp) || stepsProp.ValueKind != JsonValueKind.Array)
                throw new FormatException("Fehlende oder ungültige 'steps'-Liste.");

            foreach (var stepElement in root.GetProperty("steps").EnumerateArray())
            {
                if (!stepElement.TryGetProperty("type", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
                    throw new FormatException("Ein Step enthält keinen gültigen 'type'.");

                if (!stepElement.TryGetProperty("settings", out var settingsElement))
                    throw new FormatException("Ein Step enthält keine 'settings'.");

                var type = stepElement.GetProperty("type").GetString();
                object step = type switch
                {
                    "template_matching" => SafeDeserialize<TemplateMatchingStep>(settingsElement, type),
                    "process_duplication" => SafeDeserialize<ProcessDuplicationStep>(settingsElement, type),
                    "desktop_duplication" => SafeDeserialize<DesktopDuplicationStep>(settingsElement, type),
                    "show_image" => SafeDeserialize<ShowImageStep>(settingsElement, type),
                    "video_creation" => SafeDeserialize<VideoCreationStep>(settingsElement, type),
                    "makro_execution" => SafeDeserialize<MakroExecutionStep>(settingsElement, type),
                    _ => throw new NotSupportedException($"Unbekannter Step-Typ: '{type}'")
                };

                job.Steps.Add(step);
            }
            return job;
        }

        private static T SafeDeserialize<T>(JsonElement element, string type)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText())
                       ?? throw new JsonException($"Deserialisierung von '{type}' ergab null.");
            }
            catch (Exception ex)
            {
                throw new FormatException($"Fehler beim Deserialisieren des Steps vom Typ '{type}': {ex.Message}", ex);
            }
        }
    }
}