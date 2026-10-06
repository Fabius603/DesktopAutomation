using System.Diagnostics;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class LogScreenRenderingTests
{
    [Fact]
    public async Task LogWorkspace_RendersAllViewsAndSupportsFiltersCausalNavigationLivePauseAndExport()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "log-ui-render");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(LogScreenRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-logs"); start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try { await process.WaitForExitAsync(timeout.Token); } catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        foreach (var name in new[] { "overview", "run", "automation", "application", "compact", "light", "empty", "aggregate" }) Assert.True(File.Exists(Path.Combine(directory, name + ".png")));
    }
}
