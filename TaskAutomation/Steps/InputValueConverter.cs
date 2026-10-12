using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Steps;

public static class InputValueConverter
{
    public static JsonNode? Convert(JsonNode? value, InputConversionPlan plan)
    {
        // A missing value remains missing; admitting OptionalSingle never supplies a value.
        if (value is null) return null;
        return plan.Conversion switch
        {
            InputConversion.Exact => value.DeepClone(),
            InputConversion.DisplayText => JsonValue.Create(TextValueFormatter.Format(value)),
            InputConversion.LegacyColor => value is JsonValue color && color.TryGetValue<string>(out var token)
                && ColorValueRules.TryParse(token, out _) ? value.DeepClone() : throw Invalid(),
            InputConversion.LegacyFilePath => value is JsonValue path && path.TryGetValue<string>(out _)
                ? value.DeepClone() : throw Invalid(),
            _ => throw Invalid()
        };
    }

    private static InvalidOperationException Invalid() => new("StepValidation.Invalid");
}
