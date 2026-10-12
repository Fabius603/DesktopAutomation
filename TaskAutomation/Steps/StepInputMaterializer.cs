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
            return new StepInputValueResolver.ReadResult(true, ValueReferenceResolver.ToJsonNode(read.Value),
                read.Descriptor?.ToResultProperty(), Provider(reference), IsLegacyDirect(reference, results));
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
            return new StepInputValueResolver.ReadResult(true, ValueReferenceResolver.ToJsonNode(read.Value),
                read.Descriptor?.ToResultProperty(), Provider(reference), IsLegacyDirect(reference, results));
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
            var contract = InputContractRegistry.Find(step.GetType(), input.ContractId)
                ?? throw new InvalidOperationException($"Für die Eingabe '{input.ContractId}' fehlt der Backend-Vertrag.");
            var resolved = ValueReferenceResolver.Resolve(results, input.Binding);
            if (!resolved.IsSuccess)
            {
                if (resolved.Status == RuntimeValueReadStatus.SourceUnavailable
                    && input.Binding.TryGetStepResult(out var reference)
                    && results.GetResultContract(reference.StepId) is { } sourceContract
                    && StepResultMetadata.TryGetProperty(sourceContract, input.Binding, out var sourceProperty)
                    && contract.IsShapeCompatible(sourceProperty, InputCompatibilityProfile.ExistingBinding))
                {
                    // ROI feedback has a defined first-iteration fallback. Only its own
                    // persistent geometry state may be reused across iterations.
                    if (input.ContractId == "dynamicRoi" && sourceContract.TypeName == nameof(DynamicRoiResult)) continue;
                    if (contract.MissingValuePolicy == MissingValuePolicy.SkipStep)
                        throw new StepInputUnavailableException(input.ContractId);
                }
                throw new InvalidOperationException(resolved.Error ?? $"Die Eingabe '{input.ContractId}' konnte nicht aufgelöst werden.");
            }
            if (resolved.Descriptor is not { } descriptor || !contract.EvaluateSource(Provider(input.Binding), descriptor.ToResultProperty(), InputCompatibilityProfile.ExistingBinding, IsLegacyDirect(input.Binding, results)).IsAccepted)
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
            foreach (var entry in InputBindingContractTraversal.Walk(binding,
                         InputContractRegistry.Resolve(step.GetType(), field), field.Id))
            {
                if (!(entry.Binding.HasProviderReference || entry.Binding.TryGetStepResult(out _))) continue;
                var read = ReadConfiguredReference(step, definition, results, entry.Binding);
                if (!read.IsSuccess || read.Descriptor is null
                    || !InputCompatibilityEvaluator.Evaluate(entry.Contract, Provider(entry.Binding),
                        read.Descriptor.ToResultProperty(), InputCompatibilityProfile.ExistingBinding,
                        IsLegacyDirect(entry.Binding, results)).IsAccepted)
                    throw new InvalidOperationException($"{entry.Path}: StepValidation.Invalid");
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
            var contract = InputContractRegistry.Resolve(step.GetType(), field);
            if (contract.MissingValuePolicy == MissingValuePolicy.SkipStep
                && contract.EvaluateSource(ValueProviderIds.StepResult, property, InputCompatibilityProfile.ExistingBinding).IsAccepted)
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
