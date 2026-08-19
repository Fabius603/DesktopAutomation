using TaskAutomation.Steps;

namespace TaskAutomation.Jobs;

/// <summary>Metadata for one selectable value exposed by a provider.</summary>
public sealed record ValueProviderSourceDescriptor(
    string ProviderId,
    string SourceId,
    string Name,
    string Description,
    ResultValueKind ValueKind,
    ResultCardinality Cardinality,
    bool IsSensitive = false)
{
    public ResultPropertyDescriptor ToResultProperty() => new(
        Name,
        Name,
        ValueKind,
        Description,
        Cardinality: Cardinality,
        Id: SourceId);

    public static ValueProviderSourceDescriptor FromVariable(JobVariable variable) => new(
        ValueProviderIds.JobVariable,
        variable.Id.ToString("D"),
        variable.Name,
        variable.Description,
        variable.ValueKind,
        variable.Cardinality);
}
