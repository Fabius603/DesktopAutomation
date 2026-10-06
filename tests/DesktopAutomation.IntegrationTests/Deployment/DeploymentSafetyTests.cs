using DesktopAutomation.Application.Deployment;
using DesktopAutomationApp.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesktopAutomation.IntegrationTests.Deployment;

public sealed class DeploymentSafetyTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DesktopAutomation-deployment-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void SharedProfile_RejectsSecondOwnerAndCanBeReopenedAfterShutdown()
    {
        using (var first = ProfileLease.TryAcquire(_directory))
        {
            Assert.NotNull(first);
            Assert.Null(ProfileLease.TryAcquire(_directory));
            using var separate = ProfileLease.TryAcquire(_directory + "-other");
            Assert.NotNull(separate);
        }
        using var next = ProfileLease.TryAcquire(_directory);
        Assert.NotNull(next);
    }

    [Fact]
    public void ExistingLeaseFile_DoesNotPreventCrashRecovery()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, ".instance.lock"), "stale");
        using var lease = ProfileLease.TryAcquire(_directory);
        Assert.NotNull(lease);
    }

    [Fact]
    public async Task RapidSettingsChanges_ArePersistedWithoutTemporaryFileCollisions()
    {
        Directory.CreateDirectory(_directory);
        var service = new DesktopAutomation.Application.Settings.UserPreferencesService(Path.Combine(_directory, "settings.json"));
        var writes = new List<Task>();
        for (var index = 0; index < 30; index++)
        {
            service.Current.StartWithWindows = index % 2 == 0;
            writes.Add(service.SaveAsync());
        }
        await Task.WhenAll(writes);
        var reader = new DesktopAutomation.Application.Settings.UserPreferencesService(Path.Combine(_directory, "settings.json"));
        await reader.LoadAsync();
        Assert.False(reader.Current.StartWithWindows);
        Assert.False(File.Exists(Path.Combine(_directory, "settings.json.tmp")));
    }

    [Theory]
    [InlineData("{\"format\":2}")]
    [InlineData("{\"format\":0}")]
    [InlineData("{\"format\":\"1\"}")]
    [InlineData("{}")]
    public void UnsupportedProfile_IsNotOverwritten(string content)
    {
        using var lease = ProfileLease.TryAcquire(_directory);
        var path = Path.Combine(_directory, ".profile-format.json");
        File.WriteAllText(path, content);
        Assert.False(ProfileCompatibility.EnsureSupported(_directory));
        Assert.Equal(content, File.ReadAllText(path));
    }

    [Fact]
    public void ExistingProfile_IsCompatibleWithoutMovingUserFiles()
    {
        using var lease = ProfileLease.TryAcquire(_directory);
        var job = Path.Combine(_directory, "job.json");
        File.WriteAllText(job, "{\"Id\":\"existing\"}");
        Assert.True(ProfileCompatibility.EnsureSupported(_directory));
        Assert.True(ProfileCompatibility.EnsureSupported(_directory));
        Assert.Equal("{\"Id\":\"existing\"}", File.ReadAllText(job));
    }

    [Theory]
    [InlineData(InstallationKind.Msix)]
    [InlineData(InstallationKind.Unpackaged)]
    public async Task NonVelopackStart_NeverOffersDownloadsOrRestarts(InstallationKind kind)
    {
        var service = new UpdateService(NullLogger<UpdateService>.Instance, new InstallationContext(kind, "1.6.0"));
        var observed = new List<UpdateCheckResult>();
        service.UpdateChecked += observed.Add;
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => service.CheckForUpdateAsync()));
        Assert.All(results, result => { Assert.False(result.HasUpdate); Assert.Equal("1.6.0", result.CurrentVersion); });
        Assert.Equal(8, observed.Count);
        Assert.False(await service.DownloadUpdateAsync());
        Assert.False(service.PrepareUpdateAndRestart());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        if (Directory.Exists(_directory + "-other")) Directory.Delete(_directory + "-other", true);
    }
}
