using System.Diagnostics;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class ContextMenuRenderingTests
{
    [Fact]
    public async Task ContextMenus_PreserveSelectionAndRenderBothThemes()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "context-menu-render");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(ContextMenuRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-context-menus"); start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); } catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        Assert.True(File.Exists(Path.Combine(directory, "Light.png")));
        Assert.True(File.Exists(Path.Combine(directory, "Dark.png")));
    }
}
