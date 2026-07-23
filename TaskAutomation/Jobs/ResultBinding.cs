using System.Text.Json.Serialization;

namespace TaskAutomation.Jobs;

public enum ResultValueKind
{
    Boolean, Integer, Number, Text, DateTime, Image, Point, Rectangle,
    Detection, ProcessReference, ResultObject, Enum
}

public enum ResultCardinality { Single, OptionalSingle, Collection }
public enum MissingValuePolicy { FailStep, SkipStep, UseDefault }

/// <summary>Persisted reference to one property produced by a previous job step.</summary>
public sealed class ResultBinding
{
    [JsonPropertyName("source_step_id")]
    public string SourceStepId { get; set; } = string.Empty;

    /// <summary>Stable backend-owned property identity that survives CLR member renames.</summary>
    [JsonPropertyName("property_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? PropertyId { get; set; }

    /// <summary>Legacy CLR property path retained for backwards compatibility.</summary>
    [JsonPropertyName("property_path")]
    public string PropertyPath { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SourceStepId)
                                && (!string.IsNullOrWhiteSpace(PropertyId)
                                    || !string.IsNullOrWhiteSpace(PropertyPath));

}
