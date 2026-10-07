using System.Diagnostics;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class StartPageRenderingTests
{
    [Fact]
    public async Task MinimumWindowShowsAllDashboardSectionsWithoutOuterScrollingInBothLanguages()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "start-page-render");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(StartPageRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-start-page");
        start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        foreach (var culture in new[] { "de-DE", "en-US" })
            Assert.True(File.Exists(Path.Combine(directory, culture + ".png")));
    }
}
