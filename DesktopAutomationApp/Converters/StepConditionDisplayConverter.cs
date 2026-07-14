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
    /// <summary>
    /// Builds a readable single-line text for If/ElseIf conditions in step list previews.
    /// values[0] = StepCondition
    /// values[1] = Steps-Collection (IList) — used to resolve current step number dynamically
    /// values[2] = StepsVersion (int) — cache-key so the name updates on every reorder
    /// </summary>
    public sealed class StepConditionDisplayConverter : IMultiValueConverter
    {
        private int _cacheVersion = int.MinValue;
        private Dictionary<string, string>? _nameMap;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is null || values.Length < 1 || values[0] is not StepCondition c)
                return string.Empty;

            // Rebuild name map when version changes
            var list    = values.Length > 1 ? values[1] as IList : null;
            int version = values.Length > 2 && values[2] is int v ? v : 0;

            if (list != null && (_nameMap is null || version != _cacheVersion))
            {
                _nameMap = new Dictionary<string, string>(list.Count, StringComparer.Ordinal);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is JobStep step)
                    {
                        _nameMap[step.Id] = StepLocalization.NumberedName(step.GetType(), i + 1);
                    }
                }
                _cacheVersion = version;
            }

            // Resolve source name: prefer live lookup, fall back to stored display name
            string source;
            if (_nameMap != null && !string.IsNullOrWhiteSpace(c.SourceStepId)
                && _nameMap.TryGetValue(c.SourceStepId, out var resolved))
                source = resolved;
            else
                source = !string.IsNullOrWhiteSpace(c.SourceStepDisplayName)
                    ? c.SourceStepDisplayName
                    : (string.IsNullOrWhiteSpace(c.SourceStepId) ? Loc.Get("Step.Unknown") : c.SourceStepId);

            var prop = !string.IsNullOrWhiteSpace(c.Property)
                ? StepLocalization.Property(c.Property, c.PropertyDisplayName)
                : c.PropertyDisplayName;
            if (string.IsNullOrWhiteSpace(prop)) prop = Loc.Get("Common.Value");

            var op = c.Operator switch
            {
                ConditionOperator.IsTrue            => Loc.Get("Condition.IsTrue.Lower"),
                ConditionOperator.IsFalse           => Loc.Get("Condition.IsFalse.Lower"),
                ConditionOperator.Equals            => "=",
                ConditionOperator.NotEquals         => "!=",
                ConditionOperator.GreaterThan       => ">",
                ConditionOperator.LessThan          => "<",
                ConditionOperator.GreaterThanOrEqual => ">=",
                ConditionOperator.LessThanOrEqual   => "<=",
                _ => c.Operator.ToString()
            };

            var needsComparisonValue = c.Operator is not ConditionOperator.IsTrue and not ConditionOperator.IsFalse;
            var comparison = c.ComparisonValue?.Trim() ?? string.Empty;

            if (needsComparisonValue && !string.IsNullOrWhiteSpace(comparison))
                return $"{source} -> {prop} {op} \"{comparison}\"";

            return $"{source} -> {prop} {op}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
