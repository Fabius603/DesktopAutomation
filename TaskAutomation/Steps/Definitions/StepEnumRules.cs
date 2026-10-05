using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

/// <summary>
/// Owns the persisted token semantics of closed step-field choices.
/// Labels and picker state are presentation concerns and must never normalize these values.
/// </summary>
public static class StepEnumRules
{
    public static bool TryRead<TEnum>(string? token, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        return token is not null && Enum.GetNames<TEnum>().Contains(token, StringComparer.Ordinal)
            && Enum.TryParse(token, ignoreCase: false, out value);
    }

    public static void ApplyMetadata(JobVariable variable, string enumTypeName, StepFieldDescriptor field)
    {
        if (field.ValueKind != StepValueKind.Enum || variable.ValueKind != ResultValueKind.Enum) return;
        variable.EnumTypeName = enumTypeName;
        var options = GetOptions(field);
        variable.EnumValues = options.Select(option => option.Value).Distinct(StringComparer.Ordinal).ToList();
        variable.EnumDisplayNames = options.Where(option => !string.IsNullOrWhiteSpace(option.DisplayName))
            .ToDictionary(option => option.Value, option => option.DisplayName!, StringComparer.Ordinal);
        if (variable.EnumDisplayNames.Count == 0) variable.EnumDisplayNames = null;
    }

    public static IReadOnlyList<StepFieldOptionDescriptor> GetOptions(StepFieldDescriptor field) =>
        field.Options is { Count: > 0 }
            ? field.Options
            : (field.Constraints?.AllowedValues ?? [])
                .Select(value => new StepFieldOptionDescriptor(value, value, value))
                .ToArray();

    public static bool IsKnownToken(StepFieldDescriptor field, string? token) =>
        token is not null
        && GetOptions(field).Any(option => string.Equals(option.Value, token, StringComparison.Ordinal));

    public static bool TryReadToken(JsonNode? value, out string token)
    {
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            token = text;
            return true;
        }

        token = string.Empty;
        return false;
    }

    public static bool TryReadDefaultToken(StepFieldDescriptor field, out string token) =>
        TryReadToken(field.DefaultValue, out token) && IsKnownToken(field, token);
}
