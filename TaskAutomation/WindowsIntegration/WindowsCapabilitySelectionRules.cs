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

    private static string? Value(IReadOnlyDictionary<string, string?> values, string name)
    {
        if (values.TryGetValue(name, out var value))
            return value;

        return values.FirstOrDefault(entry =>
            entry.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
    }
}
