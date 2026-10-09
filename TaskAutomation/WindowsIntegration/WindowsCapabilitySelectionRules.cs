namespace TaskAutomation.WindowsIntegration;

public static class WindowsCapabilitySelectionRules
{
    public static bool HasRequiredParameters(
        WindowsCapabilityDescriptor? capability,
        IReadOnlyDictionary<string, string?> values)
        => capability is not null
           && (capability.Parameters ?? []).All(parameter =>
               !parameter.Required
               || !string.IsNullOrWhiteSpace(Value(values, parameter.Name)));

    public static Dictionary<string, string?> WithParameterDefaults(
        WindowsCapabilityDescriptor? capability,
        IReadOnlyDictionary<string, string?> values)
    {
        var effective = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in capability?.Parameters ?? [])
            if ((!effective.TryGetValue(parameter.Name, out var value) || value is null) && parameter.DefaultValue is not null)
                effective[parameter.Name] = parameter.DefaultValue;
        return effective;
    }

    private static string? Value(IReadOnlyDictionary<string, string?> values, string name)
    {
        if (values.TryGetValue(name, out var value))
            return value;

        return values.FirstOrDefault(entry =>
            entry.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
    }
}
