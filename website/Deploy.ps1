param()
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.ps1') -Check -Strict
if ($LASTEXITCODE -ne 0) { throw 'Website-Prüfung fehlgeschlagen; Upload wurde nicht gestartet.' }
$nodeCommand = Get-Command node -ErrorAction SilentlyContinue
$nodePath = if ($nodeCommand) { $nodeCommand.Source } else { Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe' }
$cfCommand = Get-Command cf.cmd -ErrorAction SilentlyContinue
$cfPath = if ($cfCommand) { $cfCommand.Source } else { Join-Path $env:LOCALAPPDATA 'cloudflare-cli/cf.cmd' }
$wranglerPath = Join-Path $PSScriptRoot 'node_modules/wrangler/bin/wrangler.js'
foreach ($requiredPath in @($nodePath, $cfPath, $wranglerPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) { throw "Fehlendes Werkzeug: $requiredPath. Siehe README.md." }
}
$savedPath = $env:PATH
$savedToken = $env:CLOUDFLARE_API_TOKEN
$savedAccount = $env:CLOUDFLARE_ACCOUNT_ID
Push-Location $PSScriptRoot
try {
    $env:PATH = (Split-Path $nodePath) + ';' + $env:PATH
    $loginState = (& $cfPath auth whoami | Out-String | ConvertFrom-Json)
    if (-not $loginState.authenticated) {
        Write-Host 'Bitte die Cloudflare-Anmeldung im Browser bestätigen.'
        & $cfPath auth login
        if ($LASTEXITCODE -ne 0) { throw 'Cloudflare-Anmeldung fehlgeschlagen.' }
    }
    $profilePath = Join-Path $env:USERPROFILE 'AppData/Roaming/xdg.config/cloudflare/config/default.json'
    $loginProfile = Get-Content -LiteralPath $profilePath -Raw | ConvertFrom-Json
    if (-not $loginProfile.oauth_token) { throw 'Cloudflare-Zugang fehlt im Standardprofil.' }
    $env:CLOUDFLARE_API_TOKEN = $loginProfile.oauth_token
    $env:CLOUDFLARE_ACCOUNT_ID = 'feea43412bff69e88e49e0aecafba999'
    & $nodePath --check 'dist/website.js'
    if ($LASTEXITCODE -ne 0) { throw 'Website-JavaScript enthält einen Syntaxfehler.' }
    Write-Host 'Die Dateien in dist werden jetzt öffentlich veröffentlicht.'
    & $nodePath $wranglerPath pages deploy dist --project-name desktopautomation --branch main
    if ($LASTEXITCODE -ne 0) { throw 'Veröffentlichung fehlgeschlagen.' }
    Write-Host 'Website: https://desktopautomation.pages.dev'
} finally {
    $env:PATH = $savedPath
    $env:CLOUDFLARE_API_TOKEN = $savedToken
    $env:CLOUDFLARE_ACCOUNT_ID = $savedAccount
    Pop-Location
}
