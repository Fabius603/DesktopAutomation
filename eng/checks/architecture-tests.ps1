param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$ArtifactsRoot)
& (Join-Path $PSScriptRoot 'run-test-project.ps1') -RepositoryRoot $RepositoryRoot -ArtifactsRoot $ArtifactsRoot -Project 'tests\DesktopAutomation.ArchitectureTests\DesktopAutomation.ArchitectureTests.csproj' -ResultName 'architecture'
exit $LASTEXITCODE
