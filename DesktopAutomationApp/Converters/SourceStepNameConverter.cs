using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Löst eine SourceStepId (string) in einen lesbaren Anzeigenamen auf.
    /// values[0] = sourceStepId (string)
    /// values[1] = Steps-Collection (IList)
    /// values[2] = StepsVersion (int) — Cache-Key, damit bei jedem Move neu gebaut wird.
    /// Gibt "–" zurück wenn die ID leer ist, sonst "FriendlyName (Step N)".
    /// </summary>
    public class SourceStepNameConverter : IMultiValueConverter
    {
        private int _cacheVersion = int.MinValue;
        private Dictionary<string, string>? _nameMap;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is null || values.Length < 2) return "–";
            if (values[0] is not string id || string.IsNullOrWhiteSpace(id)) return "–";

            var list = values[1] as IList;
            if (list is null) return id;

            int version = values.Length > 2 && values[2] is int v ? v : 0;

            if (_nameMap is null || version != _cacheVersion)
            {
                _nameMap = new Dictionary<string, string>(list.Count, StringComparer.Ordinal);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is JobStep step)
                    {
                        var friendly = StepResultMetadata.GetFriendlyName(step.GetType().Name);
                        _nameMap[step.Id] = $"{friendly} (Step {i + 1})";
                    }
                }
                _cacheVersion = version;
            }

            return _nameMap.TryGetValue(id, out var name) ? name : $"(Unbekannt)";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
