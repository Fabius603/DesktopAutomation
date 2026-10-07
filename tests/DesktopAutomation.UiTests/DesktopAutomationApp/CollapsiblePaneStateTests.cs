using DesktopAutomationApp.ViewModels;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class CollapsiblePaneStateTests
{
    [Fact]
    public void NarrowLayout_CollapsesAutomaticallyAndRestoresWithoutSavingManualPreference()
    {
        var saved = new List<bool>();
        var pane = new CollapsiblePaneState(900, save: value => { saved.Add(value); return Task.CompletedTask; });
        pane.UpdateWidth(1000);
        Assert.True(pane.IsExpanded);
        pane.UpdateWidth(850);
        Assert.True(pane.IsCollapsed);
        pane.UpdateWidth(910);
        Assert.True(pane.IsCollapsed);
        pane.UpdateWidth(1000);
        Assert.True(pane.IsExpanded);
        Assert.Empty(saved);
    }

    [Fact]
    public void ManualCollapse_SurvivesResizeAndReloadUntilExplicitlyExpanded()
    {
        var saved = false;
        var pane = new CollapsiblePaneState(900, save: value => { saved = value; return Task.CompletedTask; });
        pane.ToggleCommand.Execute(null);
        pane.UpdateWidth(850);
        pane.UpdateWidth(1200);
        Assert.True(pane.IsCollapsed);
        Assert.True(saved);
        var restored = new CollapsiblePaneState(900, saved);
        restored.UpdateWidth(1200);
        Assert.True(restored.IsCollapsed);
        restored.ToggleCommand.Execute(null);
        Assert.True(restored.IsExpanded);
    }

    [Fact]
    public void AutomaticallyCollapsedPane_CanBeOpenedAtTheCurrentWidthAndKeepsThatChoiceWithinTheNarrowBand()
    {
        var pane = new CollapsiblePaneState(900);
        pane.UpdateWidth(850);
        pane.ToggleCommand.Execute(null);
        Assert.True(pane.IsExpanded);
        pane.UpdateWidth(860);
        Assert.True(pane.IsExpanded);
        pane.UpdateWidth(1000);
        pane.UpdateWidth(850);
        Assert.True(pane.IsCollapsed);
    }
}
