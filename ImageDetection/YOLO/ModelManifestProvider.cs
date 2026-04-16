using ImageDetection.Model;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace ImageDetection.YOLO
{
    public static class ModelManifestProvider
    {
        private const string ManifestFileName = "yolo-models.json";
        private const string EmbeddedResourceName = "ImageDetection.YOLO.Resources.yolo-models.json";

        /// <summary>
        /// Lädt das Manifest aus dem lokalen Modell-Ordner.
        /// Falls noch nicht vorhanden, wird die eingebettete Standard-Liste dorthin kopiert.
        /// </summary>
        public static ModelRegistry LoadManifest(string modelFolderPath)
        {
            Directory.CreateDirectory(modelFolderPath);

            var path = Path.Combine(modelFolderPath, ManifestFileName);
            if (!File.Exists(path))
                SeedDefaultManifest(path);

            var json = File.ReadAllText(path, Encoding.UTF8);
            return ModelRegistry.FromJson(json);
        }

        /// <summary>
        /// Kopiert die eingebettete Standard-Modellliste in den Ziel-Pfad.
        /// </summary>
        private static void SeedDefaultManifest(string targetPath)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(EmbeddedResourceName);
            if (stream == null)
                throw new InvalidOperationException($"Eingebettete Resource nicht gefunden: {EmbeddedResourceName}");

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            File.WriteAllText(targetPath, json, Encoding.UTF8);
        }
    }
}
