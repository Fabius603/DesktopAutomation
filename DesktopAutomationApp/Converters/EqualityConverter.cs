using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// MultiValueConverter: Gibt true zurück, wenn alle Werte als Strings gleich sind.
    /// Wird für den Sidebar Active-State verwendet (vergleicht CurrentContentName mit Button.Tag).
    /// </summary>
    public sealed class EqualityConverter : IMultiValueConverter
    {
        public static readonly EqualityConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not { Length: 2 })
                return false;

            var a = values[0]?.ToString();
            var b = values[1]?.ToString();
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
