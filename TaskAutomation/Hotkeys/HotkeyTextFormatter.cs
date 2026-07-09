using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace TaskAutomation.Hotkeys
{
    public static class HotkeyTextFormatter
    {
        private const uint MAPVK_VK_TO_VSC = 0;

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

        [DllImport("user32.dll")]
        private static extern int ToUnicodeEx(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags,
            IntPtr dwhkl);

        public static string Format(KeyModifiers mods, uint vk, string separator = " + ")
        {
            var parts = new List<string>(5);
            if (mods.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (mods.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
            if (mods.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
            if (mods.HasFlag(KeyModifiers.Windows)) parts.Add("Win");

            var keyText = VirtualKeyToText(vk);
            if (!string.IsNullOrWhiteSpace(keyText))
                parts.Add(keyText);

            return string.Join(separator, parts);
        }

        public static string VirtualKeyToText(uint vk)
        {
            if ((vk >= 0x30 && vk <= 0x39) || (vk >= 0x41 && vk <= 0x5A))
                return ((char)vk).ToString().ToUpperInvariant();

            if (vk >= 0x70 && vk <= 0x7B)
                return $"F{vk - 0x6F}";

            var key = KeyInterop.KeyFromVirtualKey(unchecked((int)vk));
            var special = KeyToPrettyString(key);
            if (special is not null)
                return special;

            var keyboardText = TryGetKeyboardLayoutText(vk);
            if (!string.IsNullOrWhiteSpace(keyboardText))
                return keyboardText.ToUpperInvariant();

            if (key != Key.None)
                return key.ToString();

            return $"0x{vk:X2}";
        }

        private static string? TryGetKeyboardLayoutText(uint vk)
        {
            try
            {
                var layout = GetKeyboardLayout(0);
                var scanCode = MapVirtualKeyEx(vk, MAPVK_VK_TO_VSC, layout);
                var keyboardState = new byte[256];
                var buffer = new StringBuilder(8);
                var result = ToUnicodeEx(vk, scanCode, keyboardState, buffer, buffer.Capacity, 0, layout);

                if (result > 0)
                    return buffer.ToString(0, Math.Min(result, buffer.Length));

                if (result < 0)
                {
                    var text = buffer.Length > 0
                        ? buffer.ToString(0, Math.Min(-result, buffer.Length))
                        : null;
                    ToUnicodeEx(vk, scanCode, keyboardState, buffer, buffer.Capacity, 0, layout);
                    return text;
                }
            }
            catch
            {
                // Fall back to framework key names below.
            }

            return null;
        }

        private static string? KeyToPrettyString(Key key) => key switch
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
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "Page Up",
            Key.PageDown => "Page Down",
            Key.PrintScreen => "Print Screen",
            Key.Pause => "Pause",
            Key.CapsLock => "Caps Lock",
            Key.NumLock => "Num Lock",
            Key.Scroll => "Scroll Lock",
            _ => null
        };
    }
}
