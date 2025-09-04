using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ImageDetection.Model
{
    public sealed class ModelEntry
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = "";
        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
