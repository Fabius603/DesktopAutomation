param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$ArtifactsRoot)
$ErrorActionPreference = 'Stop'
& dotnet build (Join-Path $RepositoryRoot 'DesktopAutomation.sln') --configuration Release --no-restore --nologo `
    --artifacts-path $ArtifactsRoot --disable-build-servers -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
