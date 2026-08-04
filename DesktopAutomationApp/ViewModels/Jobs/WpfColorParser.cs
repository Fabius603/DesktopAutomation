using System.Globalization;
using System.Reflection;
using System.Windows.Media;

namespace DesktopAutomationApp.ViewModels;

internal static class WpfColorParser
{
    private static readonly IReadOnlyDictionary<string, Color> NamedColors =
        typeof(Colors)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(Color))
            .ToDictionary(
                property => property.Name,
                property => (Color)property.GetValue(null)!,
                StringComparer.OrdinalIgnoreCase);

    public static bool TryParse(string? value, out Color color)
    {
        color = Colors.White;
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
            return false;

        if (!text.StartsWith('#'))
        {
            if (NamedColors.TryGetValue(text, out var namedColor))
            {
                color = namedColor;
                return true;
            }
            return false;
        }

        var hex = text.AsSpan(1);
        return hex.Length switch
        {
            3 => TryParseShort(hex, hasAlpha: false, out color),
            4 => TryParseShort(hex, hasAlpha: true, out color),
            6 => TryParseLong(hex, hasAlpha: false, out color),
            8 => TryParseLong(hex, hasAlpha: true, out color),
            _ => false
        };
    }

    private static bool TryParseShort(ReadOnlySpan<char> hex, bool hasAlpha, out Color color)
    {
        color = Colors.White;
        Span<byte> parts = stackalloc byte[4];
        var offset = hasAlpha ? 0 : 1;
        if (!hasAlpha)
            parts[0] = byte.MaxValue;
        for (var index = 0; index < hex.Length; index++)
        {
            if (!byte.TryParse(hex.Slice(index, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var nibble))
                return false;
            parts[index + offset] = (byte)(nibble * 17);
        }
        color = Color.FromArgb(parts[0], parts[1], parts[2], parts[3]);
        return true;
    }

    private static bool TryParseLong(ReadOnlySpan<char> hex, bool hasAlpha, out Color color)
    {
        color = Colors.White;
        Span<byte> parts = stackalloc byte[4];
        var offset = hasAlpha ? 0 : 1;
        if (!hasAlpha)
            parts[0] = byte.MaxValue;
        for (var index = 0; index < hex.Length / 2; index++)
        {
            if (!byte.TryParse(hex.Slice(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parts[index + offset]))
                return false;
        }
        color = Color.FromArgb(parts[0], parts[1], parts[2], parts[3]);
        return true;
    }
}
