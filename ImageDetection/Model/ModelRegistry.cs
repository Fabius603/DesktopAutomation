using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.Model
{
    public sealed class ModelRegistry
    {
        public Dictionary<string, ModelEntry> Models { get; set; } = new();
        public static ModelRegistry FromJson(string json)
            => new ModelRegistry { Models = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ModelEntry>>(json)! };
    }
}
