using System.Globalization;

namespace TaskAutomation.Makros;

public static class MakroTimeFormatter
{
    public static string FormatMicroseconds(long microseconds, bool includePrefix = false)
    {
        var prefix = includePrefix ? "+" : string.Empty;
        var absolute = Math.Abs((double)microseconds);
        var (value, unit) = absolute switch
        {
            >= 3_600_000_000d => (microseconds / 3_600_000_000d, "h"),
            >= 60_000_000d => (microseconds / 60_000_000d, "min"),
            >= 1_000_000d => (microseconds / 1_000_000d, "s"),
            >= 1_000d => (microseconds / 1_000d, "ms"),
            _ => (microseconds, "\u00B5s")
        };
        return $"{prefix}{value.ToString("0.###", CultureInfo.CurrentCulture)} {unit}";
    }

    public static string FormatMilliseconds(long milliseconds)
        => FormatMicroseconds(checked(milliseconds * 1_000L));
}
