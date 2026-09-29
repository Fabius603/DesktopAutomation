using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

internal static class DefinitionValueReader
{
    public static bool TryInteger(JsonNode? node, out int result)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue<int>(out result)) return true;
            if (value.TryGetValue<long>(out var longResult)
                && longResult is >= int.MinValue and <= int.MaxValue)
            { result = (int)longResult; return true; }
        }
        result = 0;
        return false;
    }

    public static bool TryBoolean(JsonNode? node, out bool result)
    {
        if (node is JsonValue value && value.TryGetValue<bool>(out result)) return true;
        result = false;
        return false;
    }

    public static string String(StepDraft draft, string id)
    {
        return draft.Values.GetValueOrDefault(id) is JsonValue value
               && value.TryGetValue<string>(out var result) ? result : string.Empty;
    }

    public static TEnum Enum<TEnum>(StepDraft draft, string id) where TEnum : struct, Enum
    {
        if (TryEnum<TEnum>(draft, id, out var result))
            return result;
        var token = String(draft, id);
        throw new InvalidOperationException($"Field '{id}' contains unknown {typeof(TEnum).Name} token '{token}'.");
    }

    public static bool TryEnum<TEnum>(StepDraft draft, string id, out TEnum result)
        where TEnum : struct, Enum =>
        System.Enum.TryParse(String(draft, id), ignoreCase: false, out result)
        && System.Enum.IsDefined(result);

    public static int Integer(StepDraft draft, string id)
    {
        return TryInteger(draft.Values.GetValueOrDefault(id), out var result) ? result : int.MinValue;
    }

    public static double Number(StepDraft draft, string id)
    {
        if (draft.Values.GetValueOrDefault(id) is not JsonValue value) return 0;
        if (value.TryGetValue<double>(out var result)) return result;
        if (value.TryGetValue<float>(out var floatResult)) return floatResult;
        if (value.TryGetValue<decimal>(out var decimalResult)) return (double)decimalResult;
        if (value.TryGetValue<long>(out var integerResult)) return integerResult;
        return double.NaN;
    }

    public static bool Boolean(StepDraft draft, string id)
    {
        return draft.Values.GetValueOrDefault(id) is JsonValue value
               && value.TryGetValue<bool>(out var result) && result;
    }

    public static ResultBinding Binding(StepDraft draft, string id)
    {
        try { return draft.Values.GetValueOrDefault(id)?.Deserialize<ResultBinding>() ?? new ResultBinding(); }
        catch (JsonException) { return new ResultBinding(); }
    }
}
