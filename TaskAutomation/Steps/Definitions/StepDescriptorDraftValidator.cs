using System.Text.Json.Nodes;
using System.Text.Json;
using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Steps.Definitions;

internal static class StepDescriptorDraftValidator
{
    public static IReadOnlyList<StepValidationIssue> Validate(
        StepDescriptor descriptor,
        StepDraft draft,
        StepValidationContext context)
    {
        var issues = new List<StepValidationIssue>();
        var fieldsById = descriptor.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var activeFields = StepEditorActivity.GetActiveFieldIds(
            descriptor,
            fieldId => context.IsResolved(fieldId) && TryGetStringValue(draft, fieldId, out var value) ? value : null,
            fieldId => CanDetermineVisibility(fieldsById[fieldId], context)
                       && IsVisible(fieldsById[fieldId], draft));
        foreach (var field in descriptor.Fields.Where(field =>
                     activeFields.Contains(field.Id) && IsVisible(field, draft)))
        {
            if (!context.IsResolved(field.Id) || !CanDetermineVisibility(field, context))
                continue;
            draft.Values.TryGetValue(field.Id, out var value);
            if (field.Required && IsEmpty(field, value))
            {
                issues.Add(new("StepValidation.Required", field.Id));
                continue;
            }
            if (value is null)
                continue;
            if (!TryReadComparable(field.ValueKind, value, out var number, out var text, out var length))
            {
                issues.Add(new(TypeError(field.ValueKind), field.Id));
                continue;
            }

            var constraints = field.Constraints;
            var allowedValues = constraints?.AllowedValues is { Count: > 0 }
                ? constraints.AllowedValues
                : field.Options?.Select(option => option.Value).ToArray();
            if (allowedValues is { Count: > 0 }
                && (text is null || !allowedValues.Contains(text, StringComparer.Ordinal)))
                issues.Add(new("StepValidation.Invalid", field.Id));
            if (constraints?.Minimum is { } minimum && number is { } numeric && numeric < minimum)
                issues.Add(new("StepValidation.Minimum", field.Id,
                    Arguments: new Dictionary<string, object?> { ["minimum"] = minimum }));
            if (constraints?.Maximum is { } maximum && number is { } numericMaximum && numericMaximum > maximum)
                issues.Add(new("StepValidation.Maximum", field.Id,
                    Arguments: new Dictionary<string, object?> { ["maximum"] = maximum }));
            if (constraints?.MinimumLength is { } minimumLength && length is { } actualMinimum && actualMinimum < minimumLength)
                issues.Add(new("StepValidation.Minimum", field.Id,
                    Arguments: new Dictionary<string, object?> { ["minimum"] = minimumLength }));
            if (constraints?.MaximumLength is { } maximumLength && length is { } actualMaximum && actualMaximum > maximumLength)
                issues.Add(new("StepValidation.Maximum", field.Id,
                    Arguments: new Dictionary<string, object?> { ["maximum"] = maximumLength }));
        }
        return issues;
    }

    private static bool CanDetermineVisibility(StepFieldDescriptor field, StepValidationContext context) =>
        (field.VisibleWhen is null || context.IsResolved(field.VisibleWhen.FieldId))
        && (field.VisibleWhenAll is not { Count: > 0 } rules
            || rules.All(rule => context.IsResolved(rule.FieldId)));

    internal static bool IsVisible(StepFieldDescriptor field, StepDraft draft) =>
        (field.VisibleWhen is null || RuleMatches(field.VisibleWhen, draft))
        && (field.VisibleWhenAll is not { Count: > 0 } rules || rules.All(rule => RuleMatches(rule, draft)));

    private static bool TryGetStringValue(StepDraft draft, string fieldId, out string value)
    {
        value = string.Empty;
        if (!draft.Values.TryGetValue(fieldId, out var node) || node is null)
            return false;
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            value = text;
            return true;
        }
        return false;
    }

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
        switch (kind)
        {
            case StepValueKind.Integer:
            case StepValueKind.Duration:
                if (value is not JsonValue integerValue) return false;
                if (integerValue.TryGetValue<int>(out var integer)) { number = integer; return true; }
                if (integerValue.TryGetValue<long>(out var longInteger)
                    && longInteger is >= int.MinValue and <= int.MaxValue)
                { number = longInteger; return true; }
                return false;
            case StepValueKind.Number:
                if (value is not JsonValue numberValue) return false;
                if (numberValue.TryGetValue<decimal>(out var decimalNumber)) { number = decimalNumber; return true; }
                if (numberValue.TryGetValue<double>(out var floatingPoint) && double.IsFinite(floatingPoint))
                { number = (decimal)floatingPoint; return true; }
                if (numberValue.TryGetValue<float>(out var singlePrecision) && float.IsFinite(singlePrecision))
                { number = (decimal)singlePrecision; return true; }
                if (numberValue.TryGetValue<long>(out var wholeNumber)) { number = wholeNumber; return true; }
                return false;
            case StepValueKind.Boolean:
                return value is JsonValue booleanValue && booleanValue.TryGetValue<bool>(out _);
            case StepValueKind.Text:
            case StepValueKind.DateTime:
            case StepValueKind.MultilineText:
            case StepValueKind.FilePath:
            case StepValueKind.DirectoryPath:
            case StepValueKind.Color:
            case StepValueKind.Enum:
                if (!TryGetString(value, out text)) return false;
                length = text.Length;
                return true;
            case StepValueKind.Collection:
                if (value is not JsonArray array) return false;
                length = array.Count; return true;
            case StepValueKind.ResultBinding:
                return TryDeserialize<TaskAutomation.Jobs.ResultBinding>(value, out _);
            default:
                return true;
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
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var result))
        { text = result; return true; }
        text = string.Empty;
        return false;
    }

    private static bool TryDeserialize<T>(JsonNode value, out T result) where T : class, new()
    {
        try { result = value.Deserialize<T>() ?? new T(); return true; }
        catch (System.Text.Json.JsonException) { result = new T(); return false; }
    }
}
