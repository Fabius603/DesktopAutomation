param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$ArtifactsRoot)
& (Join-Path $PSScriptRoot 'run-test-project.ps1') -RepositoryRoot $RepositoryRoot -ArtifactsRoot $ArtifactsRoot -Project 'tests\DesktopAutomation.IntegrationTests\DesktopAutomation.IntegrationTests.csproj' -ResultName 'integration'
exit $LASTEXITCODE
