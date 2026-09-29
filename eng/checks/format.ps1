param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ArtifactsRoot
)
$ErrorActionPreference = 'Stop'

function Add-ChangedSource([Collections.Generic.HashSet[string]]$Set, [string]$StatusLine) {
    if ([string]::IsNullOrWhiteSpace($StatusLine)) { return }
    $parts = $StatusLine -split "`t"
    $status = $parts[0]
    $path = $parts[-1]
    if ($status -eq 'R100') { return }
    if ($status -match '^[AMCR]' -and $path.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase)) {
        [void]$Set.Add(($path -replace '/', '\'))
    }
}

$sources = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$workingChanges = @(& git -C $RepositoryRoot diff HEAD --name-status)
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect working-tree changes for formatting.' }

if ($workingChanges.Count -gt 0) {
    foreach ($line in $workingChanges) { Add-ChangedSource $sources $line }
    $untracked = @(& git -C $RepositoryRoot ls-files --others --exclude-standard -- '*.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect untracked source files for formatting.' }
    foreach ($path in $untracked) { [void]$sources.Add(($path -replace '/', '\')) }
}
else {
    & git -C $RepositoryRoot rev-parse --verify 'HEAD^' *> $null
    if ($LASTEXITCODE -eq 0) {
        foreach ($line in @(& git -C $RepositoryRoot diff 'HEAD^' HEAD --name-status)) {
            Add-ChangedSource $sources $line
        }
    }
}

if ($sources.Count -eq 0) {
    Write-Output 'No added or modified C# source files require formatting verification.'
    exit 0
}

$arguments = @(
    'format', (Join-Path $RepositoryRoot 'DesktopAutomation.sln'),
    '--verify-no-changes', '--verbosity', 'minimal', '--include'
) + @($sources | Sort-Object)
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Output "Formatting valid: $($sources.Count) changed C# files checked."
