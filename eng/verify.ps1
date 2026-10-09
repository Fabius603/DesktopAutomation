[CmdletBinding()]
param(
    [ValidateSet('Full', 'Focused')]
    [string]$Mode = 'Full',
    [string[]]$Checks = @(),
    [string]$TestFilter = ''
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'initialize-dotnet.ps1') -RepositoryRoot $repoRoot
$manifestPath = Join-Path $PSScriptRoot 'test-manifest.json'
$artifactsRoot = Join-Path $repoRoot 'artifacts\verify'

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Verification manifest not found: $manifestPath"
}

# Validate the plan before acquiring the lock or touching existing results.
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$requestedChecks = @($Checks | ForEach-Object { $_.Split(',') } | ForEach-Object { $_.Trim() })
if ($Mode -eq 'Full' -and ($requestedChecks.Count -gt 0 -or $TestFilter)) {
    throw 'Full verification cannot be restricted by Checks or TestFilter. Use -Mode Focused.'
}
$checksById = @{}
foreach ($check in $manifest.checks) {
    $id = [IO.Path]::GetFileNameWithoutExtension([string]$check.script)
    if ($checksById.ContainsKey($id)) { throw "Duplicate verification check: $id" }
    $checksById[$id] = $check
}
$selected = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
function Add-Check([string]$Id, [string[]]$Ancestors = @()) {
    if (-not $checksById.ContainsKey($Id)) {
        throw "Unknown check '$Id'. Available checks: $((@($checksById.Keys) | Sort-Object) -join ', ')"
    }
    if ($Id -in $Ancestors) { throw "Cyclic verification dependency: $Id" }
    if ($selected.Contains($Id)) { return }
    foreach ($dependency in $checksById[$Id].prerequisites) {
        Add-Check ([string]$dependency) ($Ancestors + $Id)
    }
    [void]$selected.Add($Id)
}
if ($Mode -eq 'Focused') {
    if ($requestedChecks.Count -eq 0) { throw 'Focused verification requires -Checks.' }
    foreach ($id in $requestedChecks) { Add-Check $id }
}
$plannedChecks = @($manifest.checks | Where-Object {
    $Mode -eq 'Full' -or $selected.Contains([IO.Path]::GetFileNameWithoutExtension([string]$_.script))
})
if ($TestFilter -and @($plannedChecks | Where-Object { $_.acceptsTestFilter }).Count -eq 0) {
    throw 'TestFilter requires at least one test check.'
}

# Serialize the shared output directory across local chats and CI invocations.
$verificationHasher = [Security.Cryptography.SHA256]::Create()
try {
    $verificationKey = [BitConverter]::ToString($verificationHasher.ComputeHash(
        [Text.Encoding]::UTF8.GetBytes($repoRoot.ToLowerInvariant()))).Replace('-', '')
}
finally { $verificationHasher.Dispose() }
$verificationMutex = [Threading.Mutex]::new($false, ('Local\DesktopAutomation.Verify.' + $verificationKey))
$verificationLockTaken = $false
try {
    $verificationDeadline = [DateTimeOffset]::UtcNow.AddMinutes(10)
    try { $verificationLockTaken = $verificationMutex.WaitOne(0) }
    catch [Threading.AbandonedMutexException] { $verificationLockTaken = $true }
    while (-not $verificationLockTaken) {
        Write-Host 'Waiting for another repository verification to finish...'
        try { $verificationLockTaken = $verificationMutex.WaitOne([TimeSpan]::FromSeconds(30)) }
        catch [Threading.AbandonedMutexException] { $verificationLockTaken = $true }
        if (-not $verificationLockTaken -and [DateTimeOffset]::UtcNow -ge $verificationDeadline) {
            throw 'Timed out waiting for another repository verification to finish.'
        }
    }

    if (Test-Path -LiteralPath $artifactsRoot) {
        Remove-Item -LiteralPath $artifactsRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = '1'
    $env:DOTNET_CLI_UI_LANGUAGE = 'en'
    $env:NUGET_XMLDOC_MODE = 'skip'
    $env:TZ = 'UTC'

    $results = [Collections.Generic.List[object]]::new()
    $started = [DateTimeOffset]::UtcNow
    $powerShellExecutable = (Get-Process -Id $PID).Path

    Push-Location $repoRoot
    try {
        foreach ($check in $plannedChecks) {
            $scriptPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$check.script)))
            if (-not $scriptPath.StartsWith([IO.Path]::GetFullPath($PSScriptRoot), [StringComparison]::OrdinalIgnoreCase)) {
                throw "Verification script escapes eng directory: $($check.script)"
            }
            if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
                throw "Verification script not found: $scriptPath"
            }

            Write-Host "`n==> $($check.name)" -ForegroundColor Cyan
            $checkStarted = [DateTimeOffset]::UtcNow
            $checkArguments = @('-RepositoryRoot', $repoRoot, '-ArtifactsRoot', $artifactsRoot)
            if ($TestFilter -and $check.acceptsTestFilter) {
                $checkArguments += @('-TestFilter', $TestFilter)
            }
            & $powerShellExecutable -NoProfile -ExecutionPolicy Bypass -File $scriptPath @checkArguments
            $exitCode = $LASTEXITCODE
            $duration = [DateTimeOffset]::UtcNow - $checkStarted
            $results.Add([pscustomobject]@{
                name = [string]$check.name
                exitCode = $exitCode
                durationSeconds = [Math]::Round($duration.TotalSeconds, 3)
            })
            if ($exitCode -ne 0) {
                throw "Required check '$($check.name)' failed with exit code $exitCode."
            }
        }
    }
    finally {
        Pop-Location
        $summary = [pscustomobject]@{
            mode = $Mode
            requestedChecks = $requestedChecks
            testFilter = $TestFilter
            startedUtc = $started.ToString('O')
            finishedUtc = [DateTimeOffset]::UtcNow.ToString('O')
            checks = $results
        }
        $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $artifactsRoot 'summary.json') -Encoding utf8
    }

    Write-Host "`nAll $Mode repository checks passed." -ForegroundColor Green
}
finally {
    if ($verificationLockTaken) { $verificationMutex.ReleaseMutex() }
    $verificationMutex.Dispose()
}
