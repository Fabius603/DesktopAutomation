using System.Text.Json.Serialization;

namespace TaskAutomation.Jobs;

public enum ResultValueKind
{
    Boolean, Integer, Number, Text, DateTime, Image, Point, Rectangle,
    Detection, ProcessReference, ResultObject
}

public enum ResultCardinality { Single, OptionalSingle, Collection }
public enum MissingValuePolicy { FailStep, SkipStep, UseDefault }

/// <summary>Persisted reference to one property produced by a previous job step.</summary>
public sealed class ResultBinding
{
    [JsonPropertyName("source_step_id")]
    public string SourceStepId { get; set; } = string.Empty;

    [JsonPropertyName("property_path")]
    public string PropertyPath { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SourceStepId)
                                && !string.IsNullOrWhiteSpace(PropertyPath);

}
