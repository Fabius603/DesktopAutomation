param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ArtifactsRoot
)
$ErrorActionPreference = 'Stop'
$releaseNotesPath = Join-Path $RepositoryRoot 'DesktopAutomationApp\Resources\ReleaseNotes.json'
$notes = Get-Content -LiteralPath $releaseNotesPath -Raw | ConvertFrom-Json
if ($notes.Count -eq 0) { throw 'ReleaseNotes.json must contain at least one release.' }

$headProject = & git -C $RepositoryRoot show HEAD:DesktopAutomationApp/DesktopAutomationApp.csproj
if ($LASTEXITCODE -ne 0) { throw 'Unable to read the released version from Git HEAD.' }
$versionMatch = [regex]::Match(($headProject -join "`n"), '<Version>(?<version>[^<]+)</Version>')
if (-not $versionMatch.Success) { throw 'Git HEAD project version was not found.' }
$released = [Version]$versionMatch.Groups['version'].Value
$expected = [Version]::new($released.Major, $released.Minor, $released.Build + 1)
$newestVersion = [Version]([string]$notes[0].version)
if ($newestVersion -ne $expected) {
    throw "Newest release-note version must be $expected based on Git HEAD, found $($notes[0].version)."
}

$previous = $null
foreach ($note in $notes) {
    $current = [Version]([string]$note.version)
    if ($null -ne $previous -and $current -ge $previous) { throw 'Release notes must be newest first.' }
    $previous = $current
    [DateTime]::ParseExact([string]$note.date, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture) | Out-Null
    foreach ($section in @($note.sections)) {
        if ([string]::IsNullOrWhiteSpace([string]$section.title.de) -or [string]::IsNullOrWhiteSpace([string]$section.title.en)) {
            throw "Release $current contains a section without German and English titles."
        }
        foreach ($change in @($section.changes)) {
            if ($change.category -notin @('Added', 'Changed', 'Fixed')) {
                throw "Release $current uses unsupported category '$($change.category)'."
            }
            if ([string]::IsNullOrWhiteSpace([string]$change.de) -or [string]::IsNullOrWhiteSpace([string]$change.en)) {
                throw "Release $current contains a change without German and English text."
            }
        }
    }
}

Write-Output "Release notes valid: $($notes.Count) releases; unreleased version $expected."
