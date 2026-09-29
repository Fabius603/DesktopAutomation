param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$ArtifactsRoot)
& (Join-Path $PSScriptRoot 'run-test-project.ps1') -RepositoryRoot $RepositoryRoot -ArtifactsRoot $ArtifactsRoot -Project 'tests\DesktopAutomation.UnitTests\DesktopAutomation.UnitTests.csproj' -ResultName 'unit'
exit $LASTEXITCODE
