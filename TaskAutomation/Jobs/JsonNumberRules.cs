using System.Text.Json;
using System.Text.Json.Nodes;

namespace TaskAutomation.Jobs;

/// <summary>Number values use the same finite double representation before and after JSON storage.</summary>
public static class JsonNumberRules
{
    public static bool TryReadFiniteDouble(JsonNode? node, out double number)
    {
        number = default;
        if (node is not JsonValue value) return false;
        try
        {
            if (value.TryGetValue<double>(out number)) return double.IsFinite(number);
            if (value.TryGetValue<float>(out var single) && !float.IsFinite(single)) return false;
            if (!value.TryGetValue<float>(out _) && !value.TryGetValue<decimal>(out _)
                && !value.TryGetValue<int>(out _) && !value.TryGetValue<long>(out _)) return false;
            // Use the JSON numeric representation, including float round-trip formatting.
            number = value.Deserialize<double>();
            return double.IsFinite(number);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        { return false; }
    }

    public static double ReadFiniteDouble(JsonNode? node) => TryReadFiniteDouble(node, out var number)
        ? number : throw new InvalidOperationException("StepValidation.Invalid");
}
