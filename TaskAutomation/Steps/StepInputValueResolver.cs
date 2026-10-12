using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Steps;

/// <summary>One composition policy for known authoring inputs and fully resolved runtime inputs.</summary>
internal static class StepInputValueResolver
{
    internal sealed record Overlay(IReadOnlySet<string> UnresolvedPaths, string? Error = null);
    internal sealed record ReadResult(bool Resolved, JsonNode? Value, ResultPropertyDescriptor? Descriptor = null,
        string? ProviderId = null, bool IsLegacyDirectValue = false);

    public static Overlay Apply(JobStep step, IStepDefinition definition, StepDraft draft,
        Func<ResultBinding, ReadResult> read,
        IReadOnlySet<string>? fieldIds = null, bool requireResolved = false)
    {
        var unresolved = new HashSet<string>(StringComparer.Ordinal);
        var controls = definition.Descriptor.Fields.SelectMany(field =>
            (field.VisibleWhen is null ? Array.Empty<string>() : [field.VisibleWhen.FieldId])
            .Concat(field.VisibleWhenAll?.Select(rule => rule.FieldId) ?? []))
            .ToHashSet(StringComparer.Ordinal);
        try
        {
            foreach (var field in definition.Descriptor.Fields.Where(field => controls.Contains(field.Id)
                && StepDescriptorDraftValidator.IsVisible(field, draft)))
                ApplyField(field);
            var active = StepActiveFieldResolver.GetActiveFieldIds(definition, draft);
            foreach (var field in definition.Descriptor.Fields.Where(field => active.Contains(field.Id) && !controls.Contains(field.Id)))
                ApplyField(field);
            return new(unresolved);
        }
        catch (InvalidOperationException exception) { return new(unresolved, exception.Message); }

        void ApplyField(StepFieldDescriptor field)
        {
            if (fieldIds is not null && !fieldIds.Contains(field.Id)) return;
            if (!StepDescriptorDraftValidator.CanDetermineVisibility(field,
                    new StepValidationContext(StepValidationPhase.Authoring, unresolved)))
            { MarkUnresolved(field.Id); return; }
            var binding = ValueBindingTree.Find(step.Inputs, field.Id);
            if (binding?.IsConfigured == true)
            {
                _ = InputBindingContractTraversal.Walk(binding,
                    InputContractRegistry.Resolve(step.GetType(), field), field.Id).ToArray();
                draft.Values[field.Id] = field.ValueKind == StepValueKind.ResultBinding
                    ? JsonSerializer.SerializeToNode(binding)
                    : Compose(binding, draft.Values.GetValueOrDefault(field.Id), field.Id,
                        InputContractRegistry.Resolve(step.GetType(), field));
            }
            foreach (var (key, nested) in step.Inputs.Where(pair => pair.Key.StartsWith(field.Id + ".", StringComparison.Ordinal)))
            {
                var root = draft.Values.GetValueOrDefault(field.Id);
                if (root is null) throw Invalid(key);
                var path = key[(field.Id.Length + 1)..];
                var value = read(nested);
                if (!value.Resolved) { MarkUnresolved(key); continue; }
                var target = InputContractRegistry.ResolvePath(step.GetType(), field, key);
                var converted = ConvertValue(value, target, key);
                if (!StepDraftValueOverlay.TrySet(root, path, converted)) throw Invalid(key);
            }
        }

        JsonNode? Compose(ResultBinding binding, JsonNode? inherited, string path, InputContractNode contract)
        {
            var expectedSchema = contract.SchemaId;
            var node = inherited?.DeepClone();
            if (binding.HasProviderReference || binding.TryGetStepResult(out _))
            {
                var value = read(binding);
                if (value.Resolved) node = ConvertValue(value, contract, path);
                else MarkUnresolved(path);
            }
            InputContractSchema? schema = null;
            if (expectedSchema is not null)
            {
                if (!InputContractRegistry.TryGetSchema(expectedSchema, out schema!)
                    || binding.SchemaId is { Length: > 0 } actual && actual != expectedSchema)
                    throw Invalid(path);
                if (!unresolved.Contains(path) && (schema.ItemSchemaId is not null ? node is not JsonArray : node is not JsonObject))
                    throw Invalid(path);
            }
            foreach (var (key, child) in binding.Members ?? [])
            {
                if (!child.IsConfigured) continue;
                if (schema is not null && !schema.Members.ContainsKey(key)) throw Invalid(path + "." + key);
                if (node is null || !StepDraftValueOverlay.TryGet(node, key, out var previous)) throw Invalid(path + "." + key);
                var member = schema?.Members.GetValueOrDefault(key);
                if (member is null) throw Invalid(path + "." + key);
                var composed = Compose(child, previous, path + "." + key, member);
                if (!StepDraftValueOverlay.TrySet(node, key, composed)) throw Invalid(path + "." + key);
            }
            if (binding.Items is { Count: > 0 } items)
            {
                if (node is not JsonArray array || items.Count > array.Count || schema is { ItemSchemaId: null }) throw Invalid(path);
                for (var index = 0; index < items.Count; index++)
                    if (items[index].IsConfigured)
                        array[index] = Compose(items[index], array[index], $"{path}.{index}",
                            InputContractRegistry.Item(schema!.ItemSchemaId!));
            }
            return node;
        }

        void MarkUnresolved(string path)
        {
            unresolved.Add(path);
            if (requireResolved) throw Invalid(path);
        }
    }

    private static InvalidOperationException Invalid(string path) =>
        new($"{path}: StepValidation.Invalid");

    private static JsonNode? ConvertValue(ReadResult value, InputContractNode contract, string path)
    {
        if (value.Descriptor is null || value.ProviderId is null) throw Invalid(path);
        var result = InputCompatibilityEvaluator.Evaluate(contract, value.ProviderId, value.Descriptor,
            InputCompatibilityProfile.ExistingBinding, value.IsLegacyDirectValue);
        if (!result.IsAccepted || result.Plan is null) throw Invalid(path);
        return InputValueConverter.Convert(value.Value, result.Plan);
    }
}
