using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Jobs;

/// <summary>Converts legacy literal step settings into typed job variables without losing old files.</summary>
public static class JobVariableInputMigration
{
    public static bool Migrate(Job job, IStepDefinitionCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(job);
        catalog ??= BuiltInStepDefinitions.Instance;
        job.Variables ??= [];
        var changed = false;
        foreach (var step in job.EnumerateAllSteps())
        {
            step.Inputs ??= new Dictionary<string, ResultBinding>(StringComparer.Ordinal);
            if (!catalog.TryGetByType(step.GetType(), out var definition)) continue;
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
                    var contract = !string.IsNullOrWhiteSpace(field.InputContractId)
                        ? StepInputContractRegistry.Get(step.GetType(), field.InputContractId)
                        : null;
                    var shape = contract?.AcceptedShapes.FirstOrDefault();
                    var placeholder = new JobVariable
                    {
                        Name = UniqueName(job.Variables, $"{definition.Descriptor.TypeId}_{field.Id}"),
                        Description = $"{definition.Descriptor.TypeId}.{field.Id}",
                        ValueKind = shape?.ValueKind ?? ResultValueKind.ResultObject,
                        Cardinality = shape?.Cardinalities.FirstOrDefault(ResultCardinality.Single)
                                      ?? ResultCardinality.Single,
                        Value = null
                    };
                    job.Variables.Add(placeholder);
                    step.Inputs[field.Id] = new ResultBinding
                    {
                        ProviderId = ValueProviderIds.JobVariable,
                        SourceId = placeholder.Id.ToString("D")
                    };
                    changed = true;
                    continue;
                }
                if (step.Inputs.TryGetValue(field.Id, out var existing) && existing.IsConfigured) continue;
                var value = draft.Values.GetValueOrDefault(field.Id) ?? field.DefaultValue;
                var variable = new JobVariable
                {
                    Name = UniqueName(job.Variables, $"{definition.Descriptor.TypeId}_{field.Id}"),
                    Description = $"{definition.Descriptor.TypeId}.{field.Id}",
                    ValueKind = MapKind(field.ValueKind),
                    Cardinality = field.ValueKind == StepValueKind.Collection
                        ? ResultCardinality.Collection
                        : ResultCardinality.Single,
                    Value = value?.DeepClone()
                };
                job.Variables.Add(variable);
                step.Inputs[field.Id] = new ResultBinding
                {
                    ProviderId = ValueProviderIds.JobVariable,
                    SourceId = variable.Id.ToString("D")
                };
                changed = true;
            }
        }
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
        StepValueKind.Enum => ResultValueKind.Enum,
        StepValueKind.Point => ResultValueKind.Point,
        StepValueKind.Rectangle => ResultValueKind.Rectangle,
        StepValueKind.Object or StepValueKind.Collection => ResultValueKind.ResultObject,
        _ => ResultValueKind.Text
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
