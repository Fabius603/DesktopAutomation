using System.Globalization;
using System.Windows.Data;

namespace DesktopAutomationApp.Converters;

public sealed class OneBasedIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is int index ? index + 1 : 1;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
