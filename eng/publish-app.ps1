param([string]$OutputDirectory = 'artifacts/distribution/publish')
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
# Keep native libraries beside the application: Tesseract resolves them relative to its assembly.
& dotnet publish (Join-Path $repositoryRoot 'DesktopAutomationApp/DesktopAutomationApp.csproj') `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false `
    -m:1 -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }
