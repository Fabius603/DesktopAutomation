using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Converter, der den tatsächlichen Index eines Items in einer Collection berechnet.
    /// </summary>
    public class StepNumberConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length >= 2 && values[0] != null && values[1] is IList collection)
            {
                var item = values[0];
                var index = collection.IndexOf(item);
                
                if (index >= 0)
                    return $"{index + 1}.";
            }
            
            return "?.";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
