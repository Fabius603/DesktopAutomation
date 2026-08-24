using System.Text.Json.Serialization;

namespace TaskAutomation.Jobs;

/// <summary>
/// A typed value reference used by existing result-consuming steps. New files use
/// provider_id/source_id; the legacy fields remain readable for existing jobs.
/// </summary>
public class ResultBinding : ValueReference
{
    private string? _legacySourceStepId;
    private string? _legacyPropertyId;
    private string? _legacyPropertyPath;

    [JsonPropertyName("schema_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SchemaId { get; set; }

    [JsonPropertyName("members")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, ResultBinding>? Members { get; set; }

    [JsonPropertyName("items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<ResultBinding>? Items { get; set; }

    [JsonPropertyName("source_step_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? LegacySourceStepId
    {
        get => _legacySourceStepId;
        set => _legacySourceStepId = value;
    }

    [JsonPropertyName("property_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? LegacyPropertyId
    {
        get => _legacyPropertyId;
        set => _legacyPropertyId = value;
    }

    [JsonPropertyName("property_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? LegacyPropertyPath
    {
        get => _legacyPropertyPath;
        set => _legacyPropertyPath = value;
    }

    [JsonIgnore]
    public string SourceStepId
    {
        get => TryGetStepResult(out var source) ? source.StepId : _legacySourceStepId ?? string.Empty;
        set => _legacySourceStepId = string.IsNullOrEmpty(value) ? null : value;
    }

    [JsonIgnore]
    public string? PropertyId
    {
        get => TryGetStepResult(out var source) ? source.PropertyId : _legacyPropertyId;
        set => _legacyPropertyId = value;
    }

    [JsonIgnore]
    public string PropertyPath
    {
        get => _legacyPropertyPath ?? string.Empty;
        set => _legacyPropertyPath = string.IsNullOrEmpty(value) ? null : value;
    }

    [JsonIgnore]
    public bool IsConfigured => HasProviderReference
                                || (!string.IsNullOrWhiteSpace(_legacySourceStepId)
                                    && (!string.IsNullOrWhiteSpace(_legacyPropertyId)
                                        || !string.IsNullOrWhiteSpace(_legacyPropertyPath)))
                                || Members?.Values.Any(binding => binding.IsConfigured) == true
                                || Items?.Any(binding => binding.IsConfigured) == true;

    [JsonIgnore]
    public bool HasStructuredChildren => Members is { Count: > 0 } || Items is { Count: > 0 };

    public static ResultBinding ForStepResult(string stepId, string propertyId) => new()
    {
        ProviderId = ValueProviderIds.StepResult,
        SourceId = StepResultSourceIdCodec.Create(stepId, propertyId)
    };

    public bool TryGetStepResult(out StepResultSourceId source)
    {
        if (string.Equals(ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal)
            && StepResultSourceIdCodec.TryParse(SourceId, out source))
            return true;
        source = new(_legacySourceStepId ?? string.Empty, _legacyPropertyId ?? _legacyPropertyPath ?? string.Empty);
        return !string.IsNullOrWhiteSpace(source.StepId)
               && !string.IsNullOrWhiteSpace(source.PropertyId);
    }
}
