using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobEndSettingsTests
{
    [Fact]
    public void EndPhaseTimeout_UsesImmediateBoundIntegerInput()
    {
        var document = XDocument.Load(Path.Combine(
            RepositoryRoot(), "DesktopAutomationApp", "Views", "JobsView", "JobStepsView.xaml"));
        XNamespace controls = "http://metro.mahapps.com/winfx/xaml/controls";

        var input = Assert.Single(
            document.Descendants(controls + "NumericUpDown"),
            element => element.Attributes().Any(attribute =>
                attribute.Value.Contains("EndPhaseTimeoutSeconds", StringComparison.Ordinal)));

        Assert.Equal("Numbers", input.Attribute("NumericInputMode")?.Value);
        Assert.Equal("1", input.Attribute("Interval")?.Value);
        Assert.Contains("Mode=TwoWay", input.Attribute("Value")?.Value, StringComparison.Ordinal);
        Assert.Contains(
            "UpdateSourceTrigger=PropertyChanged",
            input.Attribute("Value")?.Value,
            StringComparison.Ordinal);
        Assert.Equal("1", input.Attribute("Minimum")?.Value);
        Assert.Equal("3600", input.Attribute("Maximum")?.Value);
    }

    private static string RepositoryRoot([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!, "..", "..", ".."));
}
