param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'

[xml]$de = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'Resources\Strings.resx'))
[xml]$en = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'Resources\Strings.en.resx'))
$deKeys = @($de.root.data | ForEach-Object { [string]$_.name })
$enKeys = @($en.root.data | ForEach-Object { [string]$_.name })
$missingEnglish = @($deKeys | Where-Object { $_ -notin $enKeys })
$missingGerman = @($enKeys | Where-Object { $_ -notin $deKeys })
if ($missingEnglish.Count -or $missingGerman.Count) {
    throw "Translation key mismatch. Missing EN: $($missingEnglish -join ', '); missing DE: $($missingGerman -join ', ')"
}

$referenced = [Collections.Generic.HashSet[string]]::new()
$sourceFiles = Get-ChildItem $ProjectRoot -Recurse -File -Include *.xaml,*.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($file in $sourceFiles) {
    $content = [IO.File]::ReadAllText($file.FullName)
    foreach ($match in [regex]::Matches($content, '(?:loc:Translate\s+(?:Key=)?|Loc\.(?:Get|Format)\(")(?<key>[A-Za-z0-9_.]+)')) {
        [void]$referenced.Add($match.Groups['key'].Value)
    }
}
$unknown = @($referenced | Where-Object { $_ -notin $deKeys })
if ($unknown.Count) { throw "Unknown localization keys: $($unknown -join ', ')" }

$hardcoded = @()
foreach ($file in ($sourceFiles | Where-Object Extension -eq '.xaml')) {
    $content = [IO.File]::ReadAllText($file.FullName)
    if ($content -match '(?<![\w])(?:Text|Content|Header|Title|ToolTip|Watermark)="[^"{][^"]*"')
        { $hardcoded += $file.FullName }
}
if ($hardcoded.Count) { throw "Hard-coded XAML UI text found in: $($hardcoded -join ', ')" }

Write-Output "Localization resources valid: $($deKeys.Count) keys, $($referenced.Count) references."
