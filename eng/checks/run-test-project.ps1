param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ArtifactsRoot,
    [Parameter(Mandatory)][string]$Project,
    [Parameter(Mandatory)][string]$ResultName
)
$ErrorActionPreference = 'Stop'
$resultDirectory = Join-Path $ArtifactsRoot 'test-results'
New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
& dotnet test (Join-Path $RepositoryRoot $Project) --configuration Release --no-build --no-restore --nologo `
    --artifacts-path $ArtifactsRoot --logger "trx;LogFileName=$ResultName.trx" --results-directory $resultDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
