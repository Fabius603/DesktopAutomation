using System;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.Converters
{
    [ValueConversion(typeof(HotkeyDefinition), typeof(string))]
    public class HotkeyDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HotkeyDefinition def) return string.Empty;

            return HotkeyTextFormatter.Format(def.Modifiers, def.VirtualKeyCode);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

    }
}
