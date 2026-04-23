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
    /// MultiValueConverter: [0] = aktueller JobStep, [1] = IEnumerable&lt;JobStep&gt; (alle Steps),
    ///                       [2] = StepsVersion (int) — cache key.
    /// Gibt IReadOnlyList&lt;PrerequisiteDisplayItem&gt; zurück – jedes Item enthält Name + IsSatisfied.
    /// IsSatisfied = true, wenn ein VORHERIGER Step im Job das benötigte Ergebnis liefert.
    ///
    /// Der Scan der gesamten Liste wird pro StepsVersion genau einmal durchgeführt (O(n)),
    /// danach ist jede Einzelabfrage O(1).
    /// </summary>
    public sealed class StepPrerequisiteStateConverter : IMultiValueConverter
    {
        public sealed record PrerequisiteDisplayItem(string Name, bool IsSatisfied);

        // ── Cache ─────────────────────────────────────────────────────────────────
        private int _cacheVersion = int.MinValue;
        private Dictionary<JobStep, List<PrerequisiteDisplayItem>>? _prereqMap;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not JobStep step)
                return Array.Empty<PrerequisiteDisplayItem>();

            int version = values.Length > 2 && values[2] is int v ? v : 0;

            if (_prereqMap == null || version != _cacheVersion)
            {
                _prereqMap    = BuildPrereqMap(values[1] as System.Collections.IEnumerable);
                _cacheVersion = version;
            }

            return _prereqMap.TryGetValue(step, out var list)
                ? list
                : (object)Array.Empty<PrerequisiteDisplayItem>();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        // ── helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Single O(n) pass: for each step, records which prerequisites are satisfied
        /// by the steps that precede it.
        /// </summary>
        private static Dictionary<JobStep, List<PrerequisiteDisplayItem>> BuildPrereqMap(
            System.Collections.IEnumerable? allSteps)
        {
            var map       = new Dictionary<JobStep, List<PrerequisiteDisplayItem>>(ReferenceEqualityComparer.Instance);
            var available = new HashSet<string>(StringComparer.Ordinal);

            if (allSteps == null) return map;

            foreach (var obj in allSteps)
            {
                if (obj is not JobStep s) continue;

                var prereqs = StepPipelineRegistry.Get(s.GetType())?.Prerequisites
                              ?? Array.Empty<string>();

                if (prereqs.Length > 0)
                {
                    var items = new List<PrerequisiteDisplayItem>(prereqs.Length);
                    foreach (var p in prereqs)
                        items.Add(new PrerequisiteDisplayItem(p, available.Contains(p)));
                    map[s] = items;
                }

                var output = StepPipelineRegistry.Get(s.GetType())?.Output;
                if (output != null) available.Add(output);
            }

            return map;
        }
    }
}
