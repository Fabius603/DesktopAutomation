namespace DesktopAutomation.Application.Deployment;

public enum InstallationKind { Unpackaged, Velopack, Msix }

/// <summary>The single owner of distribution capabilities, independent of the frontend.</summary>
public sealed record InstallationContext(InstallationKind Kind, string Version, string? PackageFamilyName = null)
{
    public bool CanUpdateInApp => Kind == InstallationKind.Velopack;
    public bool CanRegisterStartup => Kind != InstallationKind.Unpackaged;

    // Package identity takes precedence over any leftover installer metadata.
    public static InstallationContext Resolve(string version, string? packageFamilyName, bool velopackInstalled) =>
        new(!string.IsNullOrEmpty(packageFamilyName) ? InstallationKind.Msix :
            velopackInstalled ? InstallationKind.Velopack : InstallationKind.Unpackaged, version, packageFamilyName);
}
