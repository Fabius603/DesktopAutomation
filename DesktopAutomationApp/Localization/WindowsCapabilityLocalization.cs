using TaskAutomation.WindowsIntegration;

namespace DesktopAutomationApp.Localization;

public static class WindowsCapabilityLocalization
{
    public static string DisplayName(WindowsCapabilityDescriptor descriptor) =>
        GetOrFallback($"Windows.Capability.{descriptor.Id}", descriptor.DisplayName);

    public static string ParameterName(WindowsParameterDescriptor descriptor)
    {
        var typeKey = descriptor.Type switch
        {
            WindowsParameterType.ProcessName => "ProcessName",
            WindowsParameterType.Drive => "Drive",
            _ => descriptor.Name
        };
        return GetOrFallback($"Windows.Parameter.{typeKey}", descriptor.DisplayName);
    }

    public static string? ParameterPlaceholder(WindowsParameterDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Placeholder)) return null;
        var typeKey = descriptor.Type switch
        {
            WindowsParameterType.ProcessName => "ProcessName",
            WindowsParameterType.Drive => "Drive",
            _ => descriptor.Name
        };
        return GetOrFallback($"Windows.Parameter.Placeholder.{typeKey}", descriptor.Placeholder);
    }

    public static string Description(WindowsCapabilityDescriptor descriptor, bool isEvent) =>
        Loc.Format(isEvent ? "Ui.Windows.EventDescription" : "Ui.Windows.QueryDescription", DisplayName(descriptor));

    private static string GetOrFallback(string key, string fallback)
    {
        var value = Loc.Get(key);
        return value == $"[{key}]" ? fallback : value;
    }
}
