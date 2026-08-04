using System.Text.Json;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed record StepInputBinding(string ContractId, ResultBinding Binding);

internal static class StepInputBindingReader
{
    public static IReadOnlyList<StepInputBinding> Read(StepDescriptor descriptor, StepDraft draft)
    {
        var result = new List<StepInputBinding>();
        var fieldsById = descriptor.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var activeFields = StepEditorActivity.GetActiveFieldIds(
            descriptor,
            fieldId => TryGetString(draft, fieldId),
            fieldId => StepDescriptorDraftValidator.IsVisible(fieldsById[fieldId], draft));
        foreach (var field in descriptor.Fields)
        {
            if (!activeFields.Contains(field.Id) || !StepDescriptorDraftValidator.IsVisible(field, draft))
                continue;
            if (!draft.Values.TryGetValue(field.Id, out var value) || value is null)
                continue;
            if (field.ValueKind == StepValueKind.ResultBinding && field.InputContractId is { } directContract)
                Add(result, directContract, Deserialize<ResultBinding>(value));
            else if (field.EditorHint is StepEditorHints.ProcessTargetPicker or StepEditorHints.ExecutableProcessTargetPicker
                     && field.InputContractId is { } processContract
                     && ProcessSelectorDraft.TryRead(draft, field.Id, out _, out var processBinding))
                Add(result, processContract, processBinding);
            else if (field.EditorHint == StepEditorHints.PointEntryList && field.InputContractId is { } pointsContract)
            {
                foreach (var point in Deserialize<List<StepPointEntryValue>>(value) ?? [])
                    if (point.Source == "JobResult") Add(result, pointsContract, Deserialize<ResultBinding>(point.PointsSource));
            }
            else if (field.RoiPickerOptions is { } roiOptions)
            {
                var roi = Deserialize<StepRoiSelectionValue>(value);
                Add(result, roiOptions.DynamicInputContractId, Deserialize<ResultBinding>(roi?.DynamicSource));
            }
            else if (field.VisualOverlayOptions is { } overlayOptions)
            {
                var overlay = Deserialize<VisualOverlaySettings>(value) ?? new VisualOverlaySettings();
                foreach (var binding in overlay.DetectionResults ?? []) Add(result, overlayOptions.DetectionInputContractId, binding);
                foreach (var entry in overlay.TextResults ?? []) Add(result, overlayOptions.TextInputContractId, entry.Result);
            }
        }
        return result;
    }

    private static string? TryGetString(StepDraft draft, string fieldId)
    {
        if (!draft.Values.TryGetValue(fieldId, out var value) || value is null)
            return null;
        try { return value.GetValue<string>(); }
        catch (InvalidOperationException) { return null; }
    }

    private static void Add(List<StepInputBinding> bindings, string contractId, ResultBinding? binding)
    {
        if (binding is not null) bindings.Add(new(contractId, binding));
    }

    private static T? Deserialize<T>(System.Text.Json.Nodes.JsonNode? value)
    {
        try { return value is null ? default : value.Deserialize<T>(); }
        catch (JsonException) { return default; }
        catch (InvalidOperationException) { return default; }
    }
}
