using System.Diagnostics;
using System.Text.Json;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class StepListRenderingTests
{
    [Fact]
    public async Task RenderedCards_KeepBreakpointsSeparateAndExposeOnlyIconNameAndSummary()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "ui-render");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(StepListRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-step-list");
        start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        var reports = JsonSerializer.Deserialize<List<StepListRenderReport>>(File.ReadAllText(Path.Combine(directory, "layout.json")))!;
        Assert.Equal(12, reports.Count);
        Assert.Contains("Picker interactions verified", await stdout);
        Assert.Contains("Step and variable dialog interactions verified", await stdout);
        foreach (var image in new[] { "add-step-dialog.png", "add-step-empty.png", "add-step-compact.png", "job-variables-dialog.png", "job-variables-compact.png" })
            Assert.True(File.Exists(Path.Combine(directory, image)));
        Assert.All(reports, report =>
        {
            Assert.Equal(0, report.InspectorBreakpointToggles);
            Assert.Equal(0, report.InspectorEnabledToggles);
            Assert.True(report.IconOnlyDragHandle);
            Assert.True(report.CollapseAccessible);
            Assert.True(report.SourceGhostVisible);
            Assert.True(report.PreviewStable);
            Assert.True(report.PreviewRestored);
            Assert.NotEmpty(report.Cards);
            Assert.All(report.Cards, card =>
            {
                Assert.True(card.NumberRight <= card.BreakpointLeft, "Step number overlaps its breakpoint hit target.");
                Assert.True(card.Right < 803, "Ordinary nested cards must fit the list viewport without accidental horizontal overflow.");
                Assert.Equal((card.Conditional ? 2 : 0) + (card.Foldable ? 1 : 0), card.ButtonCount);
                Assert.Equal(card.Conditional ? 4 : 2, card.TextCount);
                Assert.Equal(card.Foldable ? 2 : 1, card.IconCount);
            });
        });
        Assert.All(reports, report => Assert.True(File.Exists(Path.Combine(directory, report.Image))));
        Assert.True(Assert.Single(reports, report => report.Image == "drag-snapped.png").Snapped);
        Assert.False(Assert.Single(reports, report => report.Image == "drag-free.png").Snapped);
        Assert.Contains(reports, report => report.Image == "moved-block.png");
        var nested = reports.Single(report => report.Image == "nested.png");
        var outer = nested.Cards.Single(card => card.Conditional && card.Depth == 0);
        var inner = nested.Cards.Single(card => card.Conditional && card.Depth == 1);
        Assert.True(inner.Left > outer.Left);
        Assert.True(inner.Right < outer.Right, "Nested surfaces must have a visible inset on both sides.");
    }
}

public sealed record StepCardRenderReport(double NumberRight, double BreakpointLeft, int ButtonCount, int TextCount, int IconCount,
    int Depth, double Left, double Right, bool Conditional, bool Foldable);
public sealed record StepListRenderReport(string Image, int InspectorBreakpointToggles, List<StepCardRenderReport> Cards,
    int InspectorEnabledToggles, bool IconOnlyDragHandle, bool Snapped, bool SourceGhostVisible, bool PreviewStable, bool PreviewRestored, bool CollapseAccessible);
