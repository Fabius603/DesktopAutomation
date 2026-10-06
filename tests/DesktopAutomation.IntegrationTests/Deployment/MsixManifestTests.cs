using System.Xml.Linq;

namespace DesktopAutomation.IntegrationTests.Deployment;

public sealed class MsixManifestTests
{
    [Fact]
    public void PackageContract_PreservesSharedDataAndRoutesStartupThroughTheApplication()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DesktopAutomation.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var manifest = XDocument.Load(Path.Combine(directory.FullName, "packaging", "msix", "AppxManifest.xml"));
        XNamespace desktop6 = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/6";
        XNamespace uap10 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/10";
        Assert.Equal("disabled", manifest.Descendants(desktop6 + "FileSystemWriteVirtualization").Single().Value);
        Assert.Equal("disabled", manifest.Descendants(desktop6 + "RegistryWriteVirtualization").Single().Value);
        var capabilities = manifest.Descendants().Where(element => element.Name.LocalName == "Capability").Select(element => (string?)element.Attribute("Name"));
        Assert.Contains("unvirtualizedResources", capabilities);
        Assert.Contains("runFullTrust", capabilities);
        var runtime = manifest.Descendants().Single(element => element.Name.LocalName == "PackageDependency");
        Assert.Equal("Microsoft.VCLibs.140.00.UWPDesktop", (string?)runtime.Attribute("Name"));
        Assert.Equal("14.0.33728.0", (string?)runtime.Attribute("MinVersion"));
        var startup = manifest.Descendants().Single(element => (string?)element.Attribute("Category") == "windows.startupTask");
        Assert.Equal("--startup", (string?)startup.Attribute(uap10 + "Parameters"));
        Assert.Equal("DesktopAutomationApp.exe", (string?)startup.Attribute("Executable"));
        var task = startup.Elements().Single();
        Assert.Equal("false", (string?)task.Attribute("Enabled"));
        Assert.Equal(global::DesktopAutomationApp.Settings.WindowsStartupRegistrationService.StartupTaskId, (string?)task.Attribute("TaskId"));
        Assert.Equal("10.0.19041.0", (string?)manifest.Descendants().Single(element => element.Name.LocalName == "TargetDeviceFamily").Attribute("MinVersion"));
    }
}
