using System;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Converters
{
    public sealed class ConditionMatchModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConditionMatchMode mode)
            {
                return mode switch
                {
                    ConditionMatchMode.All => "Alle Bedingungen",
                    ConditionMatchMode.Any => "Eine Bedingung",
                    _                      => mode.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
