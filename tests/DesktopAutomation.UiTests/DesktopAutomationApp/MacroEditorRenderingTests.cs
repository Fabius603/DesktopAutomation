using System.Diagnostics;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class MacroEditorRenderingTests
{
    [Fact]
    public async Task MacroEditor_RendersThemesViewportsAndValidatesRealInspectorBindings()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "macro-ui-render");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(MacroEditorRenderingTests).Assembly.Location);
        start.ArgumentList.Add("--render-macro-editor"); start.ArgumentList.Add(directory);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try { await process.WaitForExitAsync(timeout.Token); } catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        foreach (var name in new[] { "light", "combination", "multiple", "invalid", "invalid-number", "compact", "dark", "english", "four-digit", "recording-light", "recording-dark-compact", "recording-inputs", "recording-precision", "recording-english" })
            Assert.True(File.Exists(Path.Combine(directory, name + ".png")));
    }
}
