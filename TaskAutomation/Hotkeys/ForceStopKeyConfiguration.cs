using System.Threading;

namespace TaskAutomation.Hotkeys;

public static class ForceStopKeyConfiguration
{
    public const uint DefaultVirtualKey = 0x79; // F10

    private static int _virtualKey = unchecked((int)DefaultVirtualKey);

    public static uint VirtualKey => unchecked((uint)Volatile.Read(ref _virtualKey));

    public static bool IsValid(uint virtualKey) =>
        virtualKey != 0 && !IsModifierKey(virtualKey);

    public static uint Normalize(uint virtualKey) =>
        IsValid(virtualKey) ? virtualKey : DefaultVirtualKey;

    public static void Set(uint virtualKey)
    {
        if (!IsValid(virtualKey))
            throw new ArgumentException(
                "Der Force-Stop-Hotkey benötigt eine Nicht-Modifier-Taste.",
                nameof(virtualKey));

        Volatile.Write(ref _virtualKey, unchecked((int)virtualKey));
    }

    public static bool Matches(uint virtualKey) => virtualKey == VirtualKey;

    private static bool IsModifierKey(uint virtualKey) => virtualKey is
        0x10 or 0x11 or 0x12 or
        0xA0 or 0xA1 or
        0xA2 or 0xA3 or
        0xA4 or 0xA5 or
        0x5B or 0x5C;
}
