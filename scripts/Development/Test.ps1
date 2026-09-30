[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$verificationScript = Join-Path $repositoryRoot 'eng\verify.ps1'

& powershell -NoProfile -ExecutionPolicy Bypass -File $verificationScript -Mode Full
exit $LASTEXITCODE
