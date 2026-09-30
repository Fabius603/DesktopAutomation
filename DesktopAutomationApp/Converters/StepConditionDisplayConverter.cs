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
            var sourceProperty = ResolveProperty(condition, stepMap, variables);
            var operandText = operand.Kind == ComparisonOperandKind.JobResult
                ? TryFormatStoredLiteral(operand, variables, out var storedLiteral)
                    ? $"{Loc.Get("Ui.Step.IfEditor.LiteralValue")}: {FormatLiteral(storedLiteral, sourceProperty)}"
                    : $"{Loc.Get("Ui.Step.IfEditor.JobResultValue")}: {FormatReference(operand, stepMap, variables)}"
                : $"{Loc.Get("Ui.Step.IfEditor.LiteralValue")}: {FormatLiteral(operand.Value, sourceProperty)}";
            return $"{source} {operatorText} {operandText}";
        }

        public static string FormatSummary(
            IfConditionSettings settings,
            IList? steps,
            IReadOnlyList<JobVariable>? variables = null)
        {
            var conditions = settings?.Conditions?.Where(condition => condition is not null).ToList() ?? [];
            if (conditions.Count == 0)
                return Loc.Get("Ui.Job.Condition.NoConditions");
            if (conditions.Count == 1)
                return Format(conditions[0], steps, variables);

            var mode = settings.MatchMode == ConditionMatchMode.All
                ? Loc.Get("Ui.Job.Condition.AllBadge")
                : Loc.Get("Ui.Job.Condition.AnyBadge");
            var first = Format(conditions[0], steps, variables);
            return $"{mode} · {Loc.Format("Ui.Job.Condition.Count", conditions.Count)} · "
                   + $"{first} · {Loc.Format("Ui.Job.Condition.More", conditions.Count - 1)}";
        }

        private static string FormatReference(
            ResultBinding binding,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap,
            IReadOnlyList<JobVariable>? variables)
        {
            if (binding.ProviderId is ValueProviderIds.JobVariable or ValueProviderIds.LocalValue
                && Guid.TryParse(binding.SourceId, out var variableId))
            {
                var variable = variables?.FirstOrDefault(candidate => candidate.Id == variableId);
                var name = variable?.Name ?? Loc.Get("Ui.Job.Steps.SourceUnavailable");
                var sourceName = string.Equals(binding.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal)
                    ? Loc.Get("Ui.Step.IfEditor.LiteralValue")
                    : Loc.Get("Ui.ValueReference.JobVariables");
                return $"{sourceName} → {name}";
            }

            if (binding.HasProviderReference
                && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
                return $"{binding.ProviderId} → {binding.SourceId}";

            var source = ResolveStep(binding.SourceStepId, stepMap);
            return $"{source} → {ResolvePropertyName(binding, stepMap)}";
        }

        private static string ResolveStep(
            string? stepId,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap) =>
            !string.IsNullOrWhiteSpace(stepId) && stepMap.TryGetValue(stepId, out var entry)
                ? entry.Name
                : string.IsNullOrWhiteSpace(stepId) ? Loc.Get("Step.Unknown") : stepId;

        private static ResultPropertyDescriptor? ResolveProperty(
            ResultBinding binding,
            IReadOnlyDictionary<string, (string Name, JobStep Step)> stepMap,
            IReadOnlyList<JobVariable>? variables)
        {
            if (binding.ProviderId is ValueProviderIds.JobVariable or ValueProviderIds.LocalValue
                && Guid.TryParse(binding.SourceId, out var variableId)
                && variables?.FirstOrDefault(candidate => candidate.Id == variableId) is { } variable)
                return new ResultPropertyDescriptor(variable.Name, variable.Name, variable.ValueKind);

            if (!stepMap.TryGetValue(binding.SourceStepId, out var source)) return null;
            var resultType = StepResultMetadata.GetResultTypeForStep(source.Step);
            return resultType is not null
                   && StepResultMetadata.TryGetProperty(
                       resultType, binding.PropertyId, binding.PropertyPath, out var property)
                ? property
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

        private static string FormatLiteral(string? value, ResultPropertyDescriptor? property)
        {
            if (value is null) return "?";
            if (property?.DataType == ResultValueKind.Text)
                return $"\"{value}\"";
            if (property?.DataType == ResultValueKind.Boolean && bool.TryParse(value, out var boolean))
                return boolean ? "true" : "false";
            if (property?.DataType == ResultValueKind.Enum)
                return EnumValueLocalization.ForResultValue(property, value);
            return value;
        }

        private static bool TryFormatStoredLiteral(
            ResultBinding binding,
            IReadOnlyList<JobVariable>? variables,
            out string value)
        {
            value = string.Empty;
            if (!string.Equals(binding.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal)
                || !Guid.TryParse(binding.SourceId, out var variableId)
                || variables?.FirstOrDefault(candidate => candidate.Id == variableId) is not { } variable)
                return false;
            try
            {
                object? stored = JobVariableRuntimeValueReader.Read(variable);
                if (!string.IsNullOrWhiteSpace(binding.ValuePath)
                    && !ResultBindingResolver.TryReadPath(stored, binding.ValuePath, out stored))
                    return false;
                value = stored switch
                {
                    null => "?",
                    DateTime dateTime => dateTime.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    bool boolean => boolean ? "true" : "false",
                    IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
                    _ => stored.ToString() ?? "?"
                };
                return true;
            }
            catch (Exception exception) when (exception is InvalidOperationException
                or FormatException
                or System.Text.Json.JsonException)
            {
                return false;
            }
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
