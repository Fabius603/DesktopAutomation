[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $repositoryRoot 'eng\initialize-dotnet.ps1') -RepositoryRoot $repositoryRoot
$projectPath = Join-Path $repositoryRoot 'DesktopAutomationApp\DesktopAutomationApp.csproj'
$appProcess = $null
$appName = ''

function Stop-DevelopmentApp {
    if ($null -eq $script:appProcess) { return }
    if (-not $script:appProcess.HasExited) {
        Write-Host 'App wird beendet ...'
        $null = $script:appProcess.CloseMainWindow()
        if (-not $script:appProcess.WaitForExit(5000)) {
            $script:appProcess.Kill()
            if (-not $script:appProcess.WaitForExit(5000)) {
                throw 'Die App konnte nicht beendet werden.'
            }
        }
    }
    $script:appProcess.Dispose()
    $script:appProcess = $null
}

Push-Location $repositoryRoot
try {
    while ($true) {
        if ($null -ne $appProcess -and $appProcess.HasExited) {
            Write-Host "$appName wurde beendet (Exitcode $($appProcess.ExitCode))."
            $appProcess.Dispose()
            $appProcess = $null
        }
        Write-Host ''
        Write-Host 'DesktopAutomation'
        Write-Host '================='
        if ($null -ne $appProcess) { Write-Host "Aktiv: $appName (PID $($appProcess.Id))" }
        Write-Host '[1] Debug-App starten'
        Write-Host '[2] Test-App starten (Release)'
        Write-Host '[3] App beenden'
        Write-Host '[4] Alle Tests und Repository-Pruefungen ausfuehren'
        Write-Host '[Q] App und Konsole beenden'
        $selection = Read-Host 'Auswahl'
        try {
            switch ($selection.Trim().ToUpperInvariant()) {
                { $_ -in '1', '2' } {
                    Stop-DevelopmentApp
                    $configuration = if ($_ -eq '1') { 'Debug' } else { 'Release' }
                    $appName = if ($_ -eq '1') { 'Debug-App' } else { 'Test-App' }
                    & dotnet build $projectPath --configuration $configuration --nologo
                    if ($LASTEXITCODE -ne 0) {
                        Write-Host "Build fehlgeschlagen (Exitcode $LASTEXITCODE)."
                        break
                    }
                    $targetPath = & dotnet msbuild $projectPath --nologo "-property:Configuration=$configuration" -getProperty:TargetPath
                    if ($LASTEXITCODE -ne 0) {
                        throw "Der Build-Ausgabepfad konnte nicht ermittelt werden (Exitcode $LASTEXITCODE)."
                    }
                    $executable = [IO.Path]::ChangeExtension(([string]$targetPath).Trim(), '.exe')
                    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
                        throw "Die gerade gebaute App wurde nicht gefunden: $executable"
                    }
                    $appProcess = Start-Process -FilePath $executable -WorkingDirectory $repositoryRoot -PassThru
                    Write-Host "$appName gestartet (PID $($appProcess.Id))."
                }
                '3' { Stop-DevelopmentApp }
                '4' {
                    Stop-DevelopmentApp
                    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test.ps1')
                    Write-Host "Repository-Pruefung beendet (Exitcode $LASTEXITCODE)."
                }
                'Q' { Stop-DevelopmentApp; return }
                default { Write-Host 'Bitte 1, 2, 3, 4 oder Q eingeben.' }
            }
        }
        catch { Write-Host "Fehler: $_" }
    }
}
finally {
    try { Stop-DevelopmentApp } finally { Pop-Location }
}
