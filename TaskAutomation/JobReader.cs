using System;
using System.IO; // Für File
using System.Collections.Generic; // Für List
using System.Text.Json; // Für JsonDocument, JsonSerializer
// using OpenCvSharp; // Nicht direkt hier benötigt, aber in den Step-Klassen

namespace TaskAutomation
{
    public class JobReader
    {
        public static Job ReadSteps(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;
            var job = new Job
            {
                Name = root.GetProperty("name").GetString(),
                Repeating = root.GetProperty("repeating").GetBoolean(), 
                Steps = new List<object>()
            };

            foreach (var stepElement in root.GetProperty("steps").EnumerateArray())
            {
                var type = stepElement.GetProperty("type").GetString();
                var settingsElement = stepElement.GetProperty("settings");

                object step = type switch
                {
                    "template_matching" => JsonSerializer.Deserialize<TemplateMatchingStep>(settingsElement.GetRawText()),
                    "process_duplication" => JsonSerializer.Deserialize<ProcessDuplicationStep>(settingsElement.GetRawText()),
                    "desktop_duplication" => JsonSerializer.Deserialize<DesktopDuplicationStep>(settingsElement.GetRawText()),
                    "show_image" => JsonSerializer.Deserialize<ShowImageStep>(settingsElement.GetRawText()),
                    "video_creation" => JsonSerializer.Deserialize<VideoCreationStep>(settingsElement.GetRawText()),
                    _ => throw new NotSupportedException($"Unbekannter Step-Typ: {type}")
                };
                job.Steps.Add(step);
            }
            return job;
        }
    }
}