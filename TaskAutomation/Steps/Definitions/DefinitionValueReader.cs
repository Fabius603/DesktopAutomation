using System.Text.Json;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

internal static class DefinitionValueReader
{
    public static string String(StepDraft draft, string id)
    {
        try { return draft.Values.GetValueOrDefault(id)?.GetValue<string>() ?? string.Empty; }
        catch (InvalidOperationException) { return string.Empty; }
    }

    public static int Integer(StepDraft draft, string id)
    {
        try { return draft.Values.GetValueOrDefault(id)?.GetValue<int>() ?? 0; }
        catch (InvalidOperationException) { return int.MinValue; }
    }

    public static double Number(StepDraft draft, string id)
    {
        try { return draft.Values.GetValueOrDefault(id)?.Deserialize<double>() ?? 0; }
        catch (JsonException) { return double.NaN; }
        catch (InvalidOperationException) { return double.NaN; }
    }

    public static bool Boolean(StepDraft draft, string id)
    {
        try { return draft.Values.GetValueOrDefault(id)?.GetValue<bool>() ?? false; }
        catch (InvalidOperationException) { return false; }
    }

    public static ResultBinding Binding(StepDraft draft, string id)
    {
        try { return draft.Values.GetValueOrDefault(id)?.Deserialize<ResultBinding>() ?? new ResultBinding(); }
        catch (JsonException) { return new ResultBinding(); }
    }
}
