using System.Text.Json.Serialization;

namespace TaskAutomation.Jobs;

/// <summary>Provider-owned reference to exactly one typed value.</summary>
public class ValueReference
{
    [JsonPropertyName("provider_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ProviderId { get; set; } = string.Empty;

    [JsonPropertyName("source_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string SourceId { get; set; } = string.Empty;

    /// <summary>Optional path below the provider-owned source value.</summary>
    [JsonPropertyName("value_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ValuePath { get; set; }

    [JsonIgnore]
    public bool HasProviderReference => !string.IsNullOrWhiteSpace(ProviderId)
                                        && !string.IsNullOrWhiteSpace(SourceId);
}

public sealed record StepResultSourceId(string StepId, string PropertyId);

public static class StepResultSourceIdCodec
{
    private const string VersionPrefix = "v1/";

    public static string Create(string stepId, string propertyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyId);
        return $"{VersionPrefix}{Uri.EscapeDataString(stepId)}/{Uri.EscapeDataString(propertyId)}";
    }

    public static bool TryParse(string? sourceId, out StepResultSourceId result)
    {
        result = new(string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(sourceId) || !sourceId.StartsWith(VersionPrefix, StringComparison.Ordinal))
            return false;

        var separator = sourceId.IndexOf('/', VersionPrefix.Length);
        if (separator < 0 || separator == sourceId.Length - 1)
            return false;
        try
        {
            var stepId = Uri.UnescapeDataString(sourceId[VersionPrefix.Length..separator]);
            var propertyId = Uri.UnescapeDataString(sourceId[(separator + 1)..]);
            if (string.IsNullOrWhiteSpace(stepId) || string.IsNullOrWhiteSpace(propertyId))
                return false;
            result = new(stepId, propertyId);
            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}
