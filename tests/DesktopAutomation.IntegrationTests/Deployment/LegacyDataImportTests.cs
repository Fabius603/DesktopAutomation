using Common.ApplicationData;

namespace DesktopAutomation.IntegrationTests.Deployment;

public sealed class LegacyDataImportTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DesktopAutomation-import-" + Guid.NewGuid().ToString("N"));
    private string Source => Path.Combine(_root, "old");
    private string Destination => Path.Combine(_root, "new");

    [Fact]
    public void Import_PreservesOriginalsExistingTargetsAndEncryptedBytes()
    {
        Directory.CreateDirectory(Source);
        Directory.CreateDirectory(Destination);
        File.WriteAllText(Path.Combine(Source, "job.json"), "old");
        File.WriteAllText(Path.Combine(Destination, "job.json"), "edited");
        var secret = new byte[] { 0, 7, 255, 42 };
        File.WriteAllBytes(Path.Combine(Source, "secret.bin"), secret);
        Assert.True(LegacyDataImport.CopyOnce(Destination, Source));
        Assert.Equal("edited", File.ReadAllText(Path.Combine(Destination, "job.json")));
        Assert.Equal("old", File.ReadAllText(Path.Combine(Source, "job.json")));
        Assert.Equal(secret, File.ReadAllBytes(Path.Combine(Destination, "secret.bin")));
        Assert.Equal(secret, File.ReadAllBytes(Path.Combine(Source, "secret.bin")));
    }

    [Fact]
    public void CompletedImport_DoesNotResurrectDeletedJobs()
    {
        Directory.CreateDirectory(Source);
        File.WriteAllText(Path.Combine(Source, "job.json"), "old");
        Assert.True(LegacyDataImport.CopyOnce(Destination, Source));
        File.Delete(Path.Combine(Destination, "job.json"));
        Assert.True(LegacyDataImport.CopyOnce(Destination, Source));
        Assert.False(File.Exists(Path.Combine(Destination, "job.json")));
    }

    [Fact]
    public void InterruptedImport_CanRetryAndDoesNotExposePartialFiles()
    {
        Directory.CreateDirectory(Source);
        var path = Path.Combine(Source, "settings.json");
        File.WriteAllText(path, "complete");
        using (var blocked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.False(LegacyDataImport.CopyOnce(Destination, Source));
            Assert.False(File.Exists(Path.Combine(Destination, "settings.json")));
            Assert.Empty(Directory.GetFiles(Destination));
        }
        Assert.True(LegacyDataImport.CopyOnce(Destination, Source));
        Assert.Equal("complete", File.ReadAllText(Path.Combine(Destination, "settings.json")));
    }

    [Fact]
    public void SourcePriorityAndLeaseIsolation_ArePreserved()
    {
        Directory.CreateDirectory(Source);
        var older = Path.Combine(_root, "older");
        Directory.CreateDirectory(older);
        File.WriteAllText(Path.Combine(Source, "settings.json"), "current");
        File.WriteAllText(Path.Combine(older, "settings.json"), "obsolete");
        File.WriteAllText(Path.Combine(Source, ".instance.lock"), "old lock");
        Assert.True(LegacyDataImport.CopyOnce(Destination, Source, older));
        Assert.Equal("current", File.ReadAllText(Path.Combine(Destination, "settings.json")));
        Assert.False(File.Exists(Path.Combine(Destination, ".instance.lock")));
    }

    [Fact]
    public void SharedPaths_AreOutsideAppDataAndPackageInstallation()
    {
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DesktopAutomation");
        Assert.StartsWith(expected + Path.DirectorySeparatorChar, AppPaths.LocalRoot);
        Assert.StartsWith(expected + Path.DirectorySeparatorChar, AppPaths.RoamingRoot);
        Assert.DoesNotContain("AppData", Path.GetRelativePath(expected, AppPaths.LocalRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
