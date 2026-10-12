using System.Globalization;
using System.Windows.Data;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace DesktopAutomationApp.Converters;

public sealed class StepDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is JobStep step && BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition)
            ? StepLocalization.Description(definition.Descriptor) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
