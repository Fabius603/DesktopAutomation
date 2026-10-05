[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repositoryRoot 'eng\initialize-dotnet.ps1') -RepositoryRoot $repositoryRoot
$projectPath = Join-Path $repositoryRoot 'DesktopAutomationApp\DesktopAutomationApp.csproj'

Push-Location $repositoryRoot
try {
    & dotnet run --project $projectPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
