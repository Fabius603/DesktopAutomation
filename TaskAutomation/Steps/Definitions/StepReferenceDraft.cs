using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Steps.Definitions;

internal static class StepReferenceDraft
{
    public static JsonNode? Create(Guid? id, string? name) =>
        JsonSerializer.SerializeToNode(new StepReferenceValue(
            id?.ToString("D") ?? string.Empty,
            name ?? string.Empty));

    public static bool TryRead(StepDraft draft, string fieldId, out StepReferenceValue reference)
    {
        reference = new StepReferenceValue(string.Empty, string.Empty);
        if (!draft.Values.TryGetValue(fieldId, out var value) || value is null)
            return false;
        try
        {
            reference = value.Deserialize<StepReferenceValue>() ?? reference;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryGetId(
        StepDraft draft,
        string fieldId,
        out Guid id,
        out StepReferenceValue reference)
    {
        id = Guid.Empty;
        return TryRead(draft, fieldId, out reference)
            && Guid.TryParse(reference.Id, out id)
            && id != Guid.Empty;
    }
}
