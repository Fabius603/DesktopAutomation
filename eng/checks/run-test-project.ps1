param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ArtifactsRoot,
    [Parameter(Mandatory)][string]$Project,
    [Parameter(Mandatory)][string]$ResultName,
    [string]$TestFilter = ''
)
$ErrorActionPreference = 'Stop'
$resultDirectory = Join-Path $ArtifactsRoot 'test-results'
New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
$filterArguments = @()
if ($TestFilter) { $filterArguments = @('--filter', $TestFilter) }
& dotnet test (Join-Path $RepositoryRoot $Project) --configuration Release --no-build --no-restore --nologo @filterArguments `
    --artifacts-path $ArtifactsRoot --logger "trx;LogFileName=$ResultName.trx" --results-directory $resultDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($TestFilter) {
    $resultPath = Join-Path $resultDirectory "$ResultName.trx"
    [xml]$testResult = Get-Content -LiteralPath $resultPath -Raw
    $counters = $testResult.SelectSingleNode("//*[local-name()='ResultSummary']/*[local-name()='Counters']")
    if ($null -eq $counters -or [int]$counters.executed -eq 0) {
        throw "Test filter '$TestFilter' did not execute any tests in $Project."
    }
}
