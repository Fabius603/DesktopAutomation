using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Jobs;

/// <summary>Converts legacy literal step settings into private, typed local values without losing old files.</summary>
public static class JobVariableInputMigration
{
    public static bool Migrate(Job job, IStepDefinitionCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(job);
        catalog ??= BuiltInStepDefinitions.Instance;
        job.Variables ??= [];
        job.LocalValues ??= [];
        var changed = MoveLegacyStepValues(job);
        foreach (var step in job.EnumerateAllSteps())
        {
            step.Inputs ??= new Dictionary<string, ResultBinding>(StringComparer.Ordinal);
            if (!catalog.TryGetByType(step.GetType(), out var definition)) continue;
            changed |= MigrateLegacyAliases(step);
            var draft = definition.CreateDraft(step);
            foreach (var field in definition.Descriptor.Fields)
            {
                if (field.ValueKind == StepValueKind.ResultBinding)
                {
                    var binding = ReadBinding(draft.Values.GetValueOrDefault(field.Id));
                    if (binding.IsConfigured && !step.Inputs.ContainsKey(field.Id))
                    {
                        step.Inputs[field.Id] = binding;
                        changed = true;
                    }
                    if (binding.IsConfigured || step.Inputs.ContainsKey(field.Id)) continue;
                    var contract = StepInputContractRegistry.Resolve(step.GetType(), field);
                    var shape = contract?.AcceptedShapes.FirstOrDefault();
                    if (contract?.AllowsDirectValue != true) continue;
                    var placeholder = new LocalValue
                    {
                        Name = UniqueName(job.LocalValues, $"{definition.Descriptor.TypeId}_{field.Id}"),
                        Description = $"{definition.Descriptor.TypeId}.{field.Id}",
                        Scope = JobVariableScope.StepValue,
                        OwnerStepId = step.Id,
                        InputPath = field.Id,
                        ValueKind = shape?.ValueKind ?? ResultValueKind.ResultObject,
                        Cardinality = shape?.Cardinalities.FirstOrDefault(ResultCardinality.Single)
                                      ?? ResultCardinality.Single,
                        Value = LegacyDirectValue(step, field)?.DeepClone()
                                ?? field.DefaultValue?.DeepClone()
                    };
                    job.LocalValues.Add(placeholder);
                    step.Inputs[field.Id] = new ResultBinding
                    {
                        ProviderId = ValueProviderIds.LocalValue,
                        SourceId = placeholder.Id.ToString("D")
                    };
                    changed = true;
                    continue;
                }
                if (step.Inputs.TryGetValue(field.Id, out var existing) && existing.IsConfigured)
                {
                    changed |= EnrichEnumMetadata(job, existing, definition.Descriptor.TypeId, field);
                    continue;
                }
                var value = draft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue;
                var variable = new LocalValue
                {
                    Name = UniqueName(job.LocalValues, $"{definition.Descriptor.TypeId}_{field.Id}"),
                    Description = $"{definition.Descriptor.TypeId}.{field.Id}",
                    Scope = JobVariableScope.StepValue,
                    OwnerStepId = step.Id,
                    InputPath = field.Id,
                    ValueKind = MapKind(field),
                    Cardinality = field.ValueKind == StepValueKind.Collection
                        ? ResultCardinality.Collection
                        : ResultCardinality.Single,
                    Value = value?.DeepClone()
                };
                ApplyEnumMetadata(variable, definition.Descriptor.TypeId, field);
                job.LocalValues.Add(variable);
                step.Inputs[field.Id] = new ResultBinding
                {
                    ProviderId = ValueProviderIds.LocalValue,
                    SourceId = variable.Id.ToString("D")
                };
                changed = true;
            }
            changed |= ValueBindingTree.Normalize(step.Inputs, definition.Descriptor.Fields);
        }
        if (job.Variables.RemoveAll(variable => variable.Scope == JobVariableScope.StepValue) > 0)
            changed = true;
        if (job.FormatVersion < Job.CurrentFormatVersion)
        {
            job.FormatVersion = Job.CurrentFormatVersion;
            changed = true;
        }
        return changed;
    }

    public static ResultValueKind MapKind(StepValueKind kind) => kind switch
    {
        StepValueKind.Boolean => ResultValueKind.Boolean,
        StepValueKind.Integer or StepValueKind.Duration => ResultValueKind.Integer,
        StepValueKind.Number => ResultValueKind.Number,
        StepValueKind.DateTime => ResultValueKind.DateTime,
        StepValueKind.Color => ResultValueKind.Color,
        StepValueKind.FilePath => ResultValueKind.FilePath,
        StepValueKind.Enum => ResultValueKind.Enum,
        StepValueKind.Point => ResultValueKind.Point,
        StepValueKind.Rectangle => ResultValueKind.Rectangle,
        StepValueKind.Object or StepValueKind.Collection => ResultValueKind.ResultObject,
        _ => ResultValueKind.Text
    };

    public static ResultValueKind MapKind(StepFieldDescriptor field) => field.EditorHint switch
    {
        StepEditorHints.JobPicker => ResultValueKind.JobReference,
        StepEditorHints.MacroPicker => ResultValueKind.MacroReference,
        _ => MapKind(field.ValueKind)
    };

    private static bool MigrateLegacyAliases(JobStep step)
    {
        var changed = false;
        if (step is FileSystemOperationStep fileSystem)
        {
            if (fileSystem.Settings.SourceMode == FileSystemPathSource.TaskResult
                && fileSystem.Settings.SourceResult.IsConfigured
                && !step.Inputs.ContainsKey(FileSystemOperationStepDefinition.SourcePathFieldId))
            {
                step.Inputs[FileSystemOperationStepDefinition.SourcePathFieldId] = fileSystem.Settings.SourceResult;
                changed = true;
            }
            if (fileSystem.Settings.TargetMode == FileSystemPathSource.TaskResult
                && fileSystem.Settings.TargetResult.IsConfigured
                && !step.Inputs.ContainsKey(FileSystemOperationStepDefinition.TargetPathFieldId))
            {
                step.Inputs[FileSystemOperationStepDefinition.TargetPathFieldId] = fileSystem.Settings.TargetResult;
                changed = true;
            }
        }
        return changed;
    }

    private static bool MoveLegacyStepValues(Job job)
    {
        var changed = false;
        foreach (var usage in ValueReferenceUsageInspector.Find(job))
        {
            var reference = usage.Reference;
            if (!string.Equals(reference.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
                || !Guid.TryParse(reference.SourceId, out var variableId))
                continue;
            var variable = job.Variables.FirstOrDefault(candidate =>
                candidate.Id == variableId && candidate.Scope == JobVariableScope.StepValue);
            if (variable is null) continue;
            var local = new LocalValue
            {
                Id = job.LocalValues.Any(candidate => candidate.Id == variable.Id)
                    ? Guid.NewGuid()
                    : variable.Id,
                Name = variable.Name,
                Description = variable.Description,
                Scope = JobVariableScope.StepValue,
                ValueKind = variable.ValueKind,
                Cardinality = variable.Cardinality,
                Value = variable.Value?.DeepClone(),
                EnumTypeName = variable.EnumTypeName,
                EnumValues = variable.EnumValues?.ToList(),
                EnumDisplayNames = variable.EnumDisplayNames is null
                    ? null
                    : new Dictionary<string, string>(variable.EnumDisplayNames, StringComparer.Ordinal),
                OwnerStepId = usage.Step.Id,
                InputPath = ResolveInputPath(usage)
            };
            job.LocalValues.Add(local);
            reference.ProviderId = ValueProviderIds.LocalValue;
            reference.SourceId = local.Id.ToString("D");
            usage.UpdateReference(_ => { });
            changed = true;
        }
        return changed;
    }

    private static bool EnrichEnumMetadata(
        Job job,
        ResultBinding binding,
        string stepTypeId,
        StepFieldDescriptor field)
    {
        if (field.ValueKind != StepValueKind.Enum
            || binding.ProviderId is not (ValueProviderIds.LocalValue or ValueProviderIds.JobVariable)
            || !Guid.TryParse(binding.SourceId, out var variableId))
            return false;
        var variable = JobValueSources.Find(job.LocalValues.Cast<JobVariable>().Concat(job.Variables), binding);
        if (variable is null) return false;
        var previous = System.Text.Json.JsonSerializer.Serialize(variable);
        ApplyEnumMetadata(variable, stepTypeId, field);
        return !string.Equals(previous, System.Text.Json.JsonSerializer.Serialize(variable), StringComparison.Ordinal);
    }

    private static void ApplyEnumMetadata(JobVariable variable, string stepTypeId, StepFieldDescriptor field) =>
        StepEnumRules.ApplyMetadata(variable, $"{stepTypeId}.{field.Id}", field);
    private static string ResolveInputPath(ValueReferenceUsage usage)
    {
        var input = usage.Step.Inputs.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Value, usage.Reference));
        return string.IsNullOrEmpty(input.Key) ? usage.Path : input.Key;
    }

    private static JsonNode? LegacyDirectValue(JobStep step, StepFieldDescriptor field) => step switch
    {
        DynamicRoiStep dynamicRoi when field.Id == DynamicRoiStepDefinition.PaddingSourceFieldId
            && dynamicRoi.Settings.Padding >= 0 => JsonValue.Create(dynamicRoi.Settings.Padding),
        ShowTextStep showText when field.Id == ShowTextStepDefinition.TextResultFieldId
            && showText.Settings.TextSource == ShowTextSource.ExplicitText => JsonValue.Create(showText.Settings.Text),
        _ => null
    };

    private static ResultBinding ReadBinding(JsonNode? value)
    {
        try { return value?.Deserialize<ResultBinding>() ?? new ResultBinding(); }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or InvalidOperationException)
        { return new ResultBinding(); }
    }

    private static string UniqueName(IEnumerable<JobVariable> variables, string requested)
    {
        var stem = requested.Trim();
        var names = variables.Select(variable => variable.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(stem)) return stem;
        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{stem}_{suffix}";
            if (!names.Contains(candidate)) return candidate;
        }
    }
}
