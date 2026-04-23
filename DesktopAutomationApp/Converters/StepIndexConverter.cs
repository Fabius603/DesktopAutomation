using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

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
        private Dictionary<object, int>? _indexMap;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length < 2 || values![0] == null || values[1] is not IList collection)
                return "?.";

            int version = values.Length > 2 && values[2] is int v ? v : 0;

            if (_indexMap == null || version != _cacheVersion)
            {
                _indexMap = new Dictionary<object, int>(collection.Count, ReferenceEqualityComparer.Instance);
                for (int i = 0; i < collection.Count; i++)
                    if (collection[i] != null) _indexMap[collection[i]!] = i;
                _cacheVersion = version;
            }

            return _indexMap.TryGetValue(values[0], out var idx) ? $"{idx + 1}." : "?.";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
