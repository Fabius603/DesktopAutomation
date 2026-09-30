using TaskAutomation.Contracts.Steps;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Localization;

public static class EnumValueLocalization
{
    public static string ForStepOption(StepFieldOptionDescriptor option)
    {
        if (!string.IsNullOrWhiteSpace(option.DisplayName))
            return option.DisplayName;

        return LocalizedOrFallback(option.LabelKey, option.Value);
    }

    public static string ForResultValue(ResultPropertyDescriptor property, string value)
    {
        if (property.EnumDisplayNames?.TryGetValue(value, out var displayName) == true
            && !string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return LocalizedOrFallback($"Enum.{ShortTypeName(property.EnumTypeName)}.{value}", value);
    }

    public static string ShortTypeName(string? enumTypeName) =>
        enumTypeName?.Split(['.', '+'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "Enum";

    private static string LocalizedOrFallback(string key, string fallback)
    {
        var localized = Loc.Get(key);
        return localized == $"[{key}]" ? fallback : localized;
    }
}
