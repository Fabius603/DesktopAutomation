using ImageDetection.Model;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ImageDetection.YOLO
{
    public static class ModelManifestProvider
    {
        private const string ManifestFileName = "yolo-models.json";
        private const string EmbeddedResourceName = "ImageDetection.YOLO.Resources.yolo-models.json";
        private const string LegacyYolo11nUrl =
            "https://github.com/ultralytics/assets/releases/download/v8.4.0/yolo11n.onnx";

        /// <summary>
        /// Lädt das Manifest aus dem lokalen Modell-Ordner.
        /// Falls noch nicht vorhanden, wird die eingebettete Standard-Liste dorthin kopiert.
        /// </summary>
        public static ModelRegistry LoadManifest(string modelFolderPath)
        {
            Directory.CreateDirectory(modelFolderPath);

            var path = Path.Combine(modelFolderPath, ManifestFileName);
            if (!File.Exists(path))
            {
                SeedDefaultManifest(path);
                return ReadManifest(path);
            }

            var installedManifest = ReadManifest(path);
            var defaultManifest = ModelRegistry.FromJson(ReadDefaultManifestJson());
            var addedDefaults = false;

            foreach (var (modelKey, entry) in defaultManifest.Models)
            {
                if (installedManifest.Models.TryGetValue(modelKey, out var installedEntry))
                {
                    if (installedEntry.RecommendedConfidenceThreshold is null &&
                        entry.RecommendedConfidenceThreshold is not null)
                    {
                        installedEntry.RecommendedConfidenceThreshold =
                            entry.RecommendedConfidenceThreshold;
                        addedDefaults = true;
                    }

                    if (string.Equals(modelKey, "yolo11n", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(installedEntry.Url, LegacyYolo11nUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        installedEntry.Url = entry.Url;
                        installedEntry.Sha256 = entry.Sha256;
                        installedEntry.Size = entry.Size;
                        addedDefaults = true;
                    }
                    continue;
                }

                installedManifest.Models.Add(modelKey, entry);
                addedDefaults = true;
            }

            if (addedDefaults)
                TryWriteMergedManifest(path, installedManifest);

            return installedManifest;
        }

        /// <summary>
        /// Kopiert die eingebettete Standard-Modellliste in den Ziel-Pfad.
        /// </summary>
        private static void SeedDefaultManifest(string targetPath)
        {
            File.WriteAllText(targetPath, ReadDefaultManifestJson(), Encoding.UTF8);
        }

        private static ModelRegistry ReadManifest(string path)
            => ModelRegistry.FromJson(File.ReadAllText(path, Encoding.UTF8));

        private static string ReadDefaultManifestJson()
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(EmbeddedResourceName);
            if (stream == null)
                throw new InvalidOperationException($"Eingebettete Resource nicht gefunden: {EmbeddedResourceName}");

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static void TryWriteMergedManifest(string path, ModelRegistry manifest)
        {
            try
            {
                var json = JsonSerializer.Serialize(
                    manifest.Models,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best-effort: Die zusammengeführte In-Memory-Liste bleibt nutzbar.
            }
        }
    }
}
