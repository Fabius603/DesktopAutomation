# Distribution and installed application tests

DesktopAutomation supports Velopack direct downloads, Store MSIX and local source builds.
The canonical policy is `DesktopAutomation.Application/Deployment/InstallationContext.cs`.
`AppPaths` owns all persisted locations; `ProfileLease` owns exclusive access before any app
services run. See the [distribution decision](../decisions/2026-10-06-support-velopack-and-msix.md).

## Build

Use Windows with the SDK pinned in `global.json` and Windows SDK MakeAppx:

```powershell
.\eng\verify.ps1 -Mode Full
.\eng\publish-app.ps1 -OutputDirectory artifacts/distribution/publish
.\eng\build-msix.ps1 -PublishDirectory artifacts/distribution/publish `
  -IdentityName DesktopAutomation.LocalTest -Publisher CN=DesktopAutomation.LocalTest `
  -PublisherDisplayName DesktopAutomation
```

The output is self-contained and multi-file so native Tesseract libraries retain their assembly
relative paths. Both packagers receive that same directory. `build-msix.ps1` rejects unsupported
versions and refuses to replace an existing MSIX. Packages use `major.minor.patch.0`.

Native OCR/image libraries also require the Visual C++ runtime: MSIX declares the Microsoft
VCLibs UWPDesktop framework (minimum 14.0.33728.0), supplied by the Store. Velopack bootstraps
`vcredist143-x64` if necessary. Source builds need the Visual C++ 2022 x64 redistributable installed.
For offline MSIX tests, pass the corresponding signed framework package via `-DependencyPath`.

The release workflow produces a development MSIX until all three GitHub repository variables
`MSIX_IDENTITY_NAME`, `MSIX_PUBLISHER`, `MSIX_PUBLISHER_DISPLAY_NAME` are set from Partner Center.
Partial configuration fails. A development identity is never a valid Store product identity.
Upload the correctly identified MSIX manually through Partner Center; store publishing is not
performed by this workflow. Explain `runFullTrust` and `unvirtualizedResources` during certification.
For sideloading, pass `-CertificateThumbprint` for a certificate whose subject matches Publisher.
The Store supplies production package signing. Set the optional `VELOPACK_SIGN_PARAMS` GitHub secret for Velopack code signing; the referenced certificate must be available to the runner. The manual `Package MSIX` workflow also builds packages without creating a GitHub release.

## Profiles and coexistence

Default data stays under `%APPDATA%\DesktopAutomation` and `%LOCALAPPDATA%\DesktopAutomation`.
Use `DesktopAutomationApp.exe --profile development` for all three channels to select isolated
data under `Profiles\development`. Names are limited to 64 ASCII letters, digits, underscores
and hyphens, excluding Windows device names. Non-default profiles never change autostart.

All current channels share an exclusive file lease. A manual duplicate requests foreground
activation; an autostart/background duplicate simply exits. File ownership works across Windows
sessions; activation is limited to the current interactive session. Crashes release the handle.
MSIX explicitly disables write virtualization to preserve jobs, credentials, logs and Windows
settings across channels and uninstall. Settings, user files and installer files remain separate.

`.profile-format.json` is additive. Unsupported formats are refused before loading/migration.
Advance its version for a future incompatible persistence change. Older releases without this
guard cannot coordinate; upgrade them before using the shared profile concurrently.

MSIX StartupTask carries `--startup`; Velopack registers its stable launcher with the same argument.
Startup follows shared preferences and only one process executes automations. Enabling MSIX
startup removes the recognized legacy Run entry; user-disabled StartupTasks remain disabled.
Uninstalling a channel does not automatically select the other channel for startup: enable
startup explicitly in the remaining installed app. Local builds never register a build path.

## Actual installed tests

On an interactive Windows test machine, enable Developer Mode or trust the package's signing
certificate. No script enables machine policy or imports a trust certificate implicitly.

```powershell
.\eng\test-distribution.ps1 -PublishDirectory artifacts/distribution/publish `
  -MsixMetadata artifacts/msix/package.json `
  -VelopackExecutable "$env:LOCALAPPDATA\DesktopAutomation.DeploymentTest\current\DesktopAutomationApp.exe"
```

The optional Velopack executable must be an installed test copy of the same payload. Package it
with a distinct pack ID `DesktopAutomation.DeploymentTest`, a distinct pack title and no shortcuts,
then install silently. Never replace an existing personal installation to run these tests.
The script refuses pre-existing MSIX test registrations, uses a unique data profile and unregisters
only the package it registered. Probe output remains in artifacts; test profiles can be removed
after review. It exercises shared data, DPAPI across channels, native OCR, OpenCV, desktop capture,
process starts, a temporary hotkey, StartupTask manifest, update isolation, every owner/duplicate
combination, crash recovery and data access after MSIX uninstall.
The script also tests a synthetic manifest version upgrade and rejection of a package downgrade.

Run the repository verification gate separately. Store flight updates, real login/autostart,
untrusted certificate behavior, enterprise policies and elevated/secure-desktop automation require
their own Windows test environments. Successful local packaging is not Store certification.
