using DesktopAutomation.Application.Deployment;
using System.Runtime.InteropServices;
using System.Text;
using Velopack.Locators;
using Windows.ApplicationModel;

namespace DesktopAutomationApp.Infrastructure;

public static class InstallationDetector
{
    public static string? GetPackageFamilyName()
    {
        uint length = 0;
        var result = GetCurrentPackageFamilyName(ref length, null);
        if (result == 15700) return null; // APPMODEL_ERROR_NO_PACKAGE
        if (result != 122) throw new System.ComponentModel.Win32Exception(result);
        var buffer = new StringBuilder((int)length);
        result = GetCurrentPackageFamilyName(ref length, buffer);
        if (result != 0) throw new System.ComponentModel.Win32Exception(result);
        return buffer.ToString();
    }

    public static InstallationContext Detect(string? packageFamilyName)
    {
        var version = typeof(InstallationDetector).Assembly.GetName().Version!;
        if (packageFamilyName is not null)
        {
            var packageVersion = Package.Current.Id.Version;
            return InstallationContext.Resolve($"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}", packageFamilyName, false);
        }
        var installedVersion = VelopackLocator.Current.CurrentlyInstalledVersion;
        return InstallationContext.Resolve(installedVersion?.ToString() ?? $"{version.Major}.{version.Minor}.{version.Build}", null,
            installedVersion is not null);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFamilyName(ref uint length, StringBuilder? name);
}
