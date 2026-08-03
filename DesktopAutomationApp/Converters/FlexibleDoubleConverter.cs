using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesktopAutomationApp.Converters;

public sealed class FlexibleDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double number
            ? number.ToString("0.###", culture)
            : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return TryParse(value?.ToString(), culture, out var number)
            ? number
            : Binding.DoNothing;
    }

    public static string Format(double value, CultureInfo? culture = null) =>
        value.ToString("0.###", culture ?? CultureInfo.CurrentCulture);

    public static bool TryParse(string? value, out double number) =>
        TryParse(value, CultureInfo.CurrentCulture, out number);

    public static bool TryParse(string? value, CultureInfo culture, out double number)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            number = default;
            return false;
        }

        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        return double.TryParse(text, styles, culture, out number)
            || double.TryParse(text, styles, CultureInfo.InvariantCulture, out number)
            || double.TryParse(text.Replace(',', '.'), styles, CultureInfo.InvariantCulture, out number);
    }
}
