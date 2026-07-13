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
                var key = $"Step.Type.{step.GetType().Name}";
                var translated = LocalizationService.Instance[key];
                return translated == $"[{key}]" ? StepPipelineRegistry.GetDisplayName(step.GetType()) : translated;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
