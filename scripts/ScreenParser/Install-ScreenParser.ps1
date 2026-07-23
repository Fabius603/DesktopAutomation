param(
    [string]$OutputPath = (Join-Path $env:APPDATA "DesktopAutomation\YoloModels\screenparser.onnx"),
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$venvPath = Join-Path $repositoryRoot "artifacts\screenparser-export-venv"
$pythonPath = Join-Path $venvPath "Scripts\python.exe"

if (-not (Test-Path -LiteralPath $pythonPath)) {
    py -3.12 -m venv $venvPath
}

& $pythonPath -m pip install --disable-pip-version-check --upgrade pip
& $pythonPath -m pip install --disable-pip-version-check ultralytics huggingface_hub onnx onnxslim onnxruntime

$arguments = @(
    (Join-Path $PSScriptRoot "export_screenparser.py"),
    "--output",
    $OutputPath
)
if ($Force) {
    $arguments += "--force"
}

& $pythonPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "ScreenParser export failed with exit code $LASTEXITCODE."
}
