using System;
using System.Globalization;
using System.Windows.Data;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.Converters;

public sealed class MacroStepTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var type = value?.ToString() ?? string.Empty;
        if (type.Length == 0) return string.Empty;

        var key = $"Macro.Step.Type.{type}";
        var translated = LocalizationService.Instance[key];
        return translated == $"[{key}]" ? type : translated;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}
