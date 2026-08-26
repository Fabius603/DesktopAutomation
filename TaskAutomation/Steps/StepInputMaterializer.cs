using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Steps;

/// <summary>Builds the handler-facing step from its persisted input references.</summary>
internal static class StepInputMaterializer
{
    public static JobStep Materialize(JobStep source, IJobResultStore results, IStepDefinitionCatalog? catalog = null)
    {
        catalog ??= BuiltInStepDefinitions.Instance;
        if (!catalog.TryGetByType(source.GetType(), out var definition)) return source;

        var sourceDraft = definition.CreateDraft(source);
        var activeFields = StepActiveFieldResolver.GetActiveFieldIds(definition, sourceDraft);
        var resolvedValues = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        var changed = false;
        foreach (var field in definition.Descriptor.Fields.Where(field => activeFields.Contains(field.Id)))
        {
            if (!source.Inputs.TryGetValue(field.Id, out var reference) || !reference.IsConfigured) continue;
            JsonNode? resolved;
            if (field.ValueKind == TaskAutomation.Contracts.Steps.StepValueKind.ResultBinding)
            {
                resolved = JsonSerializer.SerializeToNode(reference);
            }
            else
            {
                resolved = ResolveNode(results, reference, sourceDraft.Values.GetValueOrDefault(field.Id));
            }
            resolvedValues[field.Id] = resolved;
            changed |= !JsonNode.DeepEquals(sourceDraft.Values.GetValueOrDefault(field.Id), resolved);
        }
        foreach (var (key, reference) in source.Inputs.Where(input => input.Key.Contains('.')))
        {
            if (!reference.IsConfigured) continue;
            var separator = key.IndexOf('.');
            var fieldId = key[..separator];
            if (!activeFields.Contains(fieldId)) continue;
            var root = (resolvedValues.GetValueOrDefault(fieldId)
                        ?? sourceDraft.Values.GetValueOrDefault(fieldId))?.DeepClone();
            if (root is null) continue;
            var resolved = ValueReferenceResolver.ToJsonNode(Resolve(results, reference));
            if (!StepDraftValueOverlay.TrySet(root, key[(separator + 1)..], resolved))
                throw new InvalidOperationException($"Die Step-Eingabe '{key}' konnte nicht in den Zielwert eingesetzt werden.");
            resolvedValues[fieldId] = root;
            changed = true;
        }
        var clone = changed ? Clone(source) : source;
        var draft = definition.CreateDraft(clone);
        foreach (var (fieldId, value) in resolvedValues)
            draft.Values[fieldId] = value?.DeepClone();
        var issues = definition.ValidateDraft(
                draft,
                StepValidationContext.FullyResolved(StepValidationPhase.Runtime))
            .Where(candidate => candidate.Severity == StepValidationSeverity.Error)
            .ToArray();
        if (issues.Length > 0)
            throw new InvalidOperationException(
                $"Die aufgelösten Step-Eingaben sind ungültig: {string.Join(", ", issues.Select(issue => $"{issue.FieldId ?? definition.Descriptor.TypeId} ({issue.Code})"))}.");
        var materialized = changed ? definition.ApplyDraft(draft, clone) : source;
        ValidateRuntimeBindings(definition, materialized, results);
        return materialized;
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static object? Resolve(IJobResultStore results, ResultBinding reference)
    {
        var read = ValueReferenceResolver.Resolve(results, reference);
        if (!read.IsSuccess)
            throw new InvalidOperationException(read.Error ?? "Die Step-Eingabe konnte nicht aufgelöst werden.");
        return read.Value;
    }

    private static JsonNode? ResolveNode(
        IJobResultStore results,
        ResultBinding binding,
        JsonNode? inherited = null)
    {
        JsonNode? resolved = inherited?.DeepClone();
        if (binding.HasProviderReference || binding.TryGetStepResult(out _))
        {
            var value = Resolve(results, binding);
            resolved = value switch
            {
                null => null,
                JsonNode node => node.DeepClone(),
                _ => JsonSerializer.SerializeToNode(value, value.GetType())
            };
        }

        if (binding.Members is { Count: > 0 })
        {
            if (resolved is not JsonObject objectValue)
                resolved = objectValue = new JsonObject();
            foreach (var (memberId, childBinding) in binding.Members)
            {
                var property = objectValue.FirstOrDefault(candidate =>
                    Normalize(candidate.Key) == Normalize(memberId)).Key;
                if (string.IsNullOrEmpty(property)) property = memberId;
                objectValue[property] = ResolveNode(results, childBinding, objectValue[property]);
            }
        }

        if (binding.Items is { Count: > 0 })
        {
            if (resolved is not JsonArray arrayValue)
                resolved = arrayValue = [];
            while (arrayValue.Count < binding.Items.Count) arrayValue.Add(null);
            for (var index = 0; index < binding.Items.Count; index++)
                arrayValue[index] = ResolveNode(results, binding.Items[index], arrayValue[index]);
        }
        return resolved;
    }

    private static void ValidateRuntimeBindings(
        IStepDefinition definition,
        JobStep step,
        IJobResultStore results)
    {
        foreach (var input in definition.GetInputBindings(step))
        {
            var contract = StepInputContractRegistry.Get(step.GetType(), input.ContractId)
                ?? throw new InvalidOperationException($"Für die Eingabe '{input.ContractId}' fehlt der Backend-Vertrag.");
            var resolved = ValueReferenceResolver.Resolve(results, input.Binding);
            if (!resolved.IsSuccess)
                throw new InvalidOperationException(resolved.Error ?? $"Die Eingabe '{input.ContractId}' konnte nicht aufgelöst werden.");
            if (resolved.Descriptor is not { } descriptor || !contract.Accepts(descriptor.ToResultProperty()))
                throw new InvalidOperationException($"Die aufgelöste Eingabe '{input.ContractId}' besitzt nicht den erwarteten Typ.");
        }
    }

    private static JobStep Clone(JobStep source)
    {
        var json = JsonSerializer.Serialize<JobStep>(source);
        return JsonSerializer.Deserialize<JobStep>(json)
               ?? throw new InvalidOperationException($"Step '{source.GetType().Name}' konnte nicht materialisiert werden.");
    }
}
