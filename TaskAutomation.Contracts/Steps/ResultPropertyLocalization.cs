namespace TaskAutomation.Steps;

/// <summary>Shared display projection for application and documentation; never changes binding identity.</summary>
public static class ResultPropertyLocalization
{
    public static string Name(string? resultTypeName, string path, Func<string, string?> lookup)
    {
        var specific = lookup($"Step.ResultProperty.{resultTypeName}.{path}");
        var common = lookup($"Step.ResultProperty.{path}");
        if (specific is not null || common is not null) return specific ?? common!;
        return string.Join(" / ", path.Split('.').Select(segment =>
        {
            var clean = segment.Replace("[]", string.Empty, StringComparison.Ordinal);
            return lookup($"Step.ResultProperty.{clean}")
                ?? lookup($"Ui.Job.Steps.ResultProperty.{clean}")
                ?? System.Text.RegularExpressions.Regex.Replace(clean, "(?<=[a-z0-9])([A-Z])", " $1");
        }));
    }

    public static string? Description(string resultTypeName, string path, Func<string, string?> lookup) =>
        lookup($"Step.ResultPropertyDescription.{resultTypeName}.{path}")
        ?? lookup($"Step.ResultPropertyDescription.{path}");

    public static string Description(string resultTypeName, ResultPropertyDescriptor property,
        Func<string, string?> lookup) => Description(resultTypeName, property.Name, lookup)
        ?? property.Description ?? string.Empty;

    public static ResultPropertyDescriptor Localize(string resultTypeName, ResultPropertyDescriptor property,
        Func<string, string?> lookup) => property with
        {
            DisplayName = Name(resultTypeName, property.Name, lookup),
            Description = Description(resultTypeName, property, lookup)
        };
}
