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

    public static StepValidationResult ValidateCandidate(IReadOnlyList<JobStep> precedingSteps, JobStep? candidate, IReadOnlyList<JobStep>? allSteps = null)
    {
        if (candidate == null) return new(null!, false, "Es konnte kein Step erstellt werden.");
        var steps = precedingSteps.Concat([candidate]).ToList();
        return ValidateStep(steps, candidate, allSteps);
    }

    public static bool IsSourceStepAllowed(IReadOnlyList<JobStep> steps, JobStep consumer, JobStep source)
    {
        var consumerIndex = IndexOf(steps, consumer);
        var sourceIndex = IndexOf(steps, source);
        return source.IsEnabled && sourceIndex >= 0 && consumerIndex >= 0 && sourceIndex < consumerIndex;
    }

    public static JobValidationResult ValidateJob(Job job)
    {
        var results = ValidateSection(job.StartSteps, [])
            .Concat(ValidateSection(job.Steps, job.StartSteps))
            .Concat(ValidateSection(job.EndSteps, job.StartSteps.Concat(job.Steps).ToList()))
            .ToList();
        return new JobValidationResult(results.All(r => r.IsValid), results);
    }

    private static IReadOnlyList<StepValidationResult> ValidateSection(
        IReadOnlyList<JobStep> steps,
        IReadOnlyList<JobStep> precedingPhases)
    {
        var executionOrder = precedingPhases.Concat(steps).ToList();
        var results = steps.Select(s => ValidateStep(executionOrder, s)).ToList();
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

    public static StepValidationResult ValidateStep(IReadOnlyList<JobStep> steps, JobStep step, IReadOnlyList<JobStep>? referenceSteps = null)
    {
        if (!step.IsEnabled)
            return new(step, true, null);

        var valueError = ValidateValues(step);
        if (valueError != null)
            return new(step, false, valueError);

        var index = IndexOf(steps, step);
        if (ValidateResultBindings(steps, index, step) is { } bindingError)
            return new(step, false, bindingError);
        if (step is IfStep ifStep && ValidateConditions(steps, index, ifStep.Settings.Conditions) is { } ifError)
            return new(step, false, ifError);
        if (step is ElseIfStep elseIfStep && ValidateConditions(steps, index, elseIfStep.Settings.Conditions) is { } elseIfError)
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
        var hasError = definition.ValidateDraft(definition.CreateDraft(step))
            .Any(issue => issue.Severity == StepValidationSeverity.Error);
        return hasError ? invalid : null;
    }

    private static string? ValidateConditions(IReadOnlyList<JobStep> steps, int conditionStepIndex, IEnumerable<StepCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            var source = steps.Take(conditionStepIndex).FirstOrDefault(s => s.Id == condition.SourceStepId && s.IsEnabled);
            if (source is null) return "Eine Bedingung verweist nicht auf einen gültigen vorherigen Step.";
            if (StepResultMetadata.GetResultTypeForStep(source) is null)
                return "Der ausgewählte Step besitzt keinen auswertbaren Rückgabewert.";
            var property = FindProperty(
                StepResultMetadata.GetResultTypeForStep(source),
                condition.PropertyId,
                condition.PropertyPath);
            if (property is null) return "Die ausgewählte Rückgabeeigenschaft existiert nicht mehr.";
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

            var comparisonSource = steps.Take(conditionStepIndex)
                .FirstOrDefault(s => s.Id == comparison.SourceStepId && s.IsEnabled);
            if (comparisonSource is null)
                return "Der JobResult-Vergleich verweist nicht auf einen gültigen vorherigen Step.";
            if (StepResultMetadata.GetResultTypeForStep(comparisonSource) is null)
                return "Der ausgewählte JobResult-Step besitzt keinen auswertbaren Rückgabewert.";
            var comparisonProperty = FindProperty(
                StepResultMetadata.GetResultTypeForStep(comparisonSource),
                comparison.PropertyId,
                comparison.PropertyPath);
            if ((string.IsNullOrWhiteSpace(comparison.PropertyId)
                 && string.IsNullOrWhiteSpace(comparison.PropertyPath))
                || comparisonProperty is null)
                return "Die ausgewählte JobResult-Vergleichseigenschaft existiert nicht mehr.";
            if (!StepResultMetadata.AreComparable(property, comparisonProperty))
                return "Beide Vergleichswerte müssen denselben Datentyp besitzen.";
        }
        return null;
    }

    private static string? ValidateResultBindings(IReadOnlyList<JobStep> steps, int consumerIndex, JobStep step)
    {
        if (!BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
            return "FÃ¼r den Step fehlt die Backend-Definition.";
        foreach (var configuredInput in definition.GetInputBindings(step))
        {
            var key = configuredInput.ContractId;
            var binding = configuredInput.Binding;
            var contract = StepInputContractRegistry.Get(step.GetType(), key);
            if (contract is null) return $"Für die Eingabe '{key}' fehlt der Backend-Vertrag.";
            if (!binding.IsConfigured)
            {
                if (contract.Required) return $"Für die Eingabe '{key}' wurde keine Ergebnis-Eigenschaft ausgewählt.";
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
