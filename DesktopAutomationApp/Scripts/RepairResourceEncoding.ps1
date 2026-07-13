param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))

$windows1252 = [Text.Encoding]::GetEncoding(1252)
$utf8 = [Text.Encoding]::UTF8
foreach ($name in @('Strings.resx', 'Strings.en.resx')) {
    $path = Join-Path $ProjectRoot "Resources\$name"
    [xml]$xml = [IO.File]::ReadAllText($path)
    foreach ($entry in $xml.root.data) {
        $value = [string]$entry.value
        for ($iteration = 0; $iteration -lt 6 -and $value -match '[ÃÂâ]'; $iteration++) {
            $decoded = $utf8.GetString($windows1252.GetBytes($value))
            if ($decoded -eq $value) { break }
            $value = $decoded
        }
        $entry.value = $value
    }
    $settings = [System.Xml.XmlWriterSettings]::new(); $settings.Indent = $true; $settings.Encoding = [Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($path, $settings); $xml.Save($writer); $writer.Dispose()
}
