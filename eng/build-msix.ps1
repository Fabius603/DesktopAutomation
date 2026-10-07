param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$IdentityName,
    [Parameter(Mandatory)][string]$Publisher,
    [Parameter(Mandatory)][string]$PublisherDisplayName,
    [string]$Version,
    [string]$OutputDirectory = 'artifacts/msix',
    [string]$CertificateThumbprint,
    [string]$SdkBin
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$publishRoot = (Resolve-Path -LiteralPath $PublishDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $publishRoot 'DesktopAutomationApp.exe'))) { throw 'Publish directory has no application executable.' }
if (-not $Version) {
    [xml]$project = Get-Content (Join-Path $repositoryRoot 'DesktopAutomationApp/DesktopAutomationApp.csproj')
    $Version = [string]($project.Project.PropertyGroup.Version | Select-Object -First 1)
}
if ($Version -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$') { throw 'A stable major.minor.patch release version is required.' }
foreach ($part in @($Matches.major, $Matches.minor, $Matches.patch)) {
    if ([int]$part -gt 65535) { throw 'MSIX version components must not exceed 65535.' }
}
$packageVersion = "$Version.0"
if ($IdentityName -notmatch '^[A-Za-z0-9][A-Za-z0-9.-]{2,49}$') { throw 'Invalid MSIX identity name.' }
if (-not $Publisher.StartsWith('CN=') -or [string]::IsNullOrWhiteSpace($PublisherDisplayName)) { throw 'Publisher identity and display name are required.' }
if (-not $SdkBin) {
    $sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
    $SdkBin = Get-ChildItem $sdkRoot -Directory | Where-Object { $_.Name -match '^10\.0\.\d+\.0$' } |
        Sort-Object { [version]$_.Name } -Descending | ForEach-Object { Join-Path $_.FullName 'x64' } |
        Where-Object { Test-Path (Join-Path $_ 'makeappx.exe') } | Select-Object -First 1
}
if (-not $SdkBin) { throw 'Windows SDK with MakeAppx is required.' }
$outputRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Force $outputRoot | Out-Null
$stage = Join-Path $outputRoot ('stage-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $stage | Out-Null
Get-ChildItem -LiteralPath $publishRoot | Copy-Item -Destination $stage -Recurse
[xml]$manifest = Get-Content (Join-Path $repositoryRoot 'packaging/msix/AppxManifest.xml')
$manifest.Package.Identity.SetAttribute('Name', $IdentityName)
$manifest.Package.Identity.SetAttribute('Publisher', $Publisher)
$manifest.Package.Identity.SetAttribute('Version', $packageVersion)
$manifest.Package.Properties.PublisherDisplayName = $PublisherDisplayName
$manifest.Save((Join-Path $stage 'AppxManifest.xml'))
Add-Type -AssemblyName PresentationCore
$assets = Join-Path $stage 'Assets'
New-Item -ItemType Directory -Force $assets | Out-Null
$iconStream = [IO.File]::OpenRead((Join-Path $repositoryRoot 'DesktopAutomationApp/Assets/App.ico'))
try {
    $decoder = [Windows.Media.Imaging.BitmapDecoder]::Create($iconStream, [Windows.Media.Imaging.BitmapCreateOptions]::None, [Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    $source = $decoder.Frames | Sort-Object PixelWidth -Descending | Select-Object -First 1
    foreach ($asset in @(@('StoreLogo.png',50), @('Square44x44Logo.png',44), @('Square150x150Logo.png',150))) {
        $scale = [Windows.Media.ScaleTransform]::new($asset[1] / $source.PixelWidth, $asset[1] / $source.PixelHeight)
        $bitmap = [Windows.Media.Imaging.TransformedBitmap]::new($source, $scale)
        $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
        $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
        $outputStream = [IO.File]::Create((Join-Path $assets $asset[0]))
        try { $encoder.Save($outputStream) } finally { $outputStream.Dispose() }
    }
} finally { $iconStream.Dispose() }
$packagePath = Join-Path $outputRoot "DesktopAutomation-$packageVersion-x64.msix"
if (Test-Path -LiteralPath $packagePath) { throw 'Refusing to replace an existing package. Use a new version or output directory.' }
& (Join-Path $SdkBin 'makeappx.exe') pack /d $stage /p $packagePath /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed with exit code $LASTEXITCODE." }
if ($CertificateThumbprint) {
    & (Join-Path $SdkBin 'signtool.exe') sign /fd SHA256 /sha1 $CertificateThumbprint $packagePath
    if ($LASTEXITCODE -ne 0) { throw 'MSIX signing failed.' }
}
[pscustomobject]@{ packagePath = $packagePath; manifestPath = (Join-Path $stage 'AppxManifest.xml'); version = $packageVersion; identity = $IdentityName } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputRoot 'package.json') -Encoding UTF8
