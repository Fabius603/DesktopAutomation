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
        foreach (var (key, reference) in source.Inputs.Where(input => input.Key.Contains('.')))
        {
            if (!reference.IsConfigured) continue;
            var separator = key.IndexOf('.');
            var fieldId = key[..separator];
            if (!definition.Descriptor.Fields.Any(field => field.Id == fieldId)) continue;
            var root = (resolvedValues.GetValueOrDefault(fieldId)
                        ?? sourceDraft.Values.GetValueOrDefault(fieldId))?.DeepClone();
            if (root is null) continue;
            var value = Resolve(results, reference);
            var resolved = value switch
            {
                null => null,
                JsonNode node => node.DeepClone(),
                _ => JsonSerializer.SerializeToNode(value, value.GetType())
            };
            if (!TrySetNestedValue(root, key[(separator + 1)..], resolved)) continue;
            resolvedValues[fieldId] = root;
            changed = true;
        }
        if (!changed) return source;
        var clone = Clone(source);
        var draft = definition.CreateDraft(clone);
        foreach (var (fieldId, value) in resolvedValues)
            draft.Values[fieldId] = value?.DeepClone();
        return definition.ApplyDraft(draft, clone);
    }

    private static bool TrySetNestedValue(JsonNode root, string path, JsonNode? value)
    {
        var current = root;
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length; index++)
        {
            if (current is JsonArray arrayValue
                && int.TryParse(segments[index], out var arrayIndex)
                && arrayIndex >= 0 && arrayIndex < arrayValue.Count)
            {
                if (index == segments.Length - 1)
                {
                    arrayValue[arrayIndex] = value?.DeepClone();
                    return true;
                }
                if (arrayValue[arrayIndex] is not { } arrayChild) return false;
                current = arrayChild;
                continue;
            }
            if (current is not JsonObject objectValue) return false;
            var property = objectValue.FirstOrDefault(candidate =>
                Normalize(candidate.Key) == Normalize(segments[index])).Key;
            if (string.IsNullOrEmpty(property)) return false;
            if (index == segments.Length - 1)
            {
                objectValue[property] = value?.DeepClone();
                return true;
            }
            if (objectValue[property] is not { } child) return false;
            current = child;
        }
        return false;
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

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
