# ScreenParser for DesktopAutomation

The installer exports the pinned
[docling-project/ScreenParser](https://huggingface.co/docling-project/ScreenParser)
checkpoint to a static 1280 x 1280 ONNX detection model and places it in the
DesktopAutomation YOLO model directory.

Run from the repository root:

```powershell
.\scripts\ScreenParser\Install-ScreenParser.ps1
```

Use `-Force` to replace an existing `screenparser.onnx`. The Python environment
is created below the ignored `artifacts` directory. The model itself is written
to:

```text
%AppData%\DesktopAutomation\YoloModels\screenparser.onnx
```

After the export, refresh the **YOLO models** page. The ONNX metadata supplies
the 55 ScreenParser class names; the exporter also writes the corresponding
`screenparser.labels.txt` file used by the job editor.

ScreenParser is developed by IBM Research and ETH Zurich and published under
the Apache-2.0 license. Its published checkpoint is pinned by commit in
`export_screenparser.py` so that exports remain reproducible.
