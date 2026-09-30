using System.Globalization;
using TaskAutomation.Steps;

namespace TaskAutomation.Jobs;

public static class ConditionRules
{
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
        if (StepResultMetadata.AreComparable(property, comparisonProperty)) return true;
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
            ResultValueKind.Text or ResultValueKind.Color or ResultValueKind.FilePath => value.ToString(),
            ResultValueKind.Enum => value.ToString(),
            _ => null
        };
    }
}
