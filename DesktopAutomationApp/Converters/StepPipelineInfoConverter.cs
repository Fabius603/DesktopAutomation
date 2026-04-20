using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    /// <summary>
    /// MultiValueConverter: [0] = aktueller JobStep, [1] = IEnumerable&lt;JobStep&gt; (alle Steps).
    /// Gibt IReadOnlyList&lt;PrerequisiteDisplayItem&gt; zurück – jedes Item enthält Name + IsSatisfied.
    /// IsSatisfied = true, wenn ein VORHERIGER Step im Job das benötigte Ergebnis liefert.
    /// </summary>
    public sealed class StepPrerequisiteStateConverter : IMultiValueConverter
    {
        public sealed record PrerequisiteDisplayItem(string Name, bool IsSatisfied);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not JobStep step)
                return Array.Empty<PrerequisiteDisplayItem>();

            var prereqs = StepPipelineRegistry.Get(step.GetType())?.Prerequisites
                          ?? Array.Empty<string>();

            if (prereqs.Length == 0)
                return Array.Empty<PrerequisiteDisplayItem>();

            // Ausgaben aller Steps VOR dem aktuellen sammeln
            var available = new HashSet<string>(StringComparer.Ordinal);
            if (values[1] is System.Collections.IEnumerable allSteps)
            {
                foreach (var obj in allSteps)
                {
                    if (obj is not JobStep s) continue;
                    if (ReferenceEquals(s, step)) break;  // stop before current step
                    var info = StepPipelineRegistry.Get(s.GetType());
                    if (info != null) available.Add(info.Output);
                }
            }

            return prereqs
                .Select(p => new PrerequisiteDisplayItem(p, available.Contains(p)))
                .ToList();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
