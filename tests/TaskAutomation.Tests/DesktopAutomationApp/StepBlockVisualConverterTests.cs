using System.Globalization;
using System.Windows;
using DesktopAutomationApp.Converters;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class StepBlockVisualConverterTests
{
    [Fact]
    public void PreviewProjection_UsesTheFutureControlFlowPositionForCardGeometry()
    {
        var moved = new TimeoutStep();
        var conditional = new IfStep();
        var body = new TimeoutStep();
        var endIf = new EndIfStep();
        var source = new List<JobStep> { moved, conditional, body, endIf };
        var preview = new List<JobStep> { conditional, moved, body, endIf };
        var converter = new StepBlockVisualConverter();

        var originalMargin = Assert.IsType<Thickness>(converter.Convert(
            [moved, null!, source, 0],
            typeof(Thickness),
            "cardMargin",
            CultureInfo.InvariantCulture));
        var previewMargin = Assert.IsType<Thickness>(converter.Convert(
            [moved, preview, source, 0],
            typeof(Thickness),
            "cardMargin",
            CultureInfo.InvariantCulture));

        Assert.Equal(0, originalMargin.Left);
        Assert.Equal(20, previewMargin.Left);
    }
}
