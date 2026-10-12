using System.Globalization;
using TaskAutomation.Steps;
using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Jobs;

public static class ConditionRules
{
    public const int MinimumConditions = 1;
    public static InputContractNode SourceInputContract() => new(
        "condition", null,
        Enum.GetValues<ResultValueKind>().Where(kind => GetOperators(kind).Count > 0)
            .Select(kind => new AcceptedResultShape(kind, ResultCardinality.Single, ResultCardinality.OptionalSingle)).ToArray(),
        allowedProviderIds: new HashSet<string> { ValueProviderIds.JobVariable, ValueProviderIds.StepResult },
        required: true, missingValuePolicy: MissingValuePolicy.FailStep);
    public static bool IsConditionSource(ResultPropertyDescriptor property) =>
        InputCompatibilityEvaluator.EvaluateShape(SourceInputContract(), property.DataType, property.Cardinality).IsAccepted;
    public static bool CanCompare(ResultPropertyDescriptor left, ResultPropertyDescriptor right) =>
        InputCompatibilityEvaluator.Evaluate(ComparisonContract(left), ValueProviderIds.StepResult, right).IsAccepted;

    public static InputContractNode ComparisonContract(ResultPropertyDescriptor left, bool allowTextConversion = true) => new(left.StableId, left.DataType,
        InputContractRegistry.AcceptedSourceShapes(left.DataType, left.Cardinality == ResultCardinality.Collection
            ? ResultCardinality.Collection : ResultCardinality.Single, allowTextConversion),
        conversion: left.DataType == ResultValueKind.Text && allowTextConversion ? InputConversion.DisplayText : InputConversion.Exact,
        enumTypeName: string.IsNullOrWhiteSpace(left.EnumTypeName) ? null : left.EnumTypeName,
        enumPropertyId: left.DataType == ResultValueKind.Enum ? left.StableId : null);
    public static InputContractNode ComparisonInputContract(
        InputContractNode contract, ResultPropertyDescriptor property) =>
        property.DataType == ResultValueKind.Enum
            ? contract with
            {
                LegacyAcceptedShapes = [new AcceptedResultShape(ResultValueKind.Text, ResultCardinality.Single)],
                EnumTypeName = property.EnumTypeName,
                EnumPropertyId = property.StableId
            }
            : contract;

    public static InputContractNode ComparisonInputContract(string key, StepValueKind kind,
        ResultPropertyDescriptor? property = null)
    {
        var options = kind == StepValueKind.Enum && property?.EnumValues is { } values
            ? values.Select(value => new StepFieldOptionDescriptor(value, string.Empty)).ToArray() : null;
        var field = new StepFieldDescriptor(key, string.Empty, kind,
            Constraints: options is { Length: > 0 }
                ? new StepFieldConstraints(AllowedValues: options.Select(option => option.Value).ToArray()) : null,
            Options: options);
        var contract = InputContractRegistry.ForField(field);
        return property is null ? contract : ComparisonInputContract(contract, property);
    }

    private static readonly ConditionOperator[] BoolOperators =
        [ConditionOperator.Equals, ConditionOperator.NotEquals];

    private static readonly ConditionOperator[] StringOperators =
    [
        ConditionOperator.Equals, ConditionOperator.NotEquals, ConditionOperator.Contains,
        ConditionOperator.StartsWith, ConditionOperator.IsEmpty, ConditionOperator.IsNotEmpty
    ];

    private static readonly ConditionOperator[] OrderedOperators =
    [
        ConditionOperator.Equals, ConditionOperator.NotEquals, ConditionOperator.GreaterThan,
        ConditionOperator.LessThan, ConditionOperator.GreaterThanOrEqual, ConditionOperator.LessThanOrEqual
    ];

    public static IReadOnlyList<ConditionOperator> GetOperators(ResultValueKind dataType) => dataType switch
    {
        ResultValueKind.Boolean => BoolOperators,
        ResultValueKind.Enum => BoolOperators,
        ResultValueKind.Text or ResultValueKind.Color or ResultValueKind.FilePath => StringOperators,
        ResultValueKind.Integer or ResultValueKind.Number or ResultValueKind.DateTime => OrderedOperators,
        _ => []
    };

    public static bool IsOperatorAllowed(ResultValueKind dataType, ConditionOperator conditionOperator) =>
        GetOperators(dataType).Contains(conditionOperator)
        || dataType == ResultValueKind.Boolean
            && conditionOperator is ConditionOperator.IsTrue or ConditionOperator.IsFalse
        || dataType is ResultValueKind.Text or ResultValueKind.Color or ResultValueKind.FilePath
            && conditionOperator is ConditionOperator.IsEmpty or ConditionOperator.IsNotEmpty;

    public static bool RequiresComparisonValue(ConditionOperator conditionOperator) => conditionOperator is not
        (ConditionOperator.IsTrue or ConditionOperator.IsFalse or ConditionOperator.IsEmpty or ConditionOperator.IsNotEmpty);

    public static bool IsComparisonValueValid(ResultPropertyDescriptor property, ConditionOperator conditionOperator, string? value)
    {
        if (!RequiresComparisonValue(conditionOperator)) return true;
        if (property.DataType == ResultValueKind.Text) return value is not null;
        return StepResultMetadata.TryParseComparison(property, value, out _);
    }

    public static bool AreComparisonSourcesCompatible(
        ResultPropertyDescriptor property,
        ResultPropertyDescriptor comparisonProperty,
        bool isDirectLocalValue,
        string? comparisonValue)
    {
        if (CanCompare(property, comparisonProperty))
            return property.DataType != ResultValueKind.Enum || !isDirectLocalValue
                   || IsComparisonValueValid(property, ConditionOperator.Equals, comparisonValue);
        if (property.DataType != ResultValueKind.Enum
            || comparisonProperty.DataType is not (ResultValueKind.Text or ResultValueKind.Enum)
            || !isDirectLocalValue)
            return false;

        var isLegacyUserChoiceEnum = comparisonProperty.DataType == ResultValueKind.Enum
                                     && UserChoiceEnumContract.IsStepSpecific(property.EnumTypeName)
                                     && string.Equals(
                                         comparisonProperty.EnumTypeName,
                                         UserChoiceEnumContract.LegacyTypeName,
                                         StringComparison.Ordinal);
        if (comparisonProperty.DataType == ResultValueKind.Enum
            && !string.IsNullOrWhiteSpace(comparisonProperty.EnumTypeName)
            && !isLegacyUserChoiceEnum)
            return false;

        return IsComparisonValueValid(property, ConditionOperator.Equals, comparisonValue);
    }

    public static string? FormatComparisonValue(ResultPropertyDescriptor property, object? value)
    {
        if (value is null) return null;
        return property.DataType switch
        {
            ResultValueKind.Number => Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture),
            ResultValueKind.Integer => Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            ResultValueKind.DateTime => ((DateTime)value).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ResultValueKind.Boolean => Convert.ToBoolean(value, CultureInfo.InvariantCulture).ToString(),
            ResultValueKind.Text => TextValueFormatter.Format(value),
            ResultValueKind.Color or ResultValueKind.FilePath => value.ToString(),
            ResultValueKind.Enum => value.ToString(),
            _ => null
        };
    }
}
