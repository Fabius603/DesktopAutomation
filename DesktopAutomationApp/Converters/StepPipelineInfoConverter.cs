using System;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Wandelt einen <see cref="JobStep"/> in seine Voraussetzungen oder Ausgabe um.
    /// ConverterParameter: "prereqs" → kommagetrennte Voraussetzungen, "output" → Ausgabe-Typ.
    /// </summary>
    public class StepPipelineInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not JobStep step) return string.Empty;

            var info = StepPipelineRegistry.Get(step.GetType());
            if (info == null) return string.Empty;

            return parameter as string switch
            {
                "prereqs" => info.Prerequisites.Length == 0
                    ? "–"
                    : string.Join(", ", info.Prerequisites),
                "output"  => info.Output,
                _         => string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
