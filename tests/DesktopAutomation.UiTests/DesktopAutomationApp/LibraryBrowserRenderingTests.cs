using System.Diagnostics;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class LibraryBrowserRenderingTests
{
    [Fact]
    public async Task SharedLibraryRendersFoldersDescriptionsAndNavigationForEveryKind()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "library-ui-render");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(LibraryBrowserRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-library"); start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try { await process.WaitForExitAsync(timeout.Token); } catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        foreach (var name in new[] { "Job", "Makro", "Automation", "compact", "light-en" }) Assert.True(File.Exists(Path.Combine(directory, name + ".png")));
    }
}
