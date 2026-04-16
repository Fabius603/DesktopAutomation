using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.JsonRepository
{
    public sealed class JsonRepositoryOptions
    {
        public required string DirectoryPath { get; init; }
        public JsonSerializerOptions JsonOptions { get; init; } = DefaultJson();

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
