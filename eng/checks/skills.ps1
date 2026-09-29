param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ArtifactsRoot
)
$ErrorActionPreference = 'Stop'
$skillsRoot = Join-Path $RepositoryRoot '.agents\skills'
$skillDirectories = Get-ChildItem -LiteralPath $skillsRoot -Directory
if ($skillDirectories.Count -eq 0) { throw 'No repository skills found.' }

foreach ($directory in $skillDirectories) {
    if ($directory.Name -notmatch '^[a-z0-9]+(?:-[a-z0-9]+)*$' -or $directory.Name.Length -gt 64) {
        throw "Invalid skill directory name: $($directory.Name)"
    }
    $skillFile = Join-Path $directory.FullName 'SKILL.md'
    if (-not (Test-Path -LiteralPath $skillFile -PathType Leaf)) { throw "Missing SKILL.md in $($directory.Name)" }
    $content = Get-Content -LiteralPath $skillFile -Raw
    if (-not $content.StartsWith("---`n") -and -not $content.StartsWith("---`r`n")) {
        throw "SKILL.md requires YAML frontmatter: $skillFile"
    }
    $name = [regex]::Match($content, '(?m)^name:\s*(?<value>.+)$').Groups['value'].Value.Trim()
    $description = [regex]::Match($content, '(?m)^description:\s*(?<value>.+)$').Groups['value'].Value.Trim()
    if ($name -ne $directory.Name) { throw "Skill name '$name' must match directory '$($directory.Name)'." }
    if ([string]::IsNullOrWhiteSpace($description) -or $description.Length -gt 1024) {
        throw "Skill description is missing or too long: $skillFile"
    }
    if ($content -match '(?i)\b(TODO|PLACEHOLDER|REPLACE ME)\b') {
        throw "Unfinished scaffold marker in $skillFile"
    }
}

Write-Output "Agent skills valid: $($skillDirectories.Count) skills checked."
