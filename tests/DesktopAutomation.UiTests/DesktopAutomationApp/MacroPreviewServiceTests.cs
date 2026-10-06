using System.Collections.ObjectModel;
using System.Drawing;
using DesktopAutomationApp.Services.Preview;
using TaskAutomation.Makros;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class MacroPreviewServiceTests
{
    [Fact]
    public void Preview_IncludesTextCombinationAndWheelWithoutExecutingInput()
    {
        var macro = new Makro
        {
            Name = "preview",
            Befehle = new ObservableCollection<MakroBefehl>([
            new TextInputBefehl { Text = "Grüße", DelayBeforeMicroseconds = 100_000, DurationMicroseconds = 200_000 },
            new KeyCombinationBefehl { Keys = ["Ctrl", "S"], DelayBeforeMicroseconds = 50_000, DurationMicroseconds = 150_000 },
            new MouseWheelBefehl { DeltaY = 120, DelayBeforeMicroseconds = 500_000 }])
        };
        var result = new MacroPreviewService().Build(macro, new Rectangle(0, 0, 1920, 1080), new Rectangle(0, 0, 1920, 1080));
        Assert.Equal(3, result.TimedItems.Count());
        Assert.Equal(MakroTimeline.GetTotalDurationMicroseconds(macro.Befehle) / 1_000_000d, result.TotalSeconds, 6);
    }
}
