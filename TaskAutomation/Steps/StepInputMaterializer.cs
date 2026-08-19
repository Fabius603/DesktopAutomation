using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Steps;

/// <summary>Builds the handler-facing step from its persisted input references.</summary>
internal static class StepInputMaterializer
{
    public static JobStep Materialize(JobStep source, IJobResultStore results, IStepDefinitionCatalog? catalog = null)
    {
        if (source.Inputs is not { Count: > 0 }) return source;
        catalog ??= BuiltInStepDefinitions.Instance;
        if (!catalog.TryGetByType(source.GetType(), out var definition)) return source;

        var sourceDraft = definition.CreateDraft(source);
        var resolvedValues = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        var changed = false;
        foreach (var field in definition.Descriptor.Fields)
        {
            if (!source.Inputs.TryGetValue(field.Id, out var reference) || !reference.IsConfigured) continue;
            JsonNode? resolved;
            if (field.ValueKind == TaskAutomation.Contracts.Steps.StepValueKind.ResultBinding)
            {
                resolved = JsonSerializer.SerializeToNode(reference);
            }
            else
            {
                var value = Resolve(results, reference);
                resolved = value switch
                {
                    null => null,
                    JsonNode node => node.DeepClone(),
                    _ => JsonSerializer.SerializeToNode(value, value.GetType())
                };
            }
            resolvedValues[field.Id] = resolved;
            changed |= !JsonNode.DeepEquals(sourceDraft.Values.GetValueOrDefault(field.Id), resolved);
        }
        if (!changed) return source;
        var clone = Clone(source);
        var draft = definition.CreateDraft(clone);
        foreach (var (fieldId, value) in resolvedValues)
            draft.Values[fieldId] = value?.DeepClone();
        return definition.ApplyDraft(draft, clone);
    }

    private static object? Resolve(IJobResultStore results, ResultBinding reference)
    {
        if (reference.HasProviderReference
            && !string.Equals(reference.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
        {
            var read = results.ReadProvider(reference.ProviderId, reference.SourceId);
            if (!read.IsSuccess)
                throw new InvalidOperationException(read.Error ?? "Die Step-Eingabe konnte nicht aufgelöst werden.");
            return read.Value;
        }
        if (!reference.TryGetStepResult(out var source))
            throw new InvalidOperationException("Die Step-Eingabe enthält keine gültige Referenz.");
        var result = results.GetRaw(source.StepId)
            ?? throw new InvalidOperationException($"Der Quell-Step '{source.StepId}' wurde noch nicht ausgeführt.");
        if (!StepResultMetadata.TryGetProperty(result.GetType(), source.PropertyId, source.PropertyId, out var property)
            || !StepResultMetadata.TryReadValue(result, property, out var value))
            throw new InvalidOperationException($"Die Ergebnis-Eigenschaft '{source.PropertyId}' ist nicht verfügbar.");
        return value;
    }

    private static JobStep Clone(JobStep source)
    {
        var json = JsonSerializer.Serialize<JobStep>(source);
        return JsonSerializer.Deserialize<JobStep>(json)
               ?? throw new InvalidOperationException($"Step '{source.GetType().Name}' konnte nicht materialisiert werden.");
    }
}
