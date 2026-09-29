using DesktopAutomationApp.Services;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class InstalledProgramDiscoveryTests
{
    [Fact]
    public void FileEnumeration_SkipsInaccessibleProgramDirectories()
    {
        Assert.True(InstalledProgramDiscovery.TopDirectoryEnumeration.IgnoreInaccessible);
        Assert.False(InstalledProgramDiscovery.TopDirectoryEnumeration.RecurseSubdirectories);
        Assert.True(InstalledProgramDiscovery.RecursiveEnumeration.IgnoreInaccessible);
        Assert.True(InstalledProgramDiscovery.RecursiveEnumeration.RecurseSubdirectories);
    }
}
