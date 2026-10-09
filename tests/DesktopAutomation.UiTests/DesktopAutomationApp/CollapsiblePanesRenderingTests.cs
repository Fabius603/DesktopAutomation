using System.Diagnostics;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class CollapsiblePanesRenderingTests
{
    [Fact]
    public async Task NavigationAndInspector_CollapseManuallyAndResponsivelyWithAccessibleControlsInBothLanguagesAndThemes()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "collapsible-panes");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(CollapsiblePanesRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-collapsible-panes");
        start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        foreach (var culture in new[] { "de-DE", "en-US" })
            foreach (var theme in new[] { "Black", "Dark", "Light" })
                foreach (var state in new[] { "expanded", "collapsed" })
                    Assert.True(File.Exists(Path.Combine(directory, $"{culture}-{theme}-{state}.png")));
    }
}
