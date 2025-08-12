using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using TaskAutomation.Hotkeys;

namespace DesktopAutomationApp.Converters
{
    [ValueConversion(typeof(HotkeyDefinition), typeof(string))]
    public class HotkeyDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HotkeyDefinition def) return string.Empty;

            var parts = new List<string>();
            if (def.Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (def.Modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
            if (def.Modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
            if (def.Modifiers.HasFlag(KeyModifiers.Windows)) parts.Add("Win");

            // VirtualKeyCode -> WPF Key -> String
            string keyText = VkToText(def.VirtualKeyCode);
            parts.Add(keyText);

            return string.Join(" + ", parts);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static string VkToText(uint vk)
        {
            try
            {
                var key = KeyInterop.KeyFromVirtualKey((int)vk);
                if (key != Key.None) return KeyToPrettyString(key);
            }
            catch { /* ignore and fall back */ }

            // Fallbacks für alphanumerisch / Funktionstasten / Hex
            if ((vk >= 0x30 && vk <= 0x39) || (vk >= 0x41 && vk <= 0x5A))
                return ((char)vk).ToString().ToUpperInvariant();

            if (vk >= 0x70 && vk <= 0x7B) // F1..F12
                return $"F{vk - 0x6F}";

            return $"0x{vk:X2}";
        }

        private static string KeyToPrettyString(Key key) => key switch
        {
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Escape => "Esc",
            Key.Tab => "Tab",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            _ => key.ToString()
        };
    }
}
