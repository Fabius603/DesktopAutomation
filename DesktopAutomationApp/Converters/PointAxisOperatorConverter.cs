using System;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Converts a <see cref="PointAxisOperator"/> enum value to a human-readable symbol string.
    /// </summary>
    public sealed class PointAxisOperatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PointAxisOperator op)
            {
                return op switch
                {
                    PointAxisOperator.LessThan           => "<",
                    PointAxisOperator.LessThanOrEqual    => "≤",
                    PointAxisOperator.GreaterThan        => ">",
                    PointAxisOperator.GreaterThanOrEqual => "≥",
                    PointAxisOperator.Equal              => "=",
                    PointAxisOperator.NotEqual           => "≠",
                    _                                    => op.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
