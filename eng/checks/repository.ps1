param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ArtifactsRoot
)
$ErrorActionPreference = 'Stop'

$required = @(
    'AGENTS.md', '.agents\README.md', '.agents\instructions', '.agents\skills',
    'docs\architecture', 'docs\decisions', 'docs\specs', 'eng\verify.ps1',
    'tests\DesktopAutomation.UnitTests\DesktopAutomation.UnitTests.csproj',
    'tests\DesktopAutomation.ContractTests\DesktopAutomation.ContractTests.csproj',
    'tests\DesktopAutomation.ArchitectureTests\DesktopAutomation.ArchitectureTests.csproj',
    'tests\DesktopAutomation.IntegrationTests\DesktopAutomation.IntegrationTests.csproj',
    'tests\DesktopAutomation.UiTests\DesktopAutomation.UiTests.csproj',
    'tests\DesktopAutomation.EndToEndTests\DesktopAutomation.EndToEndTests.csproj',
    'tests\TestInfrastructure'
)
foreach ($relative in $required) {
    $path = Join-Path $RepositoryRoot $relative
    if (-not (Test-Path -LiteralPath $path)) { throw "Required repository path is missing: $relative" }
}

$workflow = Get-Content -LiteralPath (Join-Path $RepositoryRoot '.github\workflows\test.yml') -Raw
if ($workflow -notmatch 'eng[/\\]verify\.ps1') {
    throw 'CI must invoke eng/verify.ps1 instead of maintaining a separate verification flow.'
}

$legacyProject = Join-Path $RepositoryRoot 'tests\TaskAutomation.Tests\TaskAutomation.Tests.csproj'
if (Test-Path -LiteralPath $legacyProject) {
    throw 'Legacy mixed test project still exists. Tests must be assigned to an explicit layer.'
}

Write-Output 'Repository structure is valid.'
