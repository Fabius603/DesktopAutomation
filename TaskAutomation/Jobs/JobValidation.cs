using System.Collections;
using System.Reflection;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs.ControlFlow;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Jobs;

public sealed record StepValidationResult(
    JobStep Step,
    bool IsValid,
    string? Error,
    IReadOnlyList<string>? Errors = null);
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
        var variables = (job.Variables ?? []).Cast<JobVariable>()
            .Concat(job.LocalValues ?? []).ToArray();
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
        var structureErrors = GetControlFlowStructureErrors(steps);
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

    public static bool IsControlFlowStructureAllowed(IReadOnlyList<JobStep> steps)
        => ControlFlowStructureAnalyzer.Analyze(steps).IsValid;

    public static bool IsIfStructureAllowed(IReadOnlyList<JobStep> steps)
        => IsControlFlowStructureAllowed(steps);

    private static Dictionary<JobStep, string> GetControlFlowStructureErrors(IReadOnlyList<JobStep> steps)
    {
        return ControlFlowStructureAnalyzer.Analyze(steps).Diagnostics
            .GroupBy(diagnostic => diagnostic.Step)
            .ToDictionary(
                group => group.Key,
                group => string.Join(Environment.NewLine, group.Select(FormatControlFlowDiagnostic)));
    }

    private static string FormatControlFlowDiagnostic(ControlFlowDiagnostic diagnostic)
    {
        return diagnostic.Code switch
        {
            ControlFlowDiagnosticCodes.OrphanSection when diagnostic.Step is ElseIfStep
                => "ElseIf besitzt keinen zugehoerigen If-Step.",
            ControlFlowDiagnosticCodes.OrphanSection => "Else besitzt keinen zugehoerigen If-Step.",
            ControlFlowDiagnosticCodes.SectionAfterElse => "ElseIf darf nicht hinter Else stehen.",
            ControlFlowDiagnosticCodes.DuplicateElse => "Der If-Block enthaelt mehr als einen Else-Step.",
            ControlFlowDiagnosticCodes.OrphanEnd => "EndIf besitzt keinen zugehoerigen If-Step.",
            ControlFlowDiagnosticCodes.MissingEnd => "Fuer diesen If-Step fehlt ein EndIf-Step.",
            _ => diagnostic.Code
        };
    }

    public static StepValidationResult ValidateStep(
        IReadOnlyList<JobStep> steps,
        JobStep step,
        IReadOnlyList<JobStep>? referenceSteps = null,
        IReadOnlyList<JobVariable>? variables = null,
        IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null)
    {
        if (!step.IsEnabled && step.CanBeDisabled)
            return new(step, true, null);

        var index = IndexOf(steps, step);
        var errors = new List<string>();
        errors.AddRange(ValidateResultBindings(steps, index, step, variables ?? [], providerSources ?? []));
        errors.AddRange(ValidateValues(step, variables ?? []));
        if (step is IfStep or ElseIfStep)
        {
            var settings = ReadEffectiveConditionSettings(step, variables ?? []);
            errors.AddRange(ValidateConditions(
                steps, index, settings?.Conditions, variables ?? [], providerSources ?? []));
        }

        return errors.Count == 0
            ? new(step, true, null, [])
            : new(step, false, string.Join(Environment.NewLine, errors), errors);
    }

    private static IReadOnlyList<string> ValidateValues(JobStep step, IReadOnlyList<JobVariable> variables)
    {
        if (!BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
            return ["Für den Step fehlt die Backend-Definition."];
        var draft = definition.CreateDraft(step);
        SupplyLegacyValuesForUnifiedFields(step, draft);
        var overlay = OverlayKnownInputValues(step, definition, draft, variables);
        if (overlay.Error is not null) return [overlay.Error];
        return definition.ValidateDraft(
                draft,
                new StepValidationContext(StepValidationPhase.Authoring, overlay.UnresolvedPaths))
            .Where(issue => issue.Severity == StepValidationSeverity.Error)
            .Select(issue => $"{issue.FieldId ?? definition.Descriptor.TypeId}: {issue.Code}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record KnownValueOverlay(IReadOnlySet<string> UnresolvedPaths, string? Error = null);

    private static KnownValueOverlay OverlayKnownInputValues(
        JobStep step,
        IStepDefinition definition,
        StepDraft draft,
        IReadOnlyList<JobVariable> variables)
    {
        var unresolved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in definition.Descriptor.Fields)
        {
            var binding = ValueBindingTree.Find(step.Inputs, field.Id);
            if (binding?.IsConfigured != true) continue;
            if (field.ValueKind == StepValueKind.ResultBinding)
            {
                draft.Values[field.Id] = System.Text.Json.JsonSerializer.SerializeToNode(binding);
                continue;
            }
            if (!binding.HasProviderReference && !binding.TryGetStepResult(out _))
                continue;
            if (TryReadKnownValue(binding, variables, out var value))
                draft.Values[field.Id] = value?.DeepClone();
            else
                unresolved.Add(field.Id);
        }

        foreach (var (path, binding) in ValueBindingTree.EnumerateReferences(step.Inputs)
                     .Where(candidate => candidate.Path.Contains('.')))
        {
            if (!TryReadKnownValue(binding, variables, out var value))
            {
                unresolved.Add(path);
                continue;
            }
            var separator = path.IndexOf('.');
            var fieldId = path[..separator];
            if (draft.Values.GetValueOrDefault(fieldId) is not { } root
                || !StepDraftValueOverlay.TrySet(root, path[(separator + 1)..], value))
                return new(unresolved, $"Die Eingabe '{path}' passt nicht zur Struktur des Zielfelds.");
        }
        return new(unresolved);
    }

    private static bool TryReadKnownValue(
        ResultBinding binding,
        IReadOnlyList<JobVariable> variables,
        out System.Text.Json.Nodes.JsonNode? value)
    {
        value = null;
        if (!binding.HasProviderReference
            || !Guid.TryParse(binding.SourceId, out var sourceId)
            || (binding.ProviderId != ValueProviderIds.LocalValue
                && binding.ProviderId != ValueProviderIds.JobVariable))
            return false;
        var variable = variables.FirstOrDefault(candidate => candidate.Id == sourceId);
        if (variable is null) return false;
        return ValueReferenceResolver.TryReadVariable(variable, binding.ValuePath, out value, out _);
    }

    private static IfConditionSettings? ReadEffectiveConditionSettings(
        JobStep step,
        IReadOnlyList<JobVariable> variables)
    {
        if (!BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
            return step switch
            {
                IfStep condition => condition.Settings,
                ElseIfStep condition => condition.Settings,
                _ => null
            };
        var draft = definition.CreateDraft(step);
        SupplyLegacyValuesForUnifiedFields(step, draft);
        var overlay = OverlayKnownInputValues(step, definition, draft, variables);
        return overlay.Error is null ? ConditionStepDefinitionSupport.ReadSettings(draft) : null;
    }

    private static IReadOnlyList<string> ValidateConditions(
        IReadOnlyList<JobStep> steps,
        int conditionStepIndex,
        IEnumerable<StepCondition>? conditions,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources)
    {
        if (conditions is null) return ["Die Bedingungen besitzen kein gültiges Format."];
        var errors = new List<string>();
        var sourceSteps = steps.Take(Math.Max(0, conditionStepIndex))
            .Where(step => step.IsEnabled && !string.IsNullOrWhiteSpace(step.Id))
            .GroupBy(step => step.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var conditionIndex = 0;
        foreach (var condition in conditions)
        {
            conditionIndex++;
            if (condition is null)
            {
                errors.Add($"Bedingung {conditionIndex} besitzt kein gültiges Format.");
                continue;
            }
            var property = ResolveConditionProperty(
                sourceSteps, variables, providerSources, condition);
            if (property is null)
            {
                errors.Add($"Bedingung {conditionIndex} verweist nicht auf eine gültige Referenz.");
                continue;
            }
            if (property.Cardinality == ResultCardinality.Collection)
            {
                errors.Add($"Bedingung {conditionIndex} benötigt einen einzelnen Wert statt einer Sammlung.");
                continue;
            }
            if (!ConditionRules.IsOperatorAllowed(property.DataType, condition.Operator))
            {
                errors.Add($"In Bedingung {conditionIndex} passt der Operator nicht zum Datentyp der ausgewählten Eigenschaft.");
                continue;
            }
            if (!ConditionRules.RequiresComparisonValue(condition.Operator)) continue;

            var comparison = condition.EffectiveComparison;
            if (!Enum.IsDefined(comparison.Kind))
            {
                errors.Add($"Bedingung {conditionIndex} verwendet eine unbekannte Vergleichsart.");
                continue;
            }
            if (comparison.Kind == ComparisonOperandKind.Literal)
            {
                if (!ConditionRules.IsComparisonValueValid(property, condition.Operator, comparison.Value))
                    errors.Add($"Der Vergleichswert in Bedingung {conditionIndex} besitzt nicht den erwarteten Datentyp.");
                continue;
            }

            var comparisonProperty = ResolveConditionProperty(
                sourceSteps, variables, providerSources, comparison);
            if (comparisonProperty is null)
            {
                errors.Add($"Die ausgewählte Vergleichsreferenz in Bedingung {conditionIndex} existiert nicht mehr.");
                continue;
            }
            if (comparisonProperty.Cardinality == ResultCardinality.Collection)
            {
                errors.Add($"Die Vergleichsreferenz in Bedingung {conditionIndex} muss einen einzelnen Wert liefern.");
                continue;
            }
            var isDirectLocalValue = string.Equals(
                comparison.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal);
            var directComparisonValue = isDirectLocalValue
                && Guid.TryParse(comparison.SourceId, out var comparisonValueId)
                && variables.FirstOrDefault(variable => variable.Id == comparisonValueId) is LocalValue local
                && local.Value is System.Text.Json.Nodes.JsonValue jsonValue
                && jsonValue.TryGetValue<string>(out var storedValue)
                    ? storedValue
                    : null;
            if (!ConditionRules.AreComparisonSourcesCompatible(
                    property, comparisonProperty, isDirectLocalValue, directComparisonValue))
                errors.Add($"Beide Vergleichswerte in Bedingung {conditionIndex} müssen denselben Datentyp besitzen.");
        }
        return errors;
    }

    private static ResultPropertyDescriptor? ResolveConditionProperty(
        IReadOnlyDictionary<string, JobStep> sourceSteps,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources,
        ResultBinding binding)
    {
        if (binding.HasProviderReference
            && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
        {
            if (binding.ProviderId is ValueProviderIds.LocalValue or ValueProviderIds.JobVariable
                && Guid.TryParse(binding.SourceId, out var storedValueId)
                && variables.FirstOrDefault(variable => variable.Id == storedValueId) is { } storedValue
                && !TryValidateStoredValue(storedValue, binding.ValuePath, out _))
                return null;
            var providerSource = ResolveProviderSource(variables, providerSources, binding);
            return providerSource is { IsSensitive: false }
                ? providerSource.ToResultProperty()
                : null;
        }
        return !sourceSteps.TryGetValue(binding.SourceStepId, out var source)
            ? null
            : FindProperty(StepResultMetadata.GetResultTypeForStep(source), binding.PropertyId, binding.PropertyPath);
    }

    private static IReadOnlyList<string> ValidateResultBindings(
        IReadOnlyList<JobStep> steps,
        int consumerIndex,
        JobStep step,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources)
    {
        if (!BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
            return ["Für den Step fehlt die Backend-Definition."];
        var errors = new HashSet<string>(StringComparer.Ordinal);
        var draft = definition.CreateDraft(step);
        var activityOverlay = OverlayKnownInputValues(step, definition, draft, variables);
        if (activityOverlay.Error is not null) return [activityOverlay.Error];
        var activeFields = StepActiveFieldResolver.GetActiveFieldIds(definition, draft);
        var semanticInputs = definition.GetInputBindings(step);
        foreach (var field in definition.Descriptor.Fields.Where(field => activeFields.Contains(field.Id)))
        {
            var key = field.Id;
            var binding = ValueBindingTree.Find(step.Inputs, key) ?? new ResultBinding();
            var contract = StepInputContractRegistry.Resolve(step.GetType(), field);
            if (!binding.IsConfigured)
            {
                if (field.Required && field.ValueKind == StepValueKind.ResultBinding
                    && !HasReadableLegacyInput(step, field.InputContractId ?? key)
                    && !semanticInputs.Any(input => string.Equals(
                        input.ContractId, field.InputContractId ?? key, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"Für die Eingabe '{key}' wurde keine Variable ausgewählt.");
                continue;
            }

            var structuredError = ValidateStructuredBinding(
                binding, steps, consumerIndex, variables, providerSources, key);
            if (structuredError is not null)
            {
                errors.Add(structuredError);
                continue;
            }
            if (binding.HasProviderReference || binding.TryGetStepResult(out _))
                if (ValidateConfiguredBinding(
                        steps, consumerIndex, variables, providerSources, contract, binding, key) is { } error)
                    errors.Add(error);
        }

        foreach (var input in semanticInputs)
        {
            var contract = StepInputContractRegistry.Get(step.GetType(), input.ContractId);
            if (contract is null)
            {
                errors.Add($"Für die Eingabe '{input.ContractId}' fehlt der Backend-Vertrag.");
                continue;
            }
            if (ValidateConfiguredBinding(
                    steps, consumerIndex, variables, providerSources,
                    contract, input.Binding, input.ContractId) is { } error)
                errors.Add(error);
        }
        return errors.ToArray();
    }

    private static string? ValidateConfiguredBinding(
        IReadOnlyList<JobStep> steps,
        int consumerIndex,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources,
        StepInputDescriptor contract,
        ResultBinding binding,
        string key)
    {
        if (!binding.IsConfigured)
            return contract.Required ? $"Für die Eingabe '{key}' wurde keine Variable ausgewählt." : null;
        if (binding.HasProviderReference
            && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
        {
            var providerSource = ResolveProviderSource(variables, providerSources, binding);
            if (providerSource is null)
                return "Eine Referenz verweist auf eine nicht vorhandene Wertquelle.";
            if (binding.ProviderId is ValueProviderIds.LocalValue or ValueProviderIds.JobVariable
                && Guid.TryParse(binding.SourceId, out var storedValueId)
                && variables.FirstOrDefault(variable => variable.Id == storedValueId) is { } storedValue
                && !TryValidateStoredValue(storedValue, binding.ValuePath, out var valueError))
                return valueError;
            var directValue = string.Equals(binding.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal)
                              || string.Equals(binding.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
                              && Guid.TryParse(binding.SourceId, out var variableId)
                              && variables.Any(variable => variable.Id == variableId
                                                           && variable.Scope == JobVariableScope.StepValue);
            if (directValue ? !contract.AllowsDirectValue : !contract.AllowsProvider(binding.ProviderId))
                return $"Die Wertquelle '{providerSource.Name}' ist für die Eingabe '{key}' nicht erlaubt.";
            if (!contract.Accepts(providerSource.ToResultProperty()))
                return $"Die Wertquelle '{providerSource.Name}' ist für die Eingabe '{key}' nicht erlaubt.";
            return null;
        }

        var source = steps.Take(Math.Max(0, consumerIndex))
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id, binding.SourceStepId, StringComparison.OrdinalIgnoreCase) && candidate.IsEnabled);
        if (source is null) return "Eine Ergebnis-Eigenschaft verweist nicht auf einen gültigen vorherigen Step.";
        var resultType = StepResultMetadata.GetResultTypeForStep(source);
        if (resultType is null || !StepResultMetadata.TryGetProperty(resultType, binding, out var property))
            return $"Die Ergebnis-Eigenschaft '{binding.PropertyId ?? binding.PropertyPath}' existiert für den Quell-Step nicht.";
        return contract.Accepts(property)
            ? null
            : $"Die Ergebnis-Eigenschaft '{property.DisplayName}' ist für die Eingabe '{key}' nicht erlaubt.";
    }

    private static bool TryValidateStoredValue(
        JobVariable variable,
        string? valuePath,
        out string? error)
    {
        error = null;
        try
        {
            var value = JobVariableRuntimeValueReader.Read(variable);
            if (string.IsNullOrWhiteSpace(valuePath)) return true;
            if (value is not null && ResultBindingResolver.TryReadPath(value, valuePath, out _)) return true;
            error = $"Die Untereigenschaft '{valuePath}' ist in der Wertquelle '{variable.Name}' nicht verfügbar.";
            return false;
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException
            or InvalidOperationException
            or FormatException
            or ArgumentException
            or System.IO.IOException
            or UnauthorizedAccessException
            or System.Runtime.InteropServices.ExternalException)
        {
            error = $"Die Wertquelle '{variable.Name}' enthält keinen gültigen Wert für den Typ '{variable.ValueKind}'.";
            return false;
        }
    }

    private static string? ValidateStructuredBinding(
        ResultBinding binding,
        IReadOnlyList<JobStep> steps,
        int consumerIndex,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources,
        string path)
    {
        if (!binding.HasStructuredChildren) return null;
        if (string.IsNullOrWhiteSpace(binding.SchemaId)
            || !ValueBindingSchemaRegistry.TryGet(binding.SchemaId, out var schema))
            return $"Die zusammengesetzte Eingabe '{path}' verwendet kein bekanntes Wertschema.";

        if (binding.Members is not null)
        {
            foreach (var (memberId, child) in binding.Members)
            {
                if (!schema.Members.TryGetValue(memberId, out var member))
                    return $"Das Feld '{path}.{memberId}' gehört nicht zum Wertschema '{schema.Id}'.";
                var error = ValidateStructuredChild(
                    child, member, steps, consumerIndex, variables, providerSources, $"{path}.{memberId}");
                if (error is not null) return error;
            }
        }

        if (binding.Items is not null)
        {
            if (schema.ItemSchemaId is null)
                return $"Die Eingabe '{path}' ist laut Wertschema keine Collection.";
            for (var index = 0; index < binding.Items.Count; index++)
            {
                var child = binding.Items[index];
                if (!child.HasStructuredChildren) continue;
                var error = ValidateStructuredBinding(
                    child, steps, consumerIndex, variables, providerSources, $"{path}.{index}");
                if (error is not null) return error;
            }
        }
        return null;
    }

    private static string? ValidateStructuredChild(
        ResultBinding binding,
        ValueBindingMemberDescriptor expected,
        IReadOnlyList<JobStep> steps,
        int consumerIndex,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources,
        string path)
    {
        // Missing members use the value stored in the step settings. Older editors also
        // persisted empty placeholders for direct members such as roi.enabled.
        if (!binding.IsConfigured) return null;
        if (binding.HasProviderReference || binding.TryGetStepResult(out _))
        {
            if (binding.HasProviderReference
                && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
            {
                var source = ResolveProviderSource(variables, providerSources, binding);
                if (source is null) return "Eine Referenz verweist auf eine nicht vorhandene Wertquelle.";
                if (expected.AllowedProviderIds?.Contains(binding.ProviderId) == false)
                    return $"Die Wertquelle '{source.Name}' ist für '{path}' nicht erlaubt.";
                if (source.ValueKind != expected.ValueKind
                    || !CardinalityMatches(source.Cardinality, expected.Cardinality))
                    return $"Die Wertquelle '{source.Name}' besitzt nicht den erwarteten Typ für '{path}'.";
            }
            else
            {
                if (expected.AllowedProviderIds?.Contains(ValueProviderIds.StepResult) == false)
                    return $"Step-Ergebnisse sind für '{path}' nicht erlaubt.";
                var sourceStep = steps.Take(Math.Max(0, consumerIndex))
                    .FirstOrDefault(candidate => string.Equals(
                        candidate.Id, binding.SourceStepId, StringComparison.OrdinalIgnoreCase) && candidate.IsEnabled);
                if (sourceStep is null)
                    return "Eine Ergebnis-Eigenschaft verweist nicht auf einen gültigen vorherigen Step.";
                var resultType = StepResultMetadata.GetResultTypeForStep(sourceStep);
                if (resultType is null || !StepResultMetadata.TryGetProperty(resultType, binding, out var property))
                    return $"Die Ergebnis-Eigenschaft für '{path}' existiert nicht.";
                if (property.DataType != expected.ValueKind
                    || !CardinalityMatches(property.Cardinality, expected.Cardinality))
                    return $"Die Ergebnis-Eigenschaft '{property.DisplayName}' besitzt nicht den erwarteten Typ für '{path}'.";
            }
        }
        return binding.HasStructuredChildren
            ? ValidateStructuredBinding(binding, steps, consumerIndex, variables, providerSources, path)
            : null;
    }

    private static bool CardinalityMatches(ResultCardinality actual, ResultCardinality expected) =>
        actual == expected
        || expected == ResultCardinality.Single && actual == ResultCardinality.OptionalSingle;

    private static string? ValidateLegacyResultBindings(
        IReadOnlyList<JobStep> steps,
        int consumerIndex,
        JobStep step,
        IStepDefinition definition,
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources)
    {
        var configuredInputs = definition.GetInputBindings(step).ToList();
        if (step is FileSystemOperationStep fileSystem)
        {
            if (fileSystem.Settings.SourceMode == FileSystemPathSource.TaskResult)
            {
                if (!fileSystem.Settings.SourceResult.IsConfigured)
                    return "Für die Eingabe 'source' wurde keine Ergebnis-Eigenschaft ausgewählt.";
                configuredInputs.Add(new StepInputBinding("source", fileSystem.Settings.SourceResult));
            }
            if (fileSystem.Settings.Operation is FileSystemOperation.Copy or FileSystemOperation.Move
                && fileSystem.Settings.TargetMode == FileSystemPathSource.TaskResult)
            {
                if (!fileSystem.Settings.TargetResult.IsConfigured)
                    return "Für die Eingabe 'target' wurde keine Ergebnis-Eigenschaft ausgewählt.";
                configuredInputs.Add(new StepInputBinding("target", fileSystem.Settings.TargetResult));
            }
        }
        foreach (var configuredInput in configuredInputs)
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
                var directValue = string.Equals(
                    binding.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal);
                if (directValue ? !contract.AllowsDirectValue : !contract.AllowsProvider(binding.ProviderId))
                    return $"Die Wertquelle '{providerSource.Name}' ist für die Eingabe '{key}' nicht erlaubt.";
                if (!contract.Accepts(providerSource.ToResultProperty()))
                    return $"Die Wertquelle '{providerSource.Name}' ist für die Eingabe '{key}' nicht erlaubt.";
                continue;
            }
            var source = steps.Take(Math.Max(0, consumerIndex))
                .FirstOrDefault(candidate => string.Equals(
                    candidate.Id, binding.SourceStepId, StringComparison.OrdinalIgnoreCase) && candidate.IsEnabled);
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
        step switch
        {
            DynamicRoiStep dynamicRoi when string.Equals(contractId, "padding", StringComparison.Ordinal)
                => dynamicRoi.Settings.Padding >= 0,
            ShowTextStep showText when string.Equals(contractId, "text", StringComparison.Ordinal)
                => showText.Settings.TextSource == ShowTextSource.ExplicitText
                   && !string.IsNullOrWhiteSpace(showText.Settings.Text),
            _ => false
        };

    private static void SupplyLegacyValuesForUnifiedFields(JobStep step, StepDraft draft)
    {
        if (step is FileSystemOperationStep fileSystem)
        {
            if (fileSystem.Settings.SourceMode == FileSystemPathSource.TaskResult
                && fileSystem.Settings.SourceResult.IsConfigured)
                draft.Values[FileSystemOperationStepDefinition.SourcePathFieldId] =
                    System.Text.Json.Nodes.JsonValue.Create("legacy-result");
            if (fileSystem.Settings.TargetMode == FileSystemPathSource.TaskResult
                && fileSystem.Settings.TargetResult.IsConfigured)
                draft.Values[FileSystemOperationStepDefinition.TargetPathFieldId] =
                    System.Text.Json.Nodes.JsonValue.Create("legacy-result");
        }
        if (step is ShowTextStep showText
            && showText.Settings.TextSource == ShowTextSource.ExplicitText
            && !string.IsNullOrWhiteSpace(showText.Settings.Text))
            draft.Values[ShowTextStepDefinition.TextResultFieldId] =
                System.Text.Json.JsonSerializer.SerializeToNode(new ResultBinding
                {
                    ProviderId = ValueProviderIds.JobVariable,
                    SourceId = Guid.Empty.ToString("D")
                });
        if (step is DynamicRoiStep dynamicRoi
            && dynamicRoi.Settings.PaddingSource.IsConfigured == false
            && dynamicRoi.Settings.Padding >= 0)
            draft.Values[DynamicRoiStepDefinition.PaddingSourceFieldId] =
                System.Text.Json.JsonSerializer.SerializeToNode(new ResultBinding
                {
                    ProviderId = ValueProviderIds.JobVariable,
                    SourceId = Guid.Empty.ToString("D")
                });
    }

    private static ValueProviderSourceDescriptor? ResolveProviderSource(
        IReadOnlyList<JobVariable> variables,
        IReadOnlyList<ValueProviderSourceDescriptor> providerSources,
        ValueReference reference)
    {
        if (reference.ProviderId is ValueProviderIds.LocalValue or ValueProviderIds.JobVariable
            && Guid.TryParse(reference.SourceId, out var variableId)
            && variables.FirstOrDefault(variable => variable.Id == variableId) is { } variable)
        {
            var root = ValueProviderSourceDescriptor.FromVariable(variable);
            if (string.IsNullOrWhiteSpace(reference.ValuePath)) return root;
            var property = JobVariablePropertyMetadata.GetProperties(variable).FirstOrDefault(candidate =>
                candidate.Name.Equals(reference.ValuePath, StringComparison.OrdinalIgnoreCase));
            return property is null ? null : new ValueProviderSourceDescriptor(
                root.ProviderId,
                root.SourceId,
                $"{root.Name} › {property.DisplayName}",
                property.Description ?? root.Description,
                property.DataType,
                property.Cardinality);
        }

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
