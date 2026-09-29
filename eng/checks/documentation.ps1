param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ArtifactsRoot
)
$ErrorActionPreference = 'Stop'

function Assert-FrontmatterValue([string]$Path, [string]$Name, [string]$Expected = '') {
    $content = Get-Content -LiteralPath $Path -Raw
    $match = [regex]::Match($content, "(?m)^$([regex]::Escape($Name)):\s*(?<value>.*)$")
    if (-not $match.Success) { throw "Missing '$Name' frontmatter in $Path" }
    if ($Expected -and $match.Groups['value'].Value.Trim() -ne $Expected) {
        throw "Invalid '$Name' in $Path. Expected '$Expected'."
    }
}

$specRoot = Join-Path $RepositoryRoot 'docs\specs'
Get-ChildItem -LiteralPath $specRoot -File -Filter '*.md' | Where-Object Name -ne 'README.md' | ForEach-Object {
    if ($_.Name -notmatch '^\d{4}-\d{2}-\d{2}-[a-z0-9-]+\.md$') {
        throw "Specification name must be date-title.md: $($_.Name)"
    }
    Assert-FrontmatterValue $_.FullName 'date'
    Assert-FrontmatterValue $_.FullName 'title'
    Assert-FrontmatterValue $_.FullName 'status' 'historical-snapshot'
    Assert-FrontmatterValue $_.FullName 'superseded_by'
}

$decisionRoot = Join-Path $RepositoryRoot 'docs\decisions'
$allowedStatuses = @('proposed', 'accepted', 'rejected', 'deprecated', 'superseded')
Get-ChildItem -LiteralPath $decisionRoot -File -Filter '*.md' | Where-Object Name -ne 'README.md' | ForEach-Object {
    if ($_.Name -notmatch '^\d{4}-\d{2}-\d{2}-[a-z0-9-]+\.md$') {
        throw "Decision name must be date-title.md: $($_.Name)"
    }
    Assert-FrontmatterValue $_.FullName 'date'
    $content = Get-Content -LiteralPath $_.FullName -Raw
    $status = [regex]::Match($content, '(?m)^status:\s*(?<value>.+)$').Groups['value'].Value.Trim()
    if ($status -notin $allowedStatuses) { throw "Unsupported decision status '$status' in $($_.Name)" }
    foreach ($heading in @('## Context', '## Decision', '## Consequences')) {
        if ($content.IndexOf($heading, [StringComparison]::Ordinal) -lt 0) {
            throw "Decision $($_.Name) is missing '$heading'."
        }
    }
}

$markdownFiles = Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'docs') -Recurse -File -Filter '*.md'
$markdownFiles += Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot '.agents') -Recurse -File -Filter '*.md'
foreach ($file in $markdownFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($content, '\[[^\]]+\]\((?<target>[^)]+)\)')) {
        $target = $match.Groups['target'].Value.Trim().Split('#')[0]
        if (-not $target -or $target -match '^(https?:|mailto:)') { continue }
        $resolved = [IO.Path]::GetFullPath((Join-Path $file.DirectoryName ([Uri]::UnescapeDataString($target))))
        if (-not (Test-Path -LiteralPath $resolved)) {
            throw "Broken local Markdown link in $($file.FullName): $target"
        }
    }
}

Write-Output "Documentation valid: $($markdownFiles.Count) Markdown files checked."
