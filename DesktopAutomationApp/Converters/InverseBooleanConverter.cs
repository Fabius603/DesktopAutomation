using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;

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

    /// <summary>
    /// Konvertiert bool IsSatisfied → Brush.
    /// ConverterParameter: "foreground", "background" oder "border".
    /// true  (erfüllt) → Akzentfarbe (blau/türkis)
    /// false (fehlt)   → Rot
    /// </summary>
    public sealed class PrerequisiteStateConverter : IValueConverter
    {
        private static readonly SolidColorBrush RedForeground   = new(Color.FromArgb(0xFF, 0xCC, 0x33, 0x33));
        private static readonly SolidColorBrush RedBackground   = new(Color.FromArgb(0x22, 0xFF, 0x33, 0x33));
        private static readonly SolidColorBrush RedBorder       = new(Color.FromArgb(0xFF, 0xCC, 0x33, 0x33));

        static PrerequisiteStateConverter()
        {
            RedForeground.Freeze();
            RedBackground.Freeze();
            RedBorder.Freeze();
        }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool satisfied = value is not bool b || b;
            if (satisfied) return null; // null → Binding greift auf StaticResource zurück (via FallbackValue)

            return (parameter as string) switch
            {
                "background" => RedBackground,
                "border"     => RedBorder,
                _            => RedForeground,  // "foreground" oder default
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
