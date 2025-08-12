using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace DesktopAutomationApp.Converters
{
    // true -> false, false -> true
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Erwartet als ConverterParameter einen String "TextWennFalse,TextWennTrue".
    /// Bei value==true wird Teil[1], sonst Teil[0] verwendet.
    /// </summary>
    public sealed class InverseBooleanToContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var p = parameter as string ?? string.Empty;
            var parts = p.Split(new[] { ',' }, 2);
            var whenFalse = parts.Length > 0 ? parts[0] : string.Empty;
            var whenTrue = parts.Length > 1 ? parts[1] : string.Empty;

            if (value is bool b)
                return b ? whenTrue : whenFalse;

            return whenFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
