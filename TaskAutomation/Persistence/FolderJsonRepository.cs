using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TaskAutomation.Hotkeys;

namespace TaskAutomation.Persistence
{
    public sealed class FolderJsonRepositoryOptions
    {
        /// <summary>Ordner, aus dem alle *.json geladen werden.</summary>
        public string FolderPath { get; init; } = ".";

        /// <summary>Suchmuster für Eingabedateien (beim Laden).</summary>
        public string SearchPattern { get; init; } = "*.json";

        /// <summary>Dateiname für die konsolidierte Zieldatei beim Speichern.</summary>
        public string SaveFileName { get; init; } = "items.json";

        /// <summary>JSON-Optionen (z. B. CamelCase, Enums als String …).</summary>
        public JsonSerializerOptions JsonOptions { get; init; } = DefaultJson();

        /// <summary>Optional Backup vor dem Überschreiben anlegen.</summary>
        public bool CreateBackup { get; init; } = true;

        /// <summary>Backup-Suffix (Zeitstempel).</summary>
        public string BackupSuffixFormat { get; init; } = "yyyyMMddHHmmss";

        private static JsonSerializerOptions DefaultJson()
        {
            var opts = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            opts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            return opts;
        }
    }
}
