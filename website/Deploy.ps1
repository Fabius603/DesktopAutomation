param()
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.ps1') -Check -Strict
if ($LASTEXITCODE -ne 0) { throw 'Website-Prüfung fehlgeschlagen; Upload wurde nicht gestartet.' }
$nodeCommand = Get-Command node -ErrorAction SilentlyContinue
$nodePath = if ($nodeCommand) { $nodeCommand.Source } else { Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe' }
$wranglerPath = Join-Path $PSScriptRoot 'node_modules/wrangler/bin/wrangler.js'
foreach ($requiredPath in @($nodePath, $wranglerPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) { throw "Fehlendes Werkzeug: $requiredPath. Siehe README.md." }
}
$savedPath = $env:PATH
$savedAccount = $env:CLOUDFLARE_ACCOUNT_ID
Push-Location $PSScriptRoot
try {
    $env:PATH = (Split-Path $nodePath) + ';' + $env:PATH
    $env:CLOUDFLARE_ACCOUNT_ID = 'feea43412bff69e88e49e0aecafba999'
    # Wrangler owns OAuth storage and refresh; never read another CLI's credential files.
    & $nodePath $wranglerPath whoami --json | Out-Null
    if ($LASTEXITCODE -ne 0) {
        if ($env:CLOUDFLARE_API_TOKEN -or $env:CLOUDFLARE_API_KEY) {
            throw 'Cloudflare-Anmeldung fehlgeschlagen. Prüfe die gesetzten Cloudflare-Zugangsdaten in deiner Umgebung.'
        }
        Write-Host 'Bitte die Cloudflare-Anmeldung im Browser bestätigen.'
        & $nodePath $wranglerPath login
        if ($LASTEXITCODE -ne 0) { throw 'Cloudflare-Anmeldung fehlgeschlagen.' }
        & $nodePath $wranglerPath whoami --json | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Cloudflare-Anmeldung konnte nicht bestätigt werden.' }
    }
    & $nodePath --check 'dist/website.js'
    if ($LASTEXITCODE -ne 0) { throw 'Website-JavaScript enthält einen Syntaxfehler.' }
    Write-Host 'Die Dateien in dist werden jetzt öffentlich veröffentlicht.'
    & $nodePath $wranglerPath pages deploy dist --project-name desktopautomation --branch main
    if ($LASTEXITCODE -ne 0) { throw 'Veröffentlichung fehlgeschlagen.' }
    Write-Host 'Website: https://desktopautomation.pages.dev'
} finally {
    $env:PATH = $savedPath
    $env:CLOUDFLARE_ACCOUNT_ID = $savedAccount
    Pop-Location
}
