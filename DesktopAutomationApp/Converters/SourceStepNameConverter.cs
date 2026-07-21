using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using DesktopAutomationApp.Localization;

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
            if (values is null || values.Length < 2) return Loc.Get("Ui.Job.Steps.NoSourceSelected");
            if (values[0] is not ResultBinding { IsConfigured: true } binding)
                return Loc.Get("Ui.Job.Steps.NoSourceSelected");

            var list = values[1] as IList;
            if (list is null) return binding.SourceStepId;

            int version = values.Length > 2 && values[2] is int v ? v : 0;

            if (_nameMap is null || version != _cacheVersion)
            {
                _nameMap = new Dictionary<string, string>(list.Count, StringComparer.Ordinal);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is JobStep step)
                    {
                        _nameMap[step.Id] = StepLocalization.ResultStepName(step, list);
                    }
                }
                _cacheVersion = version;
            }

            if (!_nameMap.TryGetValue(binding.SourceStepId, out var name))
                return Loc.Get("Ui.Job.Steps.SourceUnavailable");

            var source = list.Cast<object>().OfType<JobStep>()
                .FirstOrDefault(step => step.Id == binding.SourceStepId);
            var output = source is null ? null : StepPipelineRegistry.Get(source.GetType())?.Output;
            var property = string.IsNullOrWhiteSpace(output)
                ? null
                : StepResultMetadata.GetResultType(output)?.Properties.FirstOrDefault(candidate =>
                    candidate.Name.Equals(binding.PropertyPath, StringComparison.OrdinalIgnoreCase));
            var propertyName = property is null
                ? binding.PropertyPath
                : StepLocalization.PropertyPath(property.Name);
            var cardinality = property?.Cardinality == ResultCardinality.Collection ? " · Liste" : string.Empty;
            return $"{name}  →  {propertyName}{cardinality}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
