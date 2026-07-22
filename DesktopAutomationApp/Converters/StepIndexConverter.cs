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
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length < 2 || values[1] is not IList collection || values[0] is not { } item)
                return string.Empty;
            if (item is JobStep step)
            {
                var number = StepLocalization.DisplayNumber(collection, step);
                return number.HasValue ? $"{number.Value}.\u00A0" : string.Empty;
            }
            var index = collection.IndexOf(item);
            return index >= 0 ? $"{index + 1}.\u00A0" : string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
