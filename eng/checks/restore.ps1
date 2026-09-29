param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$ArtifactsRoot)
$ErrorActionPreference = 'Stop'
& dotnet restore (Join-Path $RepositoryRoot 'DesktopAutomation.sln') --nologo --artifacts-path $ArtifactsRoot `
    --disable-build-servers -m:1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
