using ImageDetection.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageDetection.YOLO
{
    public static class ModelManifestProvider
    {
        private const string ManifestFileName = "yolo-models.json"; // liegt durch <Link> im Output-Root
        private const string RelativePath = "YOLO/Resources";

        public static ModelRegistry LoadManifest()
        {
            var path = Path.Combine(AppContext.BaseDirectory, RelativePath, ManifestFileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Manifest nicht gefunden: {path}");

            string json;
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                json = reader.ReadToEnd();
            }

            return ModelRegistry.FromJson(json);
        }
    }
}
