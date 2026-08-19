using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TaskAutomation.Jobs;

public sealed class JobVariable
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("value_kind")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResultValueKind ValueKind { get; set; } = ResultValueKind.Text;

    [JsonPropertyName("cardinality")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResultCardinality Cardinality { get; set; } = ResultCardinality.Single;

    [JsonPropertyName("value")]
    public JsonNode? Value { get; set; }
}
