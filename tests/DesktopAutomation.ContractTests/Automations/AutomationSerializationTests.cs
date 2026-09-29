using System.Text.Json;
using TaskAutomation.Automations;

namespace TaskAutomation.Tests.Automations;

public sealed class AutomationSerializationTests
{
    [Fact]
    public void DeserializeLegacyAutomationWithoutFormatVersion_UsesCurrentVersion()
    {
        var automation = Assert.IsType<AutomationDefinition>(
            JsonSerializer.Deserialize<AutomationDefinition>("""{"name":"Legacy"}"""));

        Assert.Equal(AutomationDefinition.CurrentFormatVersion, automation.FormatVersion);
    }
}
