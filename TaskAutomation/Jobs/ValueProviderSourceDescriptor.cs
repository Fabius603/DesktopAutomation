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
    bool IsSensitive = false,
    string? EnumTypeName = null,
    IReadOnlyList<string>? EnumValues = null,
    IReadOnlyDictionary<string, string>? EnumDisplayNames = null)
{
    public ResultPropertyDescriptor ToResultProperty() => new(
        Name,
        Name,
        ValueKind,
        Description,
        Cardinality: Cardinality,
        EnumTypeName: EnumTypeName,
        EnumValues: EnumValues,
        EnumDisplayNames: EnumDisplayNames,
        Id: SourceId);

    public static ValueProviderSourceDescriptor FromVariable(JobVariable variable)
        => new(
            variable is LocalValue ? ValueProviderIds.LocalValue : ValueProviderIds.JobVariable,
            variable.Id.ToString("D"),
            variable.Name,
            variable.Description,
            variable.ValueKind,
            variable.Cardinality,
            EnumTypeName: variable.EnumTypeName,
            EnumValues: variable.EnumValues,
            EnumDisplayNames: variable.EnumDisplayNames);
}
