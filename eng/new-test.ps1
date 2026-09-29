[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Unit', 'Contract', 'Architecture', 'Integration', 'Ui', 'EndToEnd')]
    [string]$Type,
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z][A-Za-z0-9]+$')][string]$Behavior,
    [string]$Area = 'General'
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectNames = @{
    Unit = 'DesktopAutomation.UnitTests'; Contract = 'DesktopAutomation.ContractTests'
    Architecture = 'DesktopAutomation.ArchitectureTests'; Integration = 'DesktopAutomation.IntegrationTests'
    Ui = 'DesktopAutomation.UiTests'; EndToEnd = 'DesktopAutomation.EndToEndTests'
}
$projectName = $projectNames[$Type]
$safeArea = $Area -replace '[^A-Za-z0-9.]', ''
if ([string]::IsNullOrWhiteSpace($safeArea)) { throw 'Area must contain letters or digits.' }
$directory = Join-Path $repoRoot "tests\$projectName\$($safeArea -replace '\.', '\')"
$path = Join-Path $directory "$Behavior`Tests.cs"
if (Test-Path -LiteralPath $path) { throw "Test file already exists: $path" }

$content = @"
namespace $projectName.$safeArea;

public sealed class $Behavior`Tests
{
    [Fact]
    public void Behavior_Scenario_ExpectedResult()
    {
        // Arrange observable inputs and controlled dependencies.

        // Act through the public behavior boundary.

        // Assert the outcome or invariant, not implementation choreography.
        throw new NotImplementedException("Replace the scaffold with a behavior-focused test.");
    }
}
"@

if ($PSCmdlet.ShouldProcess($path, 'Create test scaffold')) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    Set-Content -LiteralPath $path -Value $content -Encoding utf8
    Write-Output $path
}
