param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$ArtifactsRoot, [string]$TestFilter = '')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'run-test-project.ps1') -RepositoryRoot $RepositoryRoot -ArtifactsRoot $ArtifactsRoot -Project 'tests\DesktopAutomation.ContractTests\DesktopAutomation.ContractTests.csproj' -ResultName 'contract' -TestFilter $TestFilter
exit $LASTEXITCODE
