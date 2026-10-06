using WindowsInput.Native;

namespace TaskAutomation.Makros;

/// <summary>Canonical macro command catalog, key parsing and scheduled durations.</summary>
public static class MakroCommandRules
{
    public static IReadOnlyList<string> Types { get; } = Array.AsReadOnly(new[]
    {
        "MouseMoveAbsolute", "MouseMoveRelative", "MouseWheel", "MouseDown", "MouseUp",
        "KeyDown", "KeyUp", "Timeout", "TextInput", "KeyCombination"
    });

    public static string TypeId(MakroBefehl command) => command.GetType().Name.Replace("Befehl", "", StringComparison.Ordinal);

    public static long DurationMicroseconds(MakroBefehl command) => command switch
    {
        TimeoutBefehl wait => wait.Duration * 1_000L,
        TextInputBefehl text => text.DurationMicroseconds,
        KeyCombinationBefehl keys => keys.DurationMicroseconds,
        _ => 0
    };

    public static bool TryParseKey(string? key, out VirtualKeyCode code)
    {
        var value = key?.Trim().ToUpperInvariant() ?? string.Empty;
        value = value switch { "CTRL" or "STRG" => "CONTROL", "WIN" => "LWIN", "ALT" => "MENU", "ENTER" => "RETURN", "ESC" => "ESCAPE", "BACKSPACE" => "BACK", "PAGE UP" => "PRIOR", "PAGE DOWN" => "NEXT", "CAPS LOCK" => "CAPITAL", "NUM LOCK" => "NUMLOCK", "SCROLL LOCK" => "SCROLL", "PRINT SCREEN" => "SNAPSHOT", _ => value };
        if (value.Length == 1 && char.IsAsciiLetterOrDigit(value[0])) value = "VK_" + value;
        return Enum.TryParse(value, true, out code) && Enum.IsDefined(code);
    }

    public static bool IsModifier(VirtualKeyCode key) => key is VirtualKeyCode.CONTROL or VirtualKeyCode.LCONTROL or VirtualKeyCode.RCONTROL
        or VirtualKeyCode.SHIFT or VirtualKeyCode.LSHIFT or VirtualKeyCode.RSHIFT
        or VirtualKeyCode.MENU or VirtualKeyCode.LMENU or VirtualKeyCode.RMENU or VirtualKeyCode.LWIN or VirtualKeyCode.RWIN;

    public static bool TryParseCombination(IEnumerable<string>? keys, out VirtualKeyCode[] parsed)
    {
        parsed = [];
        if (keys is null) return false;
        var result = new List<VirtualKeyCode>();
        foreach (var key in keys)
        {
            if (!TryParseKey(key, out var code) || result.Contains(code)) return false;
            result.Add(code);
        }
        if (result.Count < 2 || result.Take(result.Count - 1).Any(key => !IsModifier(key)) || IsModifier(result[^1])) return false;
        parsed = result.ToArray();
        return true;
    }

    public static string Parameters(MakroBefehl command) => command switch
    {
        MouseMoveAbsoluteBefehl move => $"X: {move.X}, Y: {move.Y}",
        MouseMoveRelativeBefehl move => $"ΔX: {move.DeltaX}, ΔY: {move.DeltaY}",
        MouseWheelBefehl wheel => $"ΔX: {wheel.DeltaX}, ΔY: {wheel.DeltaY}",
        MouseDownBefehl mouse => mouse.Button,
        MouseUpBefehl mouse => mouse.Button,
        KeyDownBefehl key => key.Key,
        KeyUpBefehl key => key.Key,
        TimeoutBefehl wait => MakroTimeFormatter.FormatMilliseconds(wait.Duration),
        TextInputBefehl text => text.Text,
        KeyCombinationBefehl keys => string.Join(" + ", keys.Keys ?? []),
        _ => string.Empty
    };
}
