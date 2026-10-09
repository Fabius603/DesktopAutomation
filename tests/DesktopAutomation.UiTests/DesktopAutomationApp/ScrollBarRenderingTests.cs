using System.Diagnostics;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class ScrollBarRenderingTests
{
    [Fact]
    public async Task Scrollbars_EmphasizeFocusAndCaptureWithoutChangingTheViewportInBothOrientationsAndThemes()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "scrollbars");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(ScrollBarRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-scrollbars");
        start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
    }
}
