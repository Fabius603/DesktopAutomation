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

    public static Overlay Apply(JobStep step, IStepDefinition definition, StepDraft draft,
        Func<ResultBinding, (bool Resolved, JsonNode? Value)> read,
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
                draft.Values[field.Id] = field.ValueKind == StepValueKind.ResultBinding
                    ? JsonSerializer.SerializeToNode(binding)
                    : Compose(binding, draft.Values.GetValueOrDefault(field.Id), field.Id,
                        ValueBindingSchemaRegistry.ForField(field));
            foreach (var (key, nested) in step.Inputs.Where(pair => pair.Key.StartsWith(field.Id + ".", StringComparison.Ordinal)))
            {
                var root = draft.Values.GetValueOrDefault(field.Id);
                if (root is null) throw Invalid(key);
                var path = key[(field.Id.Length + 1)..];
                var value = read(nested);
                if (!value.Resolved) { MarkUnresolved(key); continue; }
                if (!StepDraftValueOverlay.TrySet(root, path, value.Value)) throw Invalid(key);
            }
        }

        JsonNode? Compose(ResultBinding binding, JsonNode? inherited, string path, string? expectedSchema)
        {
            var node = inherited?.DeepClone();
            if (binding.HasProviderReference || binding.TryGetStepResult(out _))
            {
                var value = read(binding);
                if (value.Resolved) node = value.Value?.DeepClone();
                else MarkUnresolved(path);
            }
            ValueBindingSchemaDescriptor? schema = null;
            if (expectedSchema is not null)
            {
                if (!ValueBindingSchemaRegistry.TryGet(expectedSchema, out schema!)
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
                var composed = Compose(child, previous, path + "." + key, schema?.Members.GetValueOrDefault(key)?.NestedSchemaId);
                if (!StepDraftValueOverlay.TrySet(node, key, composed)) throw Invalid(path + "." + key);
            }
            if (binding.Items is { Count: > 0 } items)
            {
                if (node is not JsonArray array || items.Count > array.Count || schema is { ItemSchemaId: null }) throw Invalid(path);
                for (var index = 0; index < items.Count; index++)
                    if (items[index].IsConfigured)
                        array[index] = Compose(items[index], array[index], $"{path}.{index}", schema?.ItemSchemaId);
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
}
