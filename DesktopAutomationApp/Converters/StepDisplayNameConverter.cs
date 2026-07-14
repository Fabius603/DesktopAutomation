using System;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Wandelt einen <see cref="JobStep"/> in seinen Anzeigenamen um.
    /// Quelle ist ausschließlich <see cref="StepPipelineRegistry.GetDisplayName(Type)"/>.
    /// </summary>
    public class StepDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStep step)
            {
                return StepLocalization.Type(step.GetType());
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
