using System.Globalization;
using System.Windows.Data;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.Converters;

public sealed class LocalizedEnumConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Enum enumValue) return value?.ToString() ?? string.Empty;
        var key = $"Enum.{enumValue.GetType().Name}.{enumValue}";
        var translated = LocalizationService.Instance[key];
        return translated == $"[{key}]" ? enumValue.ToString() : translated;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
}

