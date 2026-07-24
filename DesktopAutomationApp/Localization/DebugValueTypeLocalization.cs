using System;

namespace DesktopAutomationApp.Localization;

internal static class DebugValueTypeLocalization
{
    public static string Localize(
        string typeName,
        Func<string, string, string> getOrFallback)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return getOrFallback("Step.ResultValueType.ResultObject", "Result object");

        var normalized = typeName.TrimEnd('?');
        if (normalized.EndsWith("[]", StringComparison.Ordinal))
            return CollectionOf(
                Localize(normalized[..^2], getOrFallback),
                getOrFallback);

        var genericStart = normalized.IndexOf('<');
        if (genericStart > 0 && normalized.EndsWith('>'))
        {
            var genericName = normalized[..genericStart];
            var genericArgument = normalized[(genericStart + 1)..^1].Split(',')[0].Trim();
            if (genericName is "Array" or "Collection" or "Enumerable" or "IEnumerable"
                or "List" or "IList" or "ICollection" or "IReadOnlyCollection" or "IReadOnlyList")
                return CollectionOf(Localize(genericArgument, getOrFallback), getOrFallback);
        }

        var (key, fallback) = normalized switch
        {
            "Boolean" => ("Boolean", "Boolean"),
            "Byte" or "SByte" or "Int16" or "UInt16" or "Int32" or "UInt32"
                or "Int64" or "UInt64" => ("Integer", "Integer"),
            "Single" or "Double" or "Decimal" => ("Number", "Number"),
            "String" or "Char" or "Guid" or "Uri" => ("Text", "Text"),
            "DateTime" or "DateTimeOffset" => ("DateTime", "Date/time"),
            "TimeSpan" => ("Duration", "Duration"),
            "Bitmap" => ("Image", "Image"),
            "Point" => ("Point", "Point"),
            "Rectangle" => ("Rectangle", "Rectangle"),
            "DetectionItem" => ("Detection", "Detection"),
            "RuntimeProcessReference" => ("Process", "Process reference"),
            _ => ("ResultObject", "Result object")
        };
        return getOrFallback($"Step.ResultValueType.{key}", fallback);
    }

    private static string CollectionOf(
        string elementType,
        Func<string, string, string> getOrFallback)
        => getOrFallback("Step.ResultValueType.Collection", "List of {0}")
            .Replace("{0}", elementType, StringComparison.Ordinal);
}
