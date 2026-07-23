from __future__ import annotations

import argparse
import hashlib
import shutil
from pathlib import Path

from huggingface_hub import hf_hub_download
import onnxruntime as ort
from ultralytics import YOLO


SCREENPARSER_REPOSITORY = "docling-project/ScreenParser"
SCREENPARSER_REVISION = "f029e565f1206577402e43206454522075be3f72"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def verify_and_write_labels(output: Path, labels: dict[int, str]) -> None:
    session = ort.InferenceSession(str(output), providers=["CPUExecutionProvider"])
    inputs = session.get_inputs()
    outputs = session.get_outputs()
    if len(inputs) != 1 or inputs[0].shape != [1, 3, 1280, 1280]:
        raise RuntimeError(f"Unexpected ScreenParser input: {[item.shape for item in inputs]}")
    if not outputs or outputs[0].shape != [1, 4 + len(labels), 33600]:
        raise RuntimeError(f"Unexpected ScreenParser output: {[item.shape for item in outputs]}")

    ordered_labels = [labels[index] for index in range(len(labels))]
    output.with_suffix(".labels.txt").write_text(
        "\n".join(ordered_labels) + "\n",
        encoding="utf-8",
    )


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Export the pinned ScreenParser checkpoint to a DesktopAutomation-compatible ONNX model."
    )
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()

    output = args.output.expanduser().resolve()
    if output.exists() and not args.force:
        raise FileExistsError(f"{output} already exists. Use --force to replace it.")

    checkpoint = Path(
        hf_hub_download(
            repo_id=SCREENPARSER_REPOSITORY,
            filename="best.pt",
            revision=SCREENPARSER_REVISION,
        )
    )
    model = YOLO(checkpoint)
    exported = Path(
        model.export(
            format="onnx",
            imgsz=1280,
            batch=1,
            dynamic=False,
            simplify=True,
            half=False,
            nms=False,
            opset=18,
        )
    )

    output.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(exported, output)
    verify_and_write_labels(output, model.names)
    print(f"Installed: {output}")
    print(f"Labels: {output.with_suffix('.labels.txt')} ({len(model.names)})")
    print(f"SHA256: {sha256(output)}")


if __name__ == "__main__":
    main()
