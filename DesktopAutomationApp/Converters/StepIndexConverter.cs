using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Converter, der den tatsächlichen Index eines Items in einer Collection berechnet.
    /// values[0] = item, values[1] = collection (IList), values[2] = StepsVersion (int) — cache key.
    /// Baut eine IndexMap einmalig pro Version (O(n)) und liefert danach O(1) pro Item.
    /// </summary>
    public class StepNumberConverter : IMultiValueConverter
    {
        private int _cacheVersion = int.MinValue;
        private IList? _cacheCollection;
        private Dictionary<JobStep, int> _numbers =
            new(ReferenceEqualityComparer.Instance);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length < 2 || values[1] is not IList collection || values[0] is not { } item)
                return string.Empty;
            if (item is JobStep step)
            {
                var version = values.Length > 2 && values[2] is int value ? value : 0;
                if (!ReferenceEquals(collection, _cacheCollection) || version != _cacheVersion)
                    RebuildCache(collection, version);
                return _numbers.TryGetValue(step, out var number)
                    ? $"{number}.\u00A0"
                    : string.Empty;
            }
            var index = collection.IndexOf(item);
            return index >= 0 ? $"{index + 1}.\u00A0" : string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private void RebuildCache(IList collection, int version)
        {
            var numbers = new Dictionary<JobStep, int>(
                collection.Count, ReferenceEqualityComparer.Instance);
            var number = 0;
            foreach (var item in collection)
            {
                if (item is not JobStep step || !StepLocalization.IsNumbered(step)) continue;
                numbers[step] = ++number;
            }
            _numbers = numbers;
            _cacheCollection = collection;
            _cacheVersion = version;
        }
    }
}
