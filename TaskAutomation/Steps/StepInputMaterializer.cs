using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Steps;

/// <summary>Builds the handler-facing step from its persisted input references.</summary>
internal static class StepInputMaterializer
{
    public static JobStep MaterializeFields(JobStep source, IJobResultStore results, IReadOnlySet<string> fields)
    {
        if (!BuiltInStepDefinitions.Instance.TryGetByType(source.GetType(), out var definition)) return source;
        var draft = definition.CreateDraft(source);
        var overlay = StepInputValueResolver.Apply(source, definition, draft, reference =>
        {
            var read = ReadConfiguredReference(source, definition, results, reference);
            return (true, ValueReferenceResolver.ToJsonNode(read.Value));
        }, fields, requireResolved: true);
        if (overlay.Error is not null) throw new InvalidOperationException(overlay.Error);
        ValidateConfiguredInputs(definition, source, draft, results, fields);
        return definition.ApplyDraft(draft, Clone(source));
    }

    public static JobStep Materialize(JobStep source, IJobResultStore results, IStepDefinitionCatalog? catalog = null)
    {
        catalog ??= BuiltInStepDefinitions.Instance;
        if (!catalog.TryGetByType(source.GetType(), out var definition)) return source;

        var original = definition.CreateDraft(source);
        var draft = original.Clone();
        var overlay = StepInputValueResolver.Apply(source, definition, draft, reference =>
        {
            var read = ReadConfiguredReference(source, definition, results, reference);
            return (true, ValueReferenceResolver.ToJsonNode(read.Value));
        }, requireResolved: true);
        if (overlay.Error is not null) throw new InvalidOperationException(overlay.Error);
        ValidateConfiguredInputs(definition, source, draft, results);
        var changed = !draft.Values.All(pair => JsonNode.DeepEquals(
            original.Values.GetValueOrDefault(pair.Key), pair.Value));
        var clone = changed ? Clone(source) : source;
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
            {
                if (resolved.Status == RuntimeValueReadStatus.SourceUnavailable
                    && input.Binding.TryGetStepResult(out var reference)
                    && results.GetResultContract(reference.StepId) is { } sourceContract
                    && StepResultMetadata.TryGetProperty(sourceContract, input.Binding, out var sourceProperty)
                    && contract.Accepts(sourceProperty))
                {
                    // ROI feedback has a defined first-iteration fallback. Only its own
                    // persistent geometry state may be reused across iterations.
                    if (input.ContractId == "dynamicRoi" && sourceContract.TypeName == nameof(DynamicRoiResult)) continue;
                    if (contract.MissingValuePolicy == MissingValuePolicy.SkipStep)
                        throw new StepInputUnavailableException(input.ContractId);
                }
                throw new InvalidOperationException(resolved.Error ?? $"Die Eingabe '{input.ContractId}' konnte nicht aufgelöst werden.");
            }
            if (resolved.Descriptor is not { } descriptor || !contract.AcceptsSource(Provider(input.Binding), descriptor.ToResultProperty(), IsLegacyDirect(input.Binding, results)))
                throw new InvalidOperationException($"Die aufgelöste Eingabe '{input.ContractId}' besitzt nicht den erwarteten Typ.");
        }
    }

    private static void ValidateConfiguredInputs(IStepDefinition definition, JobStep step, StepDraft draft,
        IJobResultStore results, IReadOnlySet<string>? fields = null)
    {
        var active = StepActiveFieldResolver.GetActiveFieldIds(definition, draft);
        foreach (var field in definition.Descriptor.Fields.Where(field => active.Contains(field.Id)
                     && (fields is null || fields.Contains(field.Id))))
        {
            var binding = ValueBindingTree.Find(step.Inputs, field.Id);
            if (binding is null) continue;
            if (binding.HasProviderReference || binding.TryGetStepResult(out _))
            {
                var source = Read(binding);
                var contract = StepInputContractRegistry.Resolve(step.GetType(), field);
                if (!contract.AcceptsSource(Provider(binding), source, IsLegacyDirect(binding, results)))
                    throw new InvalidOperationException($"{field.Id}: StepValidation.Invalid");
            }
            ValidateChildren(binding, ValueBindingSchemaRegistry.ForField(field), field.Id);
        }

        ResultPropertyDescriptor Read(ResultBinding binding)
        {
            var read = ReadConfiguredReference(step, definition, results, binding);
            if (!read.IsSuccess || read.Descriptor is null)
                throw new InvalidOperationException(read.Error ?? "StepValidation.Invalid");
            return read.Descriptor.ToResultProperty();
        }

        void ValidateChildren(ResultBinding binding, string? schemaId, string path)
        {
            if (!binding.HasStructuredChildren) return;
            if (schemaId is null || !ValueBindingSchemaRegistry.TryGet(schemaId, out var schema))
                throw new InvalidOperationException($"{path}: StepValidation.Invalid");
            foreach (var (key, child) in binding.Members ?? [])
            {
                if (!child.IsConfigured) continue;
                if (!schema.Members.TryGetValue(key, out var member))
                    throw new InvalidOperationException($"{path}.{key}: StepValidation.Invalid");
                if ((child.HasProviderReference || child.TryGetStepResult(out _))
                    && !member.AcceptsSource(Provider(child), Read(child)))
                    throw new InvalidOperationException($"{path}.{key}: StepValidation.Invalid");
                ValidateChildren(child, member.NestedSchemaId, $"{path}.{key}");
            }
            if (binding.Items is null) return;
            if (schema.ItemSchemaId is null) throw new InvalidOperationException($"{path}: StepValidation.Invalid");
            for (var index = 0; index < binding.Items.Count; index++)
            {
                var child = binding.Items[index];
                if (!child.IsConfigured) continue;
                var expected = ValueBindingSchemaRegistry.ItemContract(schema.ItemSchemaId);
                if ((child.HasProviderReference || child.TryGetStepResult(out _))
                    && !expected.AcceptsSource(Provider(child), Read(child)))
                    throw new InvalidOperationException($"{path}.{index}: StepValidation.Invalid");
                ValidateChildren(child, schema.ItemSchemaId, $"{path}.{index}");
            }
        }
    }

    private static ResolvedValueReference ReadConfiguredReference(JobStep step, IStepDefinition definition,
        IJobResultStore results, ResultBinding binding)
    {
        var read = ValueReferenceResolver.Resolve(results, binding);
        if (read.IsSuccess) return read;
        var field = definition.Descriptor.Fields.FirstOrDefault(candidate =>
            ReferenceEquals(ValueBindingTree.Find(step.Inputs, candidate.Id), binding));
        if (field is not null && read.Status == RuntimeValueReadStatus.SourceUnavailable
            && binding.TryGetStepResult(out var reference)
            && results.GetResultContract(reference.StepId) is { } sourceContract
            && StepResultMetadata.TryGetProperty(sourceContract, binding, out var property))
        {
            var contract = StepInputContractRegistry.Resolve(step.GetType(), field);
            if (contract.MissingValuePolicy == MissingValuePolicy.SkipStep
                && contract.AcceptsSource(ValueProviderIds.StepResult, property))
                throw new StepInputUnavailableException(field.Id);
        }
        throw new InvalidOperationException(read.Error);
    }

    private static string Provider(ResultBinding binding) => binding.HasProviderReference
        ? binding.ProviderId : ValueProviderIds.StepResult;

    private static bool IsLegacyDirect(ResultBinding binding, IJobResultStore results) =>
        binding.ProviderId == ValueProviderIds.JobVariable && Guid.TryParse(binding.SourceId, out var id)
        && results.GetVariable(id)?.Scope == JobVariableScope.StepValue;

    private static JobStep Clone(JobStep source)
    {
        var json = JsonSerializer.Serialize<JobStep>(source);
        return JsonSerializer.Deserialize<JobStep>(json)
               ?? throw new InvalidOperationException($"Step '{source.GetType().Name}' konnte nicht materialisiert werden.");
    }
}
