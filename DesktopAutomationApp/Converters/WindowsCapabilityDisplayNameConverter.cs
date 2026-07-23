using System.Globalization;
using System.Windows.Data;
using DesktopAutomationApp.Localization;
using TaskAutomation.WindowsIntegration;

namespace DesktopAutomationApp.Converters;

public sealed class WindowsCapabilityDisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is WindowsCapabilityDescriptor descriptor
            ? WindowsCapabilityLocalization.DisplayName(descriptor)
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
