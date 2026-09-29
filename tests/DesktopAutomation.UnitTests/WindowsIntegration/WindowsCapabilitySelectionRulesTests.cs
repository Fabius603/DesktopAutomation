using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.WindowsIntegration;

public sealed class WindowsCapabilitySelectionRulesTests
{
    [Fact]
    public void HasRequiredParameters_MatchesParameterNamesCaseInsensitively()
    {
        var capability = new WindowsCapabilityCatalog().Find("filesystem.path");
        var values = new Dictionary<string, string?> { ["PATH"] = @"C:\Temp" };

        Assert.True(WindowsCapabilitySelectionRules.HasRequiredParameters(capability, values));
    }

    [Fact]
    public void HasRequiredParameters_RejectsWhitespaceRequiredValue()
    {
        var capability = new WindowsCapabilityCatalog().Find("filesystem.path");
        var values = new Dictionary<string, string?> { ["path"] = " " };

        Assert.False(WindowsCapabilitySelectionRules.HasRequiredParameters(capability, values));
    }
}
