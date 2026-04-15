using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// MultiValueConverter: prüft ob values[0] (Guid) in values[1] (IEnumerable&lt;Guid&gt;) enthalten ist.
    /// Gibt Visibility.Visible zurück wenn enthalten, sonst Collapsed.
    /// ConverterParameter = "Invert" kehrt das Ergebnis um.
    /// </summary>
    public sealed class CollectionContainsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Guid id && values[1] is IEnumerable<Guid> collection)
            {
                var contains = collection.Contains(id);
                var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
                var visible = invert ? !contains : contains;
                return visible ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
