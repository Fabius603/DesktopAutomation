using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.Converters
{
    internal static class ConditionDisplayFormatter
    {
        public static string Format(StepCondition condition, IList? steps)
        {
            var stepMap = new Dictionary<string, (string Name, JobStep Step)>(StringComparer.OrdinalIgnoreCase);
            if (steps is not null)
                for (var index = 0; index < steps.Count; index++)
                    if (steps[index] is JobStep step)
                        stepMap[step.Id] = (StepLocalization.NumberedName(step, steps), step);

            var source = ResolveStep(condition.SourceStepId, stepMap);
            var property = StepLocalization.PropertyPath(condition.PropertyPath);
            if (string.IsNullOrWhiteSpace(property)) property = Loc.Get("Common.Value");

            var conditionOperator = condition.Operator;
            var operand = condition.EffectiveComparison;
            switch (condition.Operator)
            {
                case ConditionOperator.IsTrue:
                    conditionOperator = ConditionOperator.Equals;
                    operand = new ComparisonOperand { Kind = ComparisonOperandKind.Literal, Value = bool.TrueString };
                    break;
                case ConditionOperator.IsFalse:
                    conditionOperator = ConditionOperator.Equals;
                    operand = new ComparisonOperand { Kind = ComparisonOperandKind.Literal, Value = bool.FalseString };
                    break;
                case ConditionOperator.IsEmpty:
                    conditionOperator = ConditionOperator.Equals;
                    operand = new ComparisonOperand { Kind = ComparisonOperandKind.Literal, Value = string.Empty };
                    break;
                case ConditionOperator.IsNotEmpty:
                    conditionOperator = ConditionOperator.NotEquals;
                    operand = new ComparisonOperand { Kind = ComparisonOperandKind.Literal, Value = string.Empty };
                    break;
            }

            var operatorText = OperatorText(conditionOperator);
            var operandText = operand.Kind == ComparisonOperandKind.JobResult
                ? $"JobResult: {ResolveStep(operand.SourceStepId, stepMap)} → {FormatProperty(operand.PropertyPath)}"
                : $"Festwert: {FormatLiteral(operand.Value, ResolvePropertyType(condition, stepMap))}";
            return $"{source} → {property} {operatorText} {operandText}";
        }

        private static string ResolveStep(
            string? stepId,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap) =>
            !string.IsNullOrWhiteSpace(stepId) && stepMap.TryGetValue(stepId, out var entry)
                ? entry.Name
                : string.IsNullOrWhiteSpace(stepId) ? Loc.Get("Step.Unknown") : stepId;

        private static string FormatProperty(string? propertyPath)
        {
            var property = StepLocalization.PropertyPath(propertyPath ?? string.Empty);
            return string.IsNullOrWhiteSpace(property) ? Loc.Get("Common.Value") : property;
        }

        private static ResultPropertyType? ResolvePropertyType(
            StepCondition condition,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap)
        {
            if (!stepMap.TryGetValue(condition.SourceStepId, out var source)) return null;
            var output = StepPipelineRegistry.Get(source.Step.GetType())?.Output;
            return output is not null && StepResultMetadata.TryGetProperty(output, condition.PropertyPath, out var property)
                ? property.PropertyType
                : null;
        }

        private static string FormatLiteral(string? value, ResultPropertyType? propertyType)
        {
            if (value is null) return "?";
            if (propertyType == ResultPropertyType.String)
                return $"\"{value}\"";
            if (propertyType == ResultPropertyType.Bool && bool.TryParse(value, out var boolean))
                return boolean ? "true" : "false";
            return value;
        }

        private static string OperatorText(ConditionOperator conditionOperator) => conditionOperator switch
        {
            ConditionOperator.Equals => "=",
            ConditionOperator.NotEquals => "!=",
            ConditionOperator.GreaterThan => ">",
            ConditionOperator.LessThan => "<",
            ConditionOperator.GreaterThanOrEqual => ">=",
            ConditionOperator.LessThanOrEqual => "<=",
            ConditionOperator.Contains => Loc.Get("Condition.Contains"),
            ConditionOperator.StartsWith => Loc.Get("Condition.StartsWith"),
            _ => conditionOperator.ToString()
        };
    }

    /// <summary>
    /// Builds a readable single-line text for If/ElseIf conditions in step list previews.
    /// values[0] = StepCondition
    /// values[1] = Steps-Collection (IList) — used to resolve current step number dynamically
    /// values[2] = StepsVersion (int) — cache-key so the name updates on every reorder
    /// </summary>
    public sealed class StepConditionDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is null || values.Length < 1 || values[0] is not StepCondition c)
                return string.Empty;

            return ConditionDisplayFormatter.Format(c, values.Length > 1 ? values[1] as IList : null);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
