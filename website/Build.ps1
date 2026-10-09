param([switch]$Check, [switch]$Strict, [switch]$Render, [string]$Screenshots,
    [switch]$ChangedScreenshots, [switch]$ListScreenshots)
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repositoryRoot 'eng/initialize-dotnet.ps1') -RepositoryRoot $repositoryRoot
$toolsRoot = Join-Path $repositoryRoot 'artifacts/website-tools'
$exportRoot = Join-Path $repositoryRoot 'artifacts/website-export'
Push-Location $repositoryRoot
try {
    if ($ListScreenshots) {
        & python website/tools/screenshots.py list
        if ($LASTEXITCODE -ne 0) { throw 'Screenshot catalog is invalid.' }
        return
    }
    $renderRequested = $Render -or $Screenshots -or $ChangedScreenshots
    if ($Check -and $renderRequested) { throw '-Check cannot update screenshots.' }
    if ($Screenshots -and $ChangedScreenshots) { throw 'Choose explicit IDs or changed screenshots.' }
    $selection = ''
    if ($renderRequested) {
        $selectionArguments = @('website/tools/screenshots.py', 'select')
        if ($Screenshots) { $selectionArguments += @('--ids', $Screenshots) }
        if ($ChangedScreenshots) { $selectionArguments += '--changed' }
        $selection = (& python @selectionArguments) -join ''
        if ($LASTEXITCODE -ne 0) { throw 'Screenshot selection is invalid.' }
    }
    & dotnet build website/tools/Exporter/Exporter.csproj --artifacts-path $toolsRoot -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Website contract exporter build failed.' }
    & dotnet (Join-Path $toolsRoot 'bin/Exporter/debug/Exporter.dll') $repositoryRoot $exportRoot
    if ($LASTEXITCODE -ne 0) { throw 'Contract export or canonical example validation failed.' }
    & python website/tools/sync_export.py $exportRoot $(if ($Check) { '--check' } else { '--write' })
    if ($LASTEXITCODE -ne 0) { throw 'Website metadata or example configurations are stale.' }
    if ($renderRequested -and $selection) {
        & dotnet build website/tools/Renderer/Renderer.csproj --artifacts-path $toolsRoot -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Website renderer build failed.' }
        $assetRoot = Join-Path $repositoryRoot 'artifacts/website-assets'
        & dotnet (Join-Path $toolsRoot 'bin/Renderer/debug/Renderer.dll') $repositoryRoot $assetRoot $selection
        if ($LASTEXITCODE -ne 0) { throw 'Application screenshots could not be rendered.' }
        $captureFiles = & python website/tools/screenshots.py files --ids $selection
        if ($LASTEXITCODE -ne 0) { throw 'Screenshot output selection is invalid.' }
        foreach ($captureFile in $captureFiles) {
            Copy-Item -LiteralPath (Join-Path $assetRoot $captureFile) -Destination (Join-Path $PSScriptRoot "dist/$captureFile")
        }
        & python website/tools/screenshots.py record --ids $selection
        if ($LASTEXITCODE -ne 0) { throw 'Screenshot recording failed.' }
    }
    $arguments = @('website/tools/build_site.py')
    if ($Check) { $arguments += '--check' }
    if ($Strict) { $arguments += '--strict' }
    & python @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Website generation or documentation coverage failed.' }
    $imageArguments = @('website/tools/screenshots.py', 'check')
    if ($renderRequested -and $selection) { $imageArguments += @('--ids', $selection, '--allow-unreviewed') }
    & python @imageArguments
    if ($LASTEXITCODE -ne 0) { throw 'Screenshot sources, files, review status or placement are stale.' }
} finally { Pop-Location }
