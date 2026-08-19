using System.Collections;
using System.Reflection;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Jobs;

public sealed record StepValidationResult(JobStep Step, bool IsValid, string? Error);
public sealed record JobValidationResult(bool IsValid, IReadOnlyList<StepValidationResult> Steps);

/// <summary>Zentrale Regeln fuer Step-Abhaengigkeiten. UI-Code darf diese Regeln nur anzeigen.</summary>
public static class JobValidation
{
    public static bool IsStepAllowed(Job job, JobStep step)
    {
        var section = GetSection(job, step);
        if (section == null) return false;
        var precedingPhases = ReferenceEquals(section, job.Steps)
            ? job.StartSteps
            : ReferenceEquals(section, job.EndSteps)
                ? job.StartSteps.Concat(job.Steps).ToList()
                : [];
        return ValidateStep(precedingPhases.Concat(section).ToList(), step).IsValid;
    }

    public static bool IsJobAllowed(Job job) => ValidateJob(job).IsValid;

    public static bool CanConfirm(IReadOnlyList<JobStep> precedingSteps, JobStep? candidate)
    {
        if (candidate == null) return false;
        var steps = precedingSteps.Concat([candidate]).ToList();
        return ValidateStep(steps, candidate).IsValid;
    }

    public static StepValidationResult ValidateCandidate(
        IReadOnlyList<JobStep> precedingSteps,
        JobStep? candidate,
        IReadOnlyList<JobStep>? allSteps = null,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null)
    {
        if (candidate == null) return new(null!, false, "Es konnte kein Step erstellt werden.");
        var steps = precedingSteps.Concat([candidate]).ToList();
        return ValidateStep(steps, candidate, allSteps, variables, providerSources);
    }

    public static bool IsSourceStepAllowed(IReadOnlyList<JobStep> steps, JobStep consumer, JobStep source)
    {
        var consumerIndex = IndexOf(steps, consumer);
        var sourceIndex = IndexOf(steps, source);
        return source.IsEnabled && sourceIndex >= 0 && consumerIndex >= 0 && sourceIndex < consumerIndex;
    }

    public static JobValidationResult ValidateJob(
        Job job,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null)
    {
        var variables = job.Variables ?? [];
        var results = ValidateSection(job.StartSteps, [], variables, providerSources ?? [])
            .Concat(ValidateSection(job.Steps, job.StartSteps, variables, providerSources ?? []))
            .Concat(ValidateSection(job.EndSteps, job.StartSteps.Concat(job.Steps).ToList(), variables, providerSources ?? []))
            .ToList();
        return new JobValidationResult(results.All(r => r.IsValid), results);
    }

    private static IReadOnlyList<StepValidationResult> ValidateSection(
        IReadOnlyList<JobStep> steps,
        IReadOnlyList<JobStep> precedingPhases,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources)
    {
        var executionOrder = precedingPhases.Concat(steps).ToList();
        var results = steps.Select(s => ValidateStep(
            executionOrder, s, variables: variables, providerSources: providerSources)).ToList();
        var structureErrors = GetIfStructureErrors(steps);
        results = results.Select(r => structureErrors.TryGetValue(r.Step, out var error)
            ? new StepValidationResult(r.Step, false, error) : r).ToList();
        return results;
    }

    private static IReadOnlyList<JobStep>? GetSection(Job job, JobStep step)
    {
        if (job.StartSteps.Contains(step)) return job.StartSteps;
        if (job.Steps.Contains(step)) return job.Steps;
        if (job.EndSteps.Contains(step)) return job.EndSteps;
        return null;
    }

    public static bool IsIfStructureAllowed(IReadOnlyList<JobStep> steps)
        => GetIfStructureErrors(steps).Count == 0;

    private static Dictionary<JobStep, string> GetIfStructureErrors(IReadOnlyList<JobStep> steps)
    {
        var errors = new Dictionary<JobStep, string>();
        var blocks = new Stack<(IfStep Step, bool SeenElse)>();
        foreach (var step in steps)
        {
            switch (step)
            {
                case IfStep current:
                    if (blocks.Count > 0) errors[current] = "Verschachtelte If-Bloecke sind nicht erlaubt.";
                    blocks.Push((current, false));
                    break;
                case ElseIfStep:
                    if (blocks.Count == 0) errors[step] = "ElseIf besitzt keinen zugehoerigen If-Step.";
                    else if (blocks.Peek().SeenElse) errors[step] = "ElseIf darf nicht hinter Else stehen.";
                    break;
                case ElseStep:
                    if (blocks.Count == 0) errors[step] = "Else besitzt keinen zugehoerigen If-Step.";
                    else if (blocks.Peek().SeenElse) errors[step] = "Der If-Block enthaelt mehr als einen Else-Step.";
                    else { var block = blocks.Pop(); blocks.Push((block.Step, true)); }
                    break;
                case EndIfStep:
                    if (blocks.Count == 0) errors[step] = "EndIf besitzt keinen zugehoerigen If-Step.";
                    else blocks.Pop();
                    break;
            }
        }
        foreach (var block in blocks) errors[block.Step] = "Fuer diesen If-Step fehlt ein EndIf-Step.";
        return errors;
    }

    public static StepValidationResult ValidateStep(
        IReadOnlyList<JobStep> steps,
        JobStep step,
        IReadOnlyList<JobStep>? referenceSteps = null,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null)
    {
        if (!step.IsEnabled)
            return new(step, true, null);

        var valueError = ValidateValues(step);
        if (valueError != null)
            return new(step, false, valueError);

        var index = IndexOf(steps, step);
        if (ValidateResultBindings(steps, index, step, variables ?? [], providerSources ?? []) is { } bindingError)
            return new(step, false, bindingError);
        if (step is IfStep ifStep && ValidateConditions(steps, index, ifStep.Settings.Conditions, variables ?? [], providerSources ?? []) is { } ifError)
            return new(step, false, ifError);
        if (step is ElseIfStep elseIfStep && ValidateConditions(steps, index, elseIfStep.Settings.Conditions, variables ?? [], providerSources ?? []) is { } elseIfError)
            return new(step, false, elseIfError);

        // Concrete bindings and their value shapes were validated above. There is no
        // additional result-group validation because outputs are step-specific.
        return new(step, true, null);
    }

    private static string? ValidateValues(JobStep step)
    {
        const string invalid = "Der Step enthaelt ungueltige oder unvollstaendige Werte.";
        if (!BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
            return invalid;
        if (definition.Descriptor.Fields.Count > 0
            && definition.Descriptor.Fields.All(field =>
                step.Inputs.TryGetValue(field.Id, out var reference) && reference.IsConfigured))
            return null;
        var hasError = definition.ValidateDraft(definition.CreateDraft(step))
            .Any(issue => issue.Severity == StepValidationSeverity.Error);
        return hasError ? invalid : null;
    }

    private static string? ValidateConditions(
        IReadOnlyList<JobStep> steps,
        int conditionStepIndex,
        IEnumerable<StepCondition> conditions,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources)
    {
        foreach (var condition in conditions)
        {
            var property = ResolveConditionProperty(
                steps, conditionStepIndex, variables, providerSources, condition);
            if (property is null) return "Eine Bedingung verweist nicht auf eine gültige Referenz.";
            if (!ConditionRules.IsOperatorAllowed(property.DataType, condition.Operator))
                return "Der Operator passt nicht zum Datentyp der ausgewählten Eigenschaft.";
            if (!ConditionRules.RequiresComparisonValue(condition.Operator)) continue;

            var comparison = condition.EffectiveComparison;
            if (comparison.Kind == ComparisonOperandKind.Literal)
            {
                if (!ConditionRules.IsComparisonValueValid(property, condition.Operator, comparison.Value))
                    return "Der Vergleichswert besitzt nicht den erwarteten Datentyp.";
                continue;
            }

            var comparisonProperty = ResolveConditionProperty(
                steps, conditionStepIndex, variables, providerSources, comparison);
            if (comparisonProperty is null)
                return "Die ausgewählte Vergleichsreferenz existiert nicht mehr.";
            if (!StepResultMetadata.AreComparable(property, comparisonProperty))
                return "Beide Vergleichswerte müssen denselben Datentyp besitzen.";
        }
        return null;
    }

    private static ResultPropertyDescriptor? ResolveConditionProperty(
        IReadOnlyList<JobStep> steps,
        int conditionStepIndex,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources,
        ResultBinding binding)
    {
        if (binding.HasProviderReference
            && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
        {
            var providerSource = ResolveProviderSource(variables, providerSources, binding);
            return providerSource is { IsSensitive: false }
                ? providerSource.ToResultProperty()
                : null;
        }
        var source = steps.Take(conditionStepIndex)
            .FirstOrDefault(step => step.Id == binding.SourceStepId && step.IsEnabled);
        return source is null
            ? null
            : FindProperty(StepResultMetadata.GetResultTypeForStep(source), binding.PropertyId, binding.PropertyPath);
    }

    private static string? ValidateResultBindings(
        IReadOnlyList<JobStep> steps,
        int consumerIndex,
        JobStep step,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources)
    {
        if (!BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
            return "FÃ¼r den Step fehlt die Backend-Definition.";
        if (step.Inputs.Count == 0)
            return ValidateLegacyResultBindings(steps, consumerIndex, step, definition, variables, providerSources);
        foreach (var field in definition.Descriptor.Fields)
        {
            var key = field.Id;
            var binding = step.Inputs.GetValueOrDefault(key) ?? new ResultBinding();
            var contract = !string.IsNullOrWhiteSpace(field.InputContractId)
                ? StepInputContractRegistry.Get(step.GetType(), field.InputContractId)
                : StepInputContractRegistry.ForField(field);
            if (contract is null) return $"Für die Eingabe '{key}' fehlt der Backend-Vertrag.";
            if (!binding.IsConfigured)
            {
                return $"Für die Eingabe '{key}' wurde keine Variable ausgewählt.";
            }

            if (binding.HasProviderReference
                && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
            {
                var providerSource = ResolveProviderSource(variables, providerSources, binding);
                if (providerSource is null)
                    return "Eine Referenz verweist auf eine nicht vorhandene Wertquelle.";
                if (!contract.AcceptedShapes.Any(shape =>
                        shape.Accepts(providerSource.ValueKind, providerSource.Cardinality)))
                    return $"Die Wertquelle '{providerSource.Name}' ist für die Eingabe '{key}' nicht erlaubt.";
                continue;
            }

            var source = steps.Take(Math.Max(0, consumerIndex))
                .FirstOrDefault(candidate => candidate.Id == binding.SourceStepId && candidate.IsEnabled);
            if (source is null) return "Eine Ergebnis-Eigenschaft verweist nicht auf einen gültigen vorherigen Step.";
            var resultType = StepResultMetadata.GetResultTypeForStep(source);
            if (resultType is null
                || !StepResultMetadata.TryGetProperty(resultType, binding, out var property))
                return $"Die Ergebnis-Eigenschaft '{binding.PropertyId ?? binding.PropertyPath}' existiert für den Quell-Step nicht.";
            if (!contract.Accepts(property))
                return $"Die Ergebnis-Eigenschaft '{property.DisplayName}' ist für die Eingabe '{key}' nicht erlaubt.";
        }
        return null;
    }

    private static string? ValidateLegacyResultBindings(
        IReadOnlyList<JobStep> steps,
        int consumerIndex,
        JobStep step,
        IStepDefinition definition,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources)
    {
        foreach (var configuredInput in definition.GetInputBindings(step))
        {
            var key = configuredInput.ContractId;
            var binding = configuredInput.Binding;
            var contract = StepInputContractRegistry.Get(step.GetType(), key);
            if (contract is null) return $"Für die Eingabe '{key}' fehlt der Backend-Vertrag.";
            if (!binding.IsConfigured)
            {
                if (contract.Required && !HasReadableLegacyInput(step, key))
                    return $"Für die Eingabe '{key}' wurde keine Ergebnis-Eigenschaft ausgewählt.";
                continue;
            }
            if (binding.HasProviderReference
                && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
            {
                var providerSource = ResolveProviderSource(variables, providerSources, binding);
                if (providerSource is null)
                    return "Eine Referenz verweist auf eine nicht vorhandene Wertquelle.";
                if (!contract.AcceptedShapes.Any(shape =>
                        shape.Accepts(providerSource.ValueKind, providerSource.Cardinality)))
                    return $"Die Wertquelle '{providerSource.Name}' ist für die Eingabe '{key}' nicht erlaubt.";
                continue;
            }
            var source = steps.Take(Math.Max(0, consumerIndex))
                .FirstOrDefault(candidate => candidate.Id == binding.SourceStepId && candidate.IsEnabled);
            if (source is null) return "Eine Ergebnis-Eigenschaft verweist nicht auf einen gültigen vorherigen Step.";
            var resultType = StepResultMetadata.GetResultTypeForStep(source);
            if (resultType is null || !StepResultMetadata.TryGetProperty(resultType, binding, out var property))
                return $"Die Ergebnis-Eigenschaft '{binding.PropertyId ?? binding.PropertyPath}' existiert für den Quell-Step nicht.";
            if (!contract.Accepts(property))
                return $"Die Ergebnis-Eigenschaft '{property.DisplayName}' ist für die Eingabe '{key}' nicht erlaubt.";
        }
        return null;
    }

    private static bool HasReadableLegacyInput(JobStep step, string contractId) =>
        step is DynamicRoiStep dynamicRoi
        && string.Equals(contractId, "padding", StringComparison.Ordinal)
        && dynamicRoi.Settings.Padding >= 0;

    private static ValueProviderSourceDescriptor? ResolveProviderSource(
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources,
        ValueReference reference)
    {
        if (string.Equals(reference.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
            && Guid.TryParse(reference.SourceId, out var variableId)
            && variables.FirstOrDefault(variable => variable.Id == variableId) is { } variable)
            return ValueProviderSourceDescriptor.FromVariable(variable);

        var source = providerSources.FirstOrDefault(candidate =>
            string.Equals(candidate.ProviderId, reference.ProviderId, StringComparison.Ordinal)
            && string.Equals(candidate.SourceId, reference.SourceId, StringComparison.OrdinalIgnoreCase));
        if (source is not null) return source;
        return providerSources.Count == 0
               && string.Equals(reference.ProviderId, ValueProviderIds.Secret, StringComparison.Ordinal)
               && Guid.TryParse(reference.SourceId, out _)
            ? new ValueProviderSourceDescriptor(
                ValueProviderIds.Secret,
                reference.SourceId,
                "Secret",
                string.Empty,
                ResultValueKind.Text,
                ResultCardinality.Single,
                IsSensitive: true)
            : null;
    }

    private static ResultPropertyDescriptor? FindProperty(
        ResultTypeDescriptor? resultType,
        string? propertyId,
        string? propertyPath)
    {
        if (resultType is null) return null;
        return resultType.Properties.FirstOrDefault(property =>
                   !string.IsNullOrWhiteSpace(propertyId)
                   && property.StableId.Equals(propertyId, StringComparison.OrdinalIgnoreCase))
               ?? resultType.Properties.FirstOrDefault(property =>
                   property.Name.Equals(propertyPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Entfernt nur Referenzen auf Steps, die nicht mehr existieren.
    /// Voruebergehend ungueltige Referenzen (deaktivierter Step oder falsche Reihenfolge)
    /// bleiben erhalten, damit sie nach Reaktivieren oder Zurueckverschieben wieder gueltig werden.
    /// </summary>
    public static void RemoveInvalidSourceSelections(IReadOnlyList<JobStep> steps)
    {
        var existingIds = steps.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        for (var i = 0; i < steps.Count; i++)
        {
            VisitSourceProperties(steps[i], (owner, property) =>
            {
                if (property.GetValue(owner) is string id && id.Length > 0 && !existingIds.Contains(id) && property.CanWrite)
                    property.SetValue(owner, string.Empty);
            });
        }
    }

    private static void VisitSourceProperties(object? value, Action<object, PropertyInfo> visitor, HashSet<object>? seen = null)
    {
        if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum) return;
        seen ??= new(ReferenceEqualityComparer.Instance);
        if (!seen.Add(value)) return;
        if (value is IEnumerable sequence) { foreach (var item in sequence) VisitSourceProperties(item, visitor, seen); return; }
        if (value.GetType().Namespace != typeof(JobStep).Namespace) return;
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0) continue;
            if (property.PropertyType == typeof(string) && property.Name.StartsWith("Source", StringComparison.Ordinal) && property.Name.EndsWith("StepId", StringComparison.Ordinal))
                visitor(value, property);
            else if (property.PropertyType != typeof(string))
                VisitSourceProperties(property.GetValue(value), visitor, seen);
        }
    }

    private static int IndexOf(IReadOnlyList<JobStep> steps, JobStep step)
    {
        for (var i = 0; i < steps.Count; i++) if (ReferenceEquals(steps[i], step)) return i;
        return -1;
    }
}
