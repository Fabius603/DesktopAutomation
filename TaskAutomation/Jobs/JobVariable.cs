using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TaskAutomation.Jobs;

public enum JobVariableScope
{
    StepValue,
    Shared
}

public class JobVariable
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JobVariableScope Scope { get; set; } = JobVariableScope.StepValue;

    [JsonPropertyName("value_kind")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResultValueKind ValueKind { get; set; } = ResultValueKind.Text;

    [JsonPropertyName("cardinality")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResultCardinality Cardinality { get; set; } = ResultCardinality.Single;

    [JsonPropertyName("value")]
    public JsonNode? Value { get; set; }

    [JsonPropertyName("enum_type_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EnumTypeName { get; set; }

    [JsonPropertyName("enum_values")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? EnumValues { get; set; }

    [JsonPropertyName("enum_display_names")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? EnumDisplayNames { get; set; }
}

/// <summary>
/// Persisted value owned by exactly one step input. Local values use the same typed
/// storage as job variables, but are not part of the user-managed job-variable catalog.
/// </summary>
public sealed class LocalValue : JobVariable
{
    [JsonPropertyName("owner_step_id")]
    public string OwnerStepId { get; set; } = string.Empty;

    [JsonPropertyName("input_path")]
    public string InputPath { get; set; } = string.Empty;
}
