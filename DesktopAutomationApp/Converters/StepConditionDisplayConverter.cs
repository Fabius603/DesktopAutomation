using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Converters
{
    internal static class ConditionDisplayFormatter
    {
        public static string Format(
            StepCondition condition,
            IList? steps,
            IReadOnlyList<JobVariable>? variables = null)
        {
            var stepMap = new Dictionary<string, (string Name, JobStep Step)>(StringComparer.OrdinalIgnoreCase);
            if (steps is not null)
                for (var index = 0; index < steps.Count; index++)
                    if (steps[index] is JobStep step)
                        stepMap[step.Id] = (StepLocalization.NumberedName(step, steps), step);

            var source = FormatReference(condition, stepMap, variables);
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
                ? $"{Loc.Get("Ui.Step.IfEditor.JobResultValue")}: {FormatReference(operand, stepMap, variables)}"
                : $"{Loc.Get("Ui.Step.IfEditor.LiteralValue")}: {FormatLiteral(operand.Value, ResolvePropertyType(condition, stepMap, variables))}";
            return $"{source} {operatorText} {operandText}";
        }

        public static string FormatSummary(
            IfConditionSettings settings,
            IList? steps,
            IReadOnlyList<JobVariable>? variables = null)
        {
            if (settings.Conditions.Count == 0)
                return Loc.Get("Ui.Job.Condition.NoConditions");
            if (settings.Conditions.Count == 1)
                return Format(settings.Conditions[0], steps, variables);

            var mode = settings.MatchMode == ConditionMatchMode.All
                ? Loc.Get("Ui.Job.Condition.AllBadge")
                : Loc.Get("Ui.Job.Condition.AnyBadge");
            var first = Format(settings.Conditions[0], steps, variables);
            return $"{mode} · {Loc.Format("Ui.Job.Condition.Count", settings.Conditions.Count)} · "
                   + $"{first} · {Loc.Format("Ui.Job.Condition.More", settings.Conditions.Count - 1)}";
        }

        private static string FormatReference(
            ResultBinding binding,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap,
            IReadOnlyList<JobVariable>? variables)
        {
            if (string.Equals(binding.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
                && Guid.TryParse(binding.SourceId, out var variableId))
            {
                var variable = variables?.FirstOrDefault(candidate => candidate.Id == variableId);
                var name = variable?.Name ?? Loc.Get("Ui.Job.Steps.SourceUnavailable");
                return $"{Loc.Get("Ui.ValueReference.JobVariables")} → {name}";
            }

            var source = ResolveStep(binding.SourceStepId, stepMap);
            return $"{source} → {ResolvePropertyName(binding, stepMap)}";
        }

        private static string ResolveStep(
            string? stepId,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap) =>
            !string.IsNullOrWhiteSpace(stepId) && stepMap.TryGetValue(stepId, out var entry)
                ? entry.Name
                : string.IsNullOrWhiteSpace(stepId) ? Loc.Get("Step.Unknown") : stepId;

        private static ResultValueKind? ResolvePropertyType(
            ResultBinding binding,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap,
            IReadOnlyList<JobVariable>? variables)
        {
            if (string.Equals(binding.ProviderId, ValueProviderIds.JobVariable, StringComparison.Ordinal)
                && Guid.TryParse(binding.SourceId, out var variableId))
                return variables?.FirstOrDefault(candidate => candidate.Id == variableId)?.ValueKind;

            if (!stepMap.TryGetValue(binding.SourceStepId, out var source)) return null;
            var resultType = StepResultMetadata.GetResultTypeForStep(source.Step);
            return resultType is not null
                   && StepResultMetadata.TryGetProperty(
                       resultType, binding.PropertyId, binding.PropertyPath, out var property)
                ? property.DataType
                : null;
        }

        private static string ResolvePropertyName(
            ResultBinding binding,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap)
        {
            if (stepMap.TryGetValue(binding.SourceStepId, out var source))
            {
                var resultType = StepResultMetadata.GetResultTypeForStep(source.Step);
                if (resultType is not null
                    && StepResultMetadata.TryGetProperty(
                        resultType, binding.PropertyId, binding.PropertyPath, out var descriptor))
                    return StepLocalization.PropertyPath(resultType.TypeName, descriptor.Name);
            }

            var property = string.IsNullOrWhiteSpace(binding.PropertyPath)
                ? binding.PropertyId
                : binding.PropertyPath;
            var localized = StepLocalization.PropertyPath(property ?? string.Empty);
            return string.IsNullOrWhiteSpace(localized) ? Loc.Get("Common.Value") : localized;
        }

        private static string FormatLiteral(string? value, ResultValueKind? propertyType)
        {
            if (value is null) return "?";
            if (propertyType == ResultValueKind.Text)
                return $"\"{value}\"";
            if (propertyType == ResultValueKind.Boolean && bool.TryParse(value, out var boolean))
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
    /// values[1] = Steps collection (IList), used to resolve the current step number
    /// values[2] = StepsVersion (int), cache key for reordering and reference changes
    /// values[3] = Job variables
    /// </summary>
    public sealed class StepConditionDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is null || values.Length < 1 || values[0] is not StepCondition condition)
                return string.Empty;

            return ConditionDisplayFormatter.Format(
                condition,
                values.Length > 1 ? values[1] as IList : null,
                values.Length > 3 ? values[3] as IReadOnlyList<JobVariable> : null);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
