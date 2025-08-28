using System;
using System.Globalization;
using System.Windows.Data;
using OpenCvSharp;

namespace DesktopAutomationApp.Converters
{
    /// <summary>
    /// Konvertiert eine OpenCV Rect Struktur zu einem lesbaren String im Format "X,Y,W,H"
    /// </summary>
    public sealed class RectToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Rect rect)
            {
                return $"{rect.X}, {rect.Y}, {rect.Width}, {rect.Height}";
            }
            return "0, 0, 0, 0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
