using System.Text.Json.Nodes;
using System.Text.Json;
using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Steps.Definitions;

internal static class StepDescriptorDraftValidator
{
    public static IReadOnlyList<StepValidationIssue> Validate(StepDescriptor descriptor, StepDraft draft)
    {
        foreach (var field in descriptor.Fields.Where(field => IsVisible(field, draft)))
        {
            draft.Values.TryGetValue(field.Id, out var value);
            if (field.Required && IsEmpty(field, value))
                return [new("StepValidation.Required", field.Id)];
            if (value is null)
                continue;
            if (!TryReadComparable(field.ValueKind, value, out var number, out var text, out var length))
                return [new(TypeError(field.ValueKind), field.Id)];

            var constraints = field.Constraints;
            if (constraints?.AllowedValues is { Count: > 0 }
                && (text is null || !constraints.AllowedValues.Contains(text, StringComparer.Ordinal)))
                return [new("StepValidation.Invalid", field.Id)];
            if (constraints?.Minimum is { } minimum && number is { } numeric && numeric < minimum)
                return [new("StepValidation.Minimum", field.Id,
                    Arguments: new Dictionary<string, object?> { ["minimum"] = minimum })];
            if (constraints?.Maximum is { } maximum && number is { } numericMaximum && numericMaximum > maximum)
                return [new("StepValidation.Maximum", field.Id,
                    Arguments: new Dictionary<string, object?> { ["maximum"] = maximum })];
            if (constraints?.MinimumLength is { } minimumLength && length is { } actualMinimum && actualMinimum < minimumLength)
                return [new("StepValidation.Minimum", field.Id,
                    Arguments: new Dictionary<string, object?> { ["minimum"] = minimumLength })];
            if (constraints?.MaximumLength is { } maximumLength && length is { } actualMaximum && actualMaximum > maximumLength)
                return [new("StepValidation.Maximum", field.Id,
                    Arguments: new Dictionary<string, object?> { ["maximum"] = maximumLength })];
        }
        return [];
    }

    internal static bool IsVisible(StepFieldDescriptor field, StepDraft draft) =>
        (field.VisibleWhen is null || RuleMatches(field.VisibleWhen, draft))
        && (field.VisibleWhenAll is not { Count: > 0 } rules || rules.All(rule => RuleMatches(rule, draft)));

    private static bool RuleMatches(StepVisibilityRule rule, StepDraft draft)
    {
        if (!draft.Values.TryGetValue(rule.FieldId, out var value))
            return false;
        return rule.AnyOfValues is { Count: > 0 } choices
            ? choices.Any(choice => JsonNode.DeepEquals(value, choice))
            : JsonNode.DeepEquals(value, rule.EqualsValue);
    }

    private static bool IsEmpty(StepFieldDescriptor field, JsonNode? value)
    {
        if (value is null) return true;
        if (field.ValueKind is StepValueKind.Text or StepValueKind.MultilineText or StepValueKind.FilePath
            or StepValueKind.DirectoryPath or StepValueKind.Color or StepValueKind.Enum)
            return !TryGetString(value, out var text) || string.IsNullOrWhiteSpace(text);
        if (field.ValueKind == StepValueKind.ResultBinding)
            return !TryDeserialize<TaskAutomation.Jobs.ResultBinding>(value, out var binding) || !binding.IsConfigured;
        if (field.ValueKind == StepValueKind.Collection)
            return value is not JsonArray { Count: > 0 };
        return false;
    }

    private static bool TryReadComparable(
        StepValueKind kind, JsonNode value, out decimal? number, out string? text, out int? length)
    {
        number = null; text = null; length = null;
        try
        {
            switch (kind)
            {
                case StepValueKind.Integer:
                case StepValueKind.Duration:
                    number = value.GetValue<int>(); return true;
                case StepValueKind.Number:
                    var numeric = value.Deserialize<double>();
                    if (!double.IsFinite(numeric)) return false;
                    number = (decimal)numeric; return true;
                case StepValueKind.Boolean:
                    _ = value.GetValue<bool>(); return true;
                case StepValueKind.Text:
                case StepValueKind.MultilineText:
                case StepValueKind.FilePath:
                case StepValueKind.DirectoryPath:
                case StepValueKind.Color:
                case StepValueKind.Enum:
                    text = value.GetValue<string>(); length = text.Length; return true;
                case StepValueKind.Collection:
                    if (value is not JsonArray array) return false;
                    length = array.Count; return true;
                case StepValueKind.ResultBinding:
                    return TryDeserialize<TaskAutomation.Jobs.ResultBinding>(value, out _);
                default:
                    return true;
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException or OverflowException)
        {
            return false;
        }
    }

    private static string TypeError(StepValueKind kind) => kind switch
    {
        StepValueKind.Integer or StepValueKind.Duration => "StepValidation.Integer",
        StepValueKind.Boolean => "StepValidation.Boolean",
        _ => "StepValidation.Invalid"
    };

    private static bool TryGetString(JsonNode value, out string text)
    {
        try { text = value.GetValue<string>(); return true; }
        catch (InvalidOperationException) { text = string.Empty; return false; }
    }

    private static bool TryDeserialize<T>(JsonNode value, out T result) where T : class, new()
    {
        try { result = value.Deserialize<T>() ?? new T(); return true; }
        catch (System.Text.Json.JsonException) { result = new T(); return false; }
    }
}
