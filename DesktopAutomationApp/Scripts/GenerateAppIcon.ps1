param(
    [string]$Color = '#2196F3',
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\Assets\App.ico')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param([float]$X, [float]$Y, [float]$Width, [float]$Height, [float]$Radius)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPng {
    param([int]$Size, [System.Drawing.Color]$Accent)

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 256.0
    $accentBrush = New-Object System.Drawing.SolidBrush($Accent)
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $whitePen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, (12 * $scale))
    $whitePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $whitePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    $background = New-RoundedRectanglePath (12 * $scale) (12 * $scale) (232 * $scale) (232 * $scale) (54 * $scale)
    $graphics.FillPath($accentBrush, $background)
    $graphics.DrawLine($whitePen, 128 * $scale, 88 * $scale, 128 * $scale, 61 * $scale)
    $graphics.FillEllipse($whiteBrush, 119 * $scale, 48 * $scale, 18 * $scale, 18 * $scale)

    $face = New-RoundedRectanglePath (58 * $scale) (82 * $scale) (140 * $scale) (108 * $scale) (30 * $scale)
    $graphics.FillPath($whiteBrush, $face)
    $graphics.FillEllipse($accentBrush, 89 * $scale, 123 * $scale, 20 * $scale, 20 * $scale)
    $graphics.FillEllipse($accentBrush, 147 * $scale, 123 * $scale, 20 * $scale, 20 * $scale)

    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $stream.ToArray()

    $stream.Dispose()
    $face.Dispose()
    $background.Dispose()
    $whitePen.Dispose()
    $whiteBrush.Dispose()
    $accentBrush.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    return $bytes
}

$accent = [System.Drawing.ColorTranslator]::FromHtml($Color)
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($size in $sizes) {
    $frames.Add((New-IconPng -Size $size -Accent $accent))
}
$output = [System.IO.Path]::GetFullPath($OutputPath)
$stream = [System.IO.File]::Create($output)
$writer = New-Object System.IO.BinaryWriter($stream)

try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$frames.Count)

    $offset = 6 + 16 * $frames.Count
    for ($index = 0; $index -lt $frames.Count; $index++) {
        $size = $sizes[$index]
        $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $frames[$index].Length
    }

    foreach ($frame in $frames) {
        $writer.Write($frame)
    }
}
finally {
    $writer.Dispose()
    $stream.Dispose()
}

Write-Output $output
