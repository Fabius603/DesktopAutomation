using System.Text.Json;

namespace TaskAutomation.Automations;

public static class AutomationCopies
{
    public static AutomationDefinition Clone(AutomationDefinition source, string name)
    {
        var copy = JsonSerializer.Deserialize<AutomationDefinition>(JsonSerializer.Serialize(source))
            ?? throw new JsonException("Automation copy could not be materialized.");
        copy.Id = Guid.NewGuid();
        copy.Name = name;
        copy.Active = false;
        copy.LastRunAt = null;
        return copy;
    }
}
