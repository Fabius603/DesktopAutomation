using System.Collections.Frozen;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Contracts.Steps;

public enum InputCompatibilityProfile { NewSelection, ExistingBinding }
public enum InputCompatibilityStatus { AcceptedExact, AcceptedConverted, AcceptedLegacy, Rejected }
public enum InputConversion { Exact, DisplayText, LegacyColor, LegacyFilePath }

public sealed record InputConversionPlan(InputConversion Conversion, CollectionConsumptionMode CollectionConsumption);
public sealed record InputCompatibilityResult(InputCompatibilityStatus Status, InputConversionPlan? Plan,
    string? DiagnosticCode = null)
{
    public bool IsAccepted => Status != InputCompatibilityStatus.Rejected;
}

/// <summary>Immutable semantic contract shared by roots, object members and collection items.</summary>
public sealed record InputContractNode
{
    public InputContractNode(string key, ResultValueKind? targetKind,
        IReadOnlyList<AcceptedResultShape> acceptedShapes,
        IReadOnlySet<string>? allowedProviderIds = null, bool allowsDirectValue = false,
        IReadOnlyList<AcceptedResultShape>? legacyAcceptedShapes = null,
        IReadOnlySet<string>? legacyAllowedProviderIds = null,
        bool required = false, MissingValuePolicy missingValuePolicy = MissingValuePolicy.UseDefault,
        CollectionConsumptionMode collectionConsumption = CollectionConsumptionMode.NotApplicable,
        InputConversion conversion = InputConversion.Exact, string? schemaId = null,
        StepFieldConstraints? constraints = null, string? enumTypeName = null, string? enumPropertyId = null,
        ResultCardinality targetCardinality = ResultCardinality.Single)
    {
        Key = key;
        TargetKind = targetKind;
        AcceptedShapes = Freeze(acceptedShapes);
        AllowedProviderIds = allowedProviderIds?.ToFrozenSet(StringComparer.Ordinal);
        AllowsDirectValue = allowsDirectValue;
        LegacyAcceptedShapes = Freeze(legacyAcceptedShapes ?? []);
        LegacyAllowedProviderIds = legacyAllowedProviderIds?.ToFrozenSet(StringComparer.Ordinal)
            ?? FrozenSet<string>.Empty;
        Required = required;
        MissingValuePolicy = missingValuePolicy;
        CollectionConsumption = collectionConsumption;
        Conversion = conversion;
        SchemaId = schemaId;
        Constraints = constraints is null ? null : constraints with
        { AllowedValues = constraints.AllowedValues is null ? null : Array.AsReadOnly(constraints.AllowedValues.ToArray()) };
        EnumTypeName = enumTypeName;
        EnumPropertyId = enumPropertyId;
        TargetCardinality = targetCardinality;
    }

    public string Key { get; init; }
    public ResultValueKind? TargetKind { get; init; }
    public ResultCardinality TargetCardinality { get; init; }
    private readonly IReadOnlyList<AcceptedResultShape> _acceptedShapes = [];
    public IReadOnlyList<AcceptedResultShape> AcceptedShapes { get => _acceptedShapes; init => _acceptedShapes = Freeze(value); }
    private readonly IReadOnlySet<string>? _allowedProviderIds;
    [System.Text.Json.Serialization.JsonConverter(typeof(InputProviderSetConverter))]
    public IReadOnlySet<string>? AllowedProviderIds { get => _allowedProviderIds; init => _allowedProviderIds = value?.ToFrozenSet(StringComparer.Ordinal); }
    public bool AllowsDirectValue { get; init; }
    private readonly IReadOnlyList<AcceptedResultShape> _legacyAcceptedShapes = [];
    public IReadOnlyList<AcceptedResultShape> LegacyAcceptedShapes { get => _legacyAcceptedShapes; init => _legacyAcceptedShapes = Freeze(value); }
    private readonly IReadOnlySet<string> _legacyAllowedProviderIds = FrozenSet<string>.Empty;
    [System.Text.Json.Serialization.JsonConverter(typeof(InputProviderSetConverter))]
    public IReadOnlySet<string> LegacyAllowedProviderIds { get => _legacyAllowedProviderIds; init => _legacyAllowedProviderIds = value.ToFrozenSet(StringComparer.Ordinal); }
    public bool Required { get; init; }
    public MissingValuePolicy MissingValuePolicy { get; init; }
    public CollectionConsumptionMode CollectionConsumption { get; init; }
    public InputConversion Conversion { get; init; }
    public string? SchemaId { get; init; }
    private readonly StepFieldConstraints? _constraints;
    public StepFieldConstraints? Constraints
    {
        get => _constraints;
        init => _constraints = value is null ? null : value with
        { AllowedValues = value.AllowedValues is null ? null : Array.AsReadOnly(value.AllowedValues.ToArray()) };
    }
    public string? EnumTypeName { get; init; }
    public string? EnumPropertyId { get; init; }

    // Copy each shape as well as the collection: callers cannot mutate a registered policy.
    private static IReadOnlyList<AcceptedResultShape> Freeze(IEnumerable<AcceptedResultShape> shapes) =>
        Array.AsReadOnly(shapes.Select(shape => new AcceptedResultShape(shape.ValueKind,
            shape.Cardinalities.ToArray())).ToArray());
}
