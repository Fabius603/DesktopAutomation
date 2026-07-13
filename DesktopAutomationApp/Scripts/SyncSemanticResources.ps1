param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))

$rows = Get-Content (Join-Path $ProjectRoot 'Resources\SemanticTranslations.tsv') -Encoding UTF8
$targets = @(
    @{ Path = Join-Path $ProjectRoot 'Resources\Strings.resx'; Column = 1 },
    @{ Path = Join-Path $ProjectRoot 'Resources\Strings.en.resx'; Column = 2 }
)
foreach ($target in $targets) {
    [xml]$xml = [IO.File]::ReadAllText($target.Path)
    foreach ($row in $rows) {
        if ([string]::IsNullOrWhiteSpace($row)) { continue }
        $parts = $row -split "`t", 3
        if ($parts.Count -ne 3) { throw "Invalid translation row: $row" }
        $entry = $xml.root.data | Where-Object { $_.name -eq $parts[0] } | Select-Object -First 1
        if (!$entry) {
            $entry = $xml.CreateElement('data'); [void]$entry.SetAttribute('name', $parts[0]); [void]$entry.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
            $value = $xml.CreateElement('value'); [void]$entry.AppendChild($value); [void]$xml.root.AppendChild($entry)
        }
        $entry.value = $parts[$target.Column] -replace '\\n', "`n"
    }
    $settings = [System.Xml.XmlWriterSettings]::new(); $settings.Indent = $true; $settings.Encoding = [Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($target.Path, $settings); $xml.Save($writer); $writer.Dispose()
}
