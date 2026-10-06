using Common.ApplicationData;
using DesktopAutomation.Application.Deployment;
using Xunit;

namespace DesktopAutomation.UnitTests.Deployment;

public sealed class InstallationContextTests
{
    [Theory]
    [InlineData(null, false, InstallationKind.Unpackaged, false, false)]
    [InlineData(null, true, InstallationKind.Velopack, true, true)]
    [InlineData("Test_family", false, InstallationKind.Msix, false, true)]
    [InlineData("Test_family", true, InstallationKind.Msix, false, true)]
    public void DistributionCapabilities_PackageIdentityWinsOverInstallerMetadata(string? package,
        bool installed, InstallationKind expected, bool updates, bool startup)
    {
        var context = InstallationContext.Resolve("1.6.0", package, installed);
        Assert.Equal(expected, context.Kind);
        Assert.Equal(updates, context.CanUpdateInApp);
        Assert.Equal(startup, context.CanRegisterStartup);
        Assert.Equal("1.6.0", context.Version);
    }

    [Theory]
    [InlineData("../shared")]
    [InlineData("C:\\Users")]
    [InlineData("a/b")]
    [InlineData(".")]
    [InlineData("a b")]
    [InlineData("CON")]
    [InlineData("com1")]
    public void ProfileName_PathTraversalAndAmbiguousNamesAreRejected(string name) =>
        Assert.Throws<ArgumentException>(() => ProfileNames.Validate(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("development-1")]
    [InlineData("Test_profile")]
    public void ProfileName_DefaultAndIsolatedNamesAreAccepted(string? name) =>
        Assert.Equal(string.IsNullOrEmpty(name) ? null : name, ProfileNames.Validate(name));
}
