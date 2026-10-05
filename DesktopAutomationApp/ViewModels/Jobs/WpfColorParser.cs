using System.Windows.Media;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels;

internal static class WpfColorParser
{
    public static bool TryParse(string? value, out Color color)
    {
        var success = ColorValueRules.TryParse(value, out var parsed);
        color = Color.FromArgb(parsed.A, parsed.R, parsed.G, parsed.B);
        return success;
    }
}
