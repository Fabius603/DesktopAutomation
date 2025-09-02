using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Common.JsonRepository
{
    public sealed class JsonRepositoryOptions
    {
        public required string FilePath { get; init; }
        public JsonSerializerOptions JsonOptions { get; init; } = DefaultJson();
        public bool CreateBackup { get; init; } = true;
        public string BackupSuffixFormat { get; init; } = "yyyyMMddHHmmssfff";

        private static JsonSerializerOptions DefaultJson()
        {
            var o = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            o.Converters.Add(new JsonStringEnumConverter());
            return o;
        }
    }
}
