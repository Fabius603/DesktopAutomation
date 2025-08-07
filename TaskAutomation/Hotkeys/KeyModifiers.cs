using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAutomation.Hotkeys
{
    /// <summary>
    /// Flags für die Modifier-Tasten (Strg, Alt, Shift, Windows).
    /// </summary>
    [Flags]
    public enum KeyModifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Windows = 0x0008
    }
}
