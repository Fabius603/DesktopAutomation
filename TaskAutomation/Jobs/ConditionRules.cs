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
        ResultValueKind.Text => StringOperators,
        ResultValueKind.Integer or ResultValueKind.Number or ResultValueKind.DateTime => OrderedOperators,
        _ => []
    };

    public static bool IsOperatorAllowed(ResultValueKind dataType, ConditionOperator conditionOperator) =>
        GetOperators(dataType).Contains(conditionOperator)
        || dataType == ResultValueKind.Boolean
            && conditionOperator is ConditionOperator.IsTrue or ConditionOperator.IsFalse
        || dataType == ResultValueKind.Text
            && conditionOperator is ConditionOperator.IsEmpty or ConditionOperator.IsNotEmpty;

    public static bool RequiresComparisonValue(ConditionOperator conditionOperator) => conditionOperator is not
        (ConditionOperator.IsTrue or ConditionOperator.IsFalse or ConditionOperator.IsEmpty or ConditionOperator.IsNotEmpty);

    public static bool IsComparisonValueValid(ResultPropertyDescriptor property, ConditionOperator conditionOperator, string? value)
    {
        if (!RequiresComparisonValue(conditionOperator)) return true;
        if (property.DataType == ResultValueKind.Text) return value is not null;
        return StepResultMetadata.TryParseComparison(property, value, out _);
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
            ResultValueKind.Text => value.ToString(),
            ResultValueKind.Enum => value.ToString(),
            _ => null
        };
    }
}
