using System;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Converts a <see cref="ConditionOperator"/> enum value to a human-readable German string.
    /// </summary>
    public sealed class ConditionOperatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConditionOperator op)
            {
                return op switch
                {
                    ConditionOperator.IsTrue             => "Ist wahr",
                    ConditionOperator.IsFalse            => "Ist falsch",
                    ConditionOperator.Equals             => "=",
                    ConditionOperator.NotEquals          => "≠",
                    ConditionOperator.GreaterThan        => ">",
                    ConditionOperator.LessThan           => "<",
                    ConditionOperator.GreaterThanOrEqual => "≥",
                    ConditionOperator.LessThanOrEqual    => "≤",
                    _                                    => op.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
