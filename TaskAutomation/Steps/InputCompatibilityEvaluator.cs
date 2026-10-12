using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

/// <summary>The only source policy evaluator. It reads metadata, never source values.</summary>
public static class InputCompatibilityEvaluator
{
    public static bool AllowsProvider(this InputContractNode contract, string providerId,
        InputCompatibilityProfile profile = InputCompatibilityProfile.NewSelection) =>
        IsCurrentProvider(contract, providerId)
        || profile == InputCompatibilityProfile.ExistingBinding && contract.LegacyAllowedProviderIds.Contains(providerId);

    private static bool IsCurrentProvider(InputContractNode contract, string providerId) =>
        providerId == ValueProviderIds.LocalValue ? contract.AllowsDirectValue
        : contract.AllowedProviderIds is null || contract.AllowedProviderIds.Contains(providerId);

    public static InputCompatibilityResult EvaluateSource(this InputContractNode contract, string providerId,
        ResultPropertyDescriptor source, InputCompatibilityProfile profile, bool isLegacyDirectValue = false) =>
        Evaluate(contract, providerId, source, profile, isLegacyDirectValue);

    public static bool IsShapeCompatible(this InputContractNode contract, ResultValueKind kind,
        ResultCardinality cardinality, InputCompatibilityProfile profile) => EvaluateShape(contract, kind, cardinality, profile).IsAccepted;
    public static bool IsShapeCompatible(this InputContractNode contract, ResultPropertyDescriptor property,
        InputCompatibilityProfile profile) => EvaluateShape(contract, property.DataType, property.Cardinality, profile).IsAccepted;
    public static bool IsShapeCompatible(this InputContractNode contract, JobVariable variable,
        InputCompatibilityProfile profile) => EvaluateShape(contract, variable.ValueKind, variable.Cardinality, profile).IsAccepted;

    public static ResultPropertyDescriptor? FindPreferredProperty(this InputContractNode contract,
        IEnumerable<ResultPropertyDescriptor> properties)
    {
        var sources = properties.ToArray();
        foreach (var shape in contract.AcceptedShapes)
        {
            var match = sources.FirstOrDefault(source => shape.Accepts(source)
                && Evaluate(contract, ValueProviderIds.StepResult, source, InputCompatibilityProfile.NewSelection).IsAccepted);
            if (match is not null) return match;
        }
        return null;
    }

    public static InputCompatibilityResult Evaluate(InputContractNode contract, string providerId,
        ResultPropertyDescriptor source, InputCompatibilityProfile profile = InputCompatibilityProfile.NewSelection,
        bool isLegacyDirectValue = false)
    {
        var currentProvider = IsCurrentProvider(contract, providerId);
        var legacyProvider = profile == InputCompatibilityProfile.ExistingBinding
            && (contract.LegacyAllowedProviderIds.Contains(providerId) || isLegacyDirectValue && contract.AllowsDirectValue);
        if (!currentProvider && !legacyProvider) return Reject("InputContract.ProviderNotAllowed");
        var result = EvaluateShape(contract, source.DataType, source.Cardinality, profile);
        if (!result.IsAccepted) return result;
        if (source.DataType == ResultValueKind.Enum && contract.TargetKind == ResultValueKind.Enum
            && (contract.EnumTypeName is not null || contract.EnumPropertyId is not null)
            && !(contract.EnumTypeName is not null
                ? contract.EnumTypeName == source.EnumTypeName
                : string.IsNullOrWhiteSpace(source.EnumTypeName) && contract.EnumPropertyId == source.StableId))
            return Reject("InputContract.EnumIdentityMismatch");
        return !currentProvider ? result with { Status = InputCompatibilityStatus.AcceptedLegacy } : result;
    }

    public static InputCompatibilityResult EvaluateShape(InputContractNode contract, ResultValueKind kind,
        ResultCardinality cardinality, InputCompatibilityProfile profile = InputCompatibilityProfile.NewSelection)
    {
        var current = contract.AcceptedShapes.Any(shape => shape.Accepts(kind, cardinality));
        var legacy = profile == InputCompatibilityProfile.ExistingBinding
            && contract.LegacyAcceptedShapes.Any(shape => shape.Accepts(kind, cardinality));
        if (!current && !legacy) return Reject("InputContract.ShapeNotAllowed");
        var conversion = contract.Conversion;
        if (!current && kind == ResultValueKind.Text)
            conversion = contract.TargetKind switch
            {
                ResultValueKind.Color => InputConversion.LegacyColor,
                ResultValueKind.FilePath => InputConversion.LegacyFilePath,
                _ => InputConversion.Exact
            };
        var status = !current ? InputCompatibilityStatus.AcceptedLegacy
            : conversion != InputConversion.Exact ? InputCompatibilityStatus.AcceptedConverted
            : InputCompatibilityStatus.AcceptedExact;
        return new(status, new(conversion, contract.CollectionConsumption));
    }

    private static InputCompatibilityResult Reject(string code) => new(InputCompatibilityStatus.Rejected, null, code);
}
