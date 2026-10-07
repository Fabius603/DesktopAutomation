using DesktopAutomation.Application.Deployment;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services;
using DesktopAutomationApp.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace TaskAutomation.Tests.DesktopAutomationApp;

[Collection(DesktopRenderingCollection.Name)]
public sealed class DistributionSettingsTests
{
    [Fact]
    public async Task Settings_RenderExternalUpdateStatusAndDisabledStartupInBothLanguages()
    {
        var directory = Path.Combine(StepListRenderHost.RepositoryRoot(), "artifacts", "verify", "distribution-render");
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(DistributionSettingsTests).Assembly.Location);
        start.ArgumentList.Add("--render-distribution");
        start.ArgumentList.Add(directory);
        using var process = System.Diagnostics.Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
    }
    [Theory]
    [InlineData(InstallationKind.Msix, "Settings.Updates.MsixManaged")]
    [InlineData(InstallationKind.Unpackaged, "Settings.Updates.LocalBuild")]
    public void ExternallyManagedUpdates_DisableChecksAndDescribeActualUpdateSource(InstallationKind kind, string key)
    {
        var service = new UpdateService(NullLogger<UpdateService>.Instance, new InstallationContext(kind, "1.6.0"));
        var viewModel = new UpdateSettingsViewModel(new Notes(), service, NullLogger<UpdateSettingsViewModel>.Instance);
        Assert.False(viewModel.CheckForUpdatesCommand.CanExecute(null));
        Assert.Equal(Loc.Get(key), viewModel.UpdateCheckStatus);
        viewModel.CheckForUpdatesCommand.Execute(null);
        Assert.Equal(Loc.Get(key), viewModel.UpdateCheckStatus);
        Assert.True(viewModel.ShowReleaseNotesCommand.CanExecute(null));
    }

    private sealed class Notes : IReleaseNotesService
    {
        public Task ShowIfNewAsync() => Task.CompletedTask;
        public Task ShowAllAsync() => Task.CompletedTask;
    }
}
