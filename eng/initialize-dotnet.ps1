param([Parameter(Mandatory)][string]$RepositoryRoot)

# A user installation is not necessarily ahead of Program Files in PATH.
# Prefer it when it contains the SDK pinned by this repository.
$sdkConfiguration = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'global.json') -Raw | ConvertFrom-Json
$userDotnetRoot = Join-Path $env:USERPROFILE '.dotnet'
$userDotnetExecutable = Join-Path $userDotnetRoot 'dotnet.exe'
$requiredSdkDirectory = Join-Path $userDotnetRoot ('sdk\' + $sdkConfiguration.sdk.version)
if ((Test-Path -LiteralPath $userDotnetExecutable -PathType Leaf) -and
    (Test-Path -LiteralPath $requiredSdkDirectory -PathType Container)) {
    $env:PATH = $userDotnetRoot + ';' + $env:PATH
    $env:DOTNET_ROOT = $userDotnetRoot
}
