using TaskAutomation.Steps;

namespace DesktopAutomationApp.Localization;

public static class StepLocalization
{
    public static string Type(Type type) => Type(type.Name);

    public static string Type(string typeName)
    {
        var name = typeName.EndsWith("Step", StringComparison.Ordinal)
            ? typeName[..^4]
            : typeName;
        return GetOrFallback($"Step.Type.{name}", StepPipelineRegistry.GetDisplayName(typeName));
    }

    public static string Property(string propertyName, string? fallback = null) =>
        GetOrFallback($"Step.ResultProperty.{propertyName}", fallback ?? propertyName);

    public static ResultTypeDescriptor ResultType(ResultTypeDescriptor descriptor) =>
        new(
            descriptor.TypeName,
            descriptor.DisplayName,
            descriptor.Properties
                .Select(p => new ResultPropertyDescriptor(p.Name, Property(p.Name, p.DisplayName), p.PropertyType))
                .ToArray());

    public static string NumberedName(Type type, int oneBasedIndex) =>
        $"{Type(type)} ({Loc.Format("Step.Number", oneBasedIndex)})";

    private static string GetOrFallback(string key, string fallback)
    {
        var value = LocalizationService.Instance[key];
        return value == $"[{key}]" ? fallback : value;
    }
}
