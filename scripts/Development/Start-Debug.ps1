[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repositoryRoot 'eng\initialize-dotnet.ps1') -RepositoryRoot $repositoryRoot
$projectPath = Join-Path $repositoryRoot 'DesktopAutomationApp\DesktopAutomationApp.csproj'

Push-Location $repositoryRoot
try {
    & dotnet build $projectPath --configuration Debug --nologo
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    & dotnet run --project $projectPath --configuration Debug --no-build
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
