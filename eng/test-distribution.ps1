param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$MsixMetadata,
    [string]$VelopackExecutable,
    [string[]]$DependencyPath,
    [string]$OutputDirectory = 'artifacts/distribution-tests'
)
$ErrorActionPreference = 'Stop'
$metadata = Get-Content -LiteralPath $MsixMetadata -Raw | ConvertFrom-Json
if ($metadata.identity -notlike 'DesktopAutomation.LocalTest*') { throw 'Installed tests require an isolated development package identity.' }
if (Get-AppxPackage -Name $metadata.identity) { throw 'Refusing to replace a pre-existing test package.' }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force $outputRoot | Out-Null
$profile = 'deployment-' + [Guid]::NewGuid().ToString('N')
$executables = @((Join-Path (Resolve-Path $PublishDirectory).Path 'DesktopAutomationApp.exe'))
if ($VelopackExecutable) { $executables += (Resolve-Path $VelopackExecutable).Path }
$installed = $false
$processes = [Collections.Generic.List[Diagnostics.Process]]::new()
if (-not ('DesktopAutomationMsixActivation' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DesktopAutomationMsixActivation {
    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivationManager {
        [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string id,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments, uint options, out uint processId);
    }
    public static int Launch(string id, string arguments) {
        var manager = (IActivationManager)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("45ba127d-10a8-46ea-8ab7-56ea9078943c")));
        try { uint pid; Marshal.ThrowExceptionForHR(manager.ActivateApplication(id, arguments, 2, out pid)); return (int)pid; }
        finally { Marshal.FinalReleaseComObject(manager); }
    }
}
'@
}
function Start-Probe([string]$Executable, [string]$Result, [int]$Hold = 0) {
    $arguments = "--profile $profile --deployment-probe `"$Result`" --probe-hold $Hold --background"
    if ($package -and $Executable.StartsWith($package.InstallLocation, [StringComparison]::OrdinalIgnoreCase)) {
        $activatedPid = [DesktopAutomationMsixActivation]::Launch(($package.PackageFamilyName + '!App'), $arguments)
        $process = [Diagnostics.Process]::GetProcessById($activatedPid)
    } else {
        $process = Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    }
    $processes.Add($process)
    return $process
}
function Wait-Probe([Diagnostics.Process]$Process, [string]$Result, [string]$ExpectedKind) {
    if (-not $Process.WaitForExit(30000)) { throw 'Deployment probe timed out.' }
    if ($Process.ExitCode -ne 0) { throw "Deployment probe failed: $Result" }
    $report = Get-Content -LiteralPath $Result -Raw | ConvertFrom-Json
    if ($report.installation -ne $ExpectedKind) { throw "Expected $ExpectedKind, received $($report.installation)." }
    foreach ($check in $report.checks.PSObject.Properties) { if ($check.Value -ne 'Passed') { throw "$($check.Name): $($check.Value)" } }
    return $report
}
try {
    # Windows requires Developer Mode or an appropriately trusted certificate for this registration.
    $dependencyArguments = @{}
    if ($DependencyPath) { $dependencyArguments.DependencyPath = $DependencyPath }
    Add-AppxPackage -Register $metadata.manifestPath @dependencyArguments
    $installed = $true
    $package = Get-AppxPackage -Name $metadata.identity
    $executables += Join-Path $package.InstallLocation 'DesktopAutomationApp.exe'
    $reports = @()
    for ($index = 0; $index -lt $executables.Count; $index++) {
        $result = Join-Path $outputRoot "channel-$index.json"
        $process = Start-Probe $executables[$index] $result
        $kind = if ($index -eq $executables.Count - 1) { 'Msix' } elseif ($index -eq 0) { 'Unpackaged' } else { 'Velopack' }
        $reports += Wait-Probe $process $result $kind
    }
    if (@($reports.localRoot | Select-Object -Unique).Count -ne 1 -or @($reports.roamingRoot | Select-Object -Unique).Count -ne 1) {
        throw 'Distribution channels do not share data paths.'
    }
    $marker = Join-Path $reports[0].roamingRoot 'deployment-marker.txt'
    $secret = Join-Path $reports[0].localRoot 'deployment-secret.bin'
    if (-not (Test-Path $marker) -or -not (Test-Path $secret)) { throw 'Data was virtualized instead of written to the shared profile.' }
    $secretHash = (Get-FileHash -LiteralPath $secret -Algorithm SHA256).Hash
    # Exercise every distribution as owner, including simultaneous second and third launches.
    for ($owner = 0; $owner -lt $executables.Count; $owner++) {
        $ownerResult = Join-Path $outputRoot "owner-$owner.json"
        $ownerProcess = Start-Probe $executables[$owner] $ownerResult 15000
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while (-not (Test-Path $ownerResult) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
        if (-not (Test-Path $ownerResult)) { throw 'Owner did not acquire its profile.' }
        foreach ($other in 0..($executables.Count - 1)) {
            $duplicateResult = Join-Path $outputRoot "duplicate-$owner-$other.json"
            $duplicate = Start-Probe $executables[$other] $duplicateResult
            if (-not $duplicate.WaitForExit(10000) -or $duplicate.ExitCode -ne 0 -or (Test-Path $duplicateResult)) {
                throw 'A second launch was not safely suppressed.'
            }
        }
        # A terminated owner must not leave a stale lock that blocks a different channel.
        $ownerProcess.Kill()
        $ownerProcess.WaitForExit()
        $recoveryResult = Join-Path $outputRoot "recovery-$owner.json"
        $recovery = Start-Probe $executables[0] $recoveryResult
        $null = Wait-Probe $recovery $recoveryResult 'Unpackaged'
    }
    # Use synthetic manifest versions to exercise Windows upgrade/downgrade handling without releasing the app.
    [xml]$originalManifest = Get-Content -LiteralPath $metadata.manifestPath
    $originalVersion = [version]$metadata.version
    if ($originalVersion.Build -ge 65535) { throw 'The test fixture requires an incrementable patch version.' }
    $upgradeVersion = "$($originalVersion.Major).$($originalVersion.Minor).$($originalVersion.Build + 1)"
    $upgradeOutput = Join-Path $outputRoot 'upgrade'
    & (Join-Path $PSScriptRoot 'build-msix.ps1') -PublishDirectory $PublishDirectory -IdentityName $metadata.identity `
        -Publisher $originalManifest.Package.Identity.Publisher -PublisherDisplayName $originalManifest.Package.Properties.PublisherDisplayName `
        -Version $upgradeVersion -OutputDirectory $upgradeOutput
    $upgradeMetadata = Get-Content (Join-Path $upgradeOutput 'package.json') -Raw | ConvertFrom-Json
    $originalFamily = $package.PackageFamilyName
    Add-AppxPackage -Register $upgradeMetadata.manifestPath @dependencyArguments
    $package = Get-AppxPackage -Name $metadata.identity
    if ($package.PackageFamilyName -ne $originalFamily -or $package.Version.ToString() -ne "$upgradeVersion.0") {
        throw 'MSIX upgrade changed identity or did not apply the expected version.'
    }
    $upgradeExecutable = Join-Path $package.InstallLocation 'DesktopAutomationApp.exe'
    $upgradeResult = Join-Path $outputRoot 'after-upgrade.json'
    $null = Wait-Probe (Start-Probe $upgradeExecutable $upgradeResult) $upgradeResult 'Msix'
    $downgradeRejected = $false
    try { Add-AppxPackage -Register $metadata.manifestPath @dependencyArguments }
    catch {
        if ($_.Exception.Message -notmatch '80073D06') { throw }
        $downgradeRejected = $true
    }
    if (-not $downgradeRejected) { throw 'Windows unexpectedly allowed a package downgrade.' }
    if ((Get-FileHash -LiteralPath $secret -Algorithm SHA256).Hash -ne $secretHash) { throw 'Upgrade changed shared credentials.' }
    Remove-AppxPackage -Package $package.PackageFullName
    $installed = $false
    if (-not (Test-Path $marker) -or -not (Test-Path $secret) -or (Get-FileHash -LiteralPath $secret -Algorithm SHA256).Hash -ne $secretHash) {
        throw 'MSIX uninstall removed or changed shared user data.'
    }
    $afterUninstall = Join-Path $outputRoot 'after-msix-uninstall.json'
    $null = Wait-Probe (Start-Probe $executables[0] $afterUninstall) $afterUninstall 'Unpackaged'
    $reports | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $outputRoot 'summary.json') -Encoding UTF8
    Write-Output 'Installed distribution, shared data, native dependencies, collision and crash recovery tests passed.'
}
finally {
    foreach ($process in $processes) {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
    if ($installed) {
        Get-AppxPackage -Name $metadata.identity | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName }
    }
}
