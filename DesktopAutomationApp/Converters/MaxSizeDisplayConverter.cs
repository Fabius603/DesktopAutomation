using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopAutomationApp.Converters
{
    public sealed class MaxSizeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && intValue == int.MaxValue)
                return "Unbegrenzt";

            return value?.ToString() ?? "Unbegrenzt";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString();

            if (string.IsNullOrWhiteSpace(text) ||
                text.Equals("Unbegrenzt", StringComparison.OrdinalIgnoreCase))
            {
                return int.MaxValue;
            }

            if (int.TryParse(text, out var result) && result > 0)
                return result;

            return Binding.DoNothing;
        }
    }
}