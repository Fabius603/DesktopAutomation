using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Steps.Definitions;

/// <summary>
/// Owns the persisted token semantics of closed step-field choices.
/// Labels and picker state are presentation concerns and must never normalize these values.
/// </summary>
public static class StepEnumRules
{
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
