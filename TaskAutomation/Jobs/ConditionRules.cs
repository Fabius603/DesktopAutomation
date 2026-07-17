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
        ConditionOperator.StartsWith
    ];

    private static readonly ConditionOperator[] OrderedOperators =
    [
        ConditionOperator.Equals, ConditionOperator.NotEquals, ConditionOperator.GreaterThan,
        ConditionOperator.LessThan, ConditionOperator.GreaterThanOrEqual, ConditionOperator.LessThanOrEqual
    ];

    public static IReadOnlyList<ConditionOperator> GetOperators(ResultPropertyType propertyType) => propertyType switch
    {
        ResultPropertyType.Bool => BoolOperators,
        ResultPropertyType.String => StringOperators,
        _ => OrderedOperators
    };

    public static bool IsOperatorAllowed(ResultPropertyType propertyType, ConditionOperator conditionOperator) =>
        GetOperators(propertyType).Contains(conditionOperator)
        || propertyType == ResultPropertyType.Bool
            && conditionOperator is ConditionOperator.IsTrue or ConditionOperator.IsFalse
        || propertyType == ResultPropertyType.String
            && conditionOperator is ConditionOperator.IsEmpty or ConditionOperator.IsNotEmpty;

    public static bool RequiresComparisonValue(ConditionOperator conditionOperator) => conditionOperator is not
        (ConditionOperator.IsTrue or ConditionOperator.IsFalse or ConditionOperator.IsEmpty or ConditionOperator.IsNotEmpty);

    public static bool IsComparisonValueValid(ResultPropertyDescriptor property, ConditionOperator conditionOperator, string? value)
    {
        if (!RequiresComparisonValue(conditionOperator)) return true;
        if (property.PropertyType == ResultPropertyType.String) return value is not null;
        return StepResultMetadata.TryParseComparison(property, value, out _);
    }

    public static string? FormatComparisonValue(ResultPropertyDescriptor property, object? value)
    {
        if (value is null) return null;
        return property.PropertyType switch
        {
            ResultPropertyType.Double => Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture),
            ResultPropertyType.Integer => Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            ResultPropertyType.DateTime => ((DateTime)value).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ResultPropertyType.Bool => Convert.ToBoolean(value, CultureInfo.InvariantCulture).ToString(),
            ResultPropertyType.String => value.ToString(),
            _ => null
        };
    }
}
