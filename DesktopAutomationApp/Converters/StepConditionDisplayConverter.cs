using System;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Builds a readable single-line text for If/ElseIf conditions in step list previews.
    /// </summary>
    public sealed class StepConditionDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not StepCondition c)
                return string.Empty;

            var source = !string.IsNullOrWhiteSpace(c.SourceStepDisplayName)
                ? c.SourceStepDisplayName
                : c.SourceStepId;

            var prop = !string.IsNullOrWhiteSpace(c.PropertyDisplayName)
                ? c.PropertyDisplayName
                : c.Property;

            source = string.IsNullOrWhiteSpace(source) ? "Unbekannter Step" : source;
            prop = string.IsNullOrWhiteSpace(prop) ? "Wert" : prop;

            var op = c.Operator switch
            {
                ConditionOperator.IsTrue => "ist wahr",
                ConditionOperator.IsFalse => "ist falsch",
                ConditionOperator.Equals => "=",
                ConditionOperator.NotEquals => "!=",
                ConditionOperator.GreaterThan => ">",
                ConditionOperator.LessThan => "<",
                ConditionOperator.GreaterThanOrEqual => ">=",
                ConditionOperator.LessThanOrEqual => "<=",
                _ => c.Operator.ToString()
            };

            var needsComparisonValue = c.Operator is not ConditionOperator.IsTrue and not ConditionOperator.IsFalse;
            var comparison = c.ComparisonValue?.Trim() ?? string.Empty;

            if (needsComparisonValue && !string.IsNullOrWhiteSpace(comparison))
                return $"{source} -> {prop} {op} \"{comparison}\"";

            return $"{source} -> {prop} {op}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
