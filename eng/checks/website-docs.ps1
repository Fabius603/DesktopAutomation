param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ArtifactsRoot
)
$ErrorActionPreference = 'Stop'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepositoryRoot 'website/Build.ps1') -Check -Strict
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& python (Join-Path $RepositoryRoot 'website/tools/test_build_site.py')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& python (Join-Path $RepositoryRoot 'website/tools/test_screenshots.py')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output 'Website contracts, examples, generated output, coverage and links are valid.'
