using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Steps.Definitions;

internal static class StepActiveFieldResolver
{
    public static IReadOnlySet<string> GetActiveFieldIds(IStepDefinition definition, StepDraft draft)
    {
        var fields = definition.Descriptor.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        return StepEditorActivity.GetActiveFieldIds(
            definition.Descriptor,
            fieldId => TryGetString(draft, fieldId),
            fieldId => StepDescriptorDraftValidator.IsVisible(fields[fieldId], draft));
    }

    private static string? TryGetString(StepDraft draft, string fieldId)
    {
        if (!draft.Values.TryGetValue(fieldId, out var value) || value is null) return null;
        try { return value.GetValue<string>(); }
        catch (InvalidOperationException) { return null; }
    }
}
