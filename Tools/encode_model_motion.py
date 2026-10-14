"""Encode actual Player frames using their recorded simulation capture times."""
import argparse
import csv
import hashlib
import json
from pathlib import Path

import cv2
from PIL import Image


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--frames", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    folder = args.frames.resolve(strict=True)
    if not folder.is_relative_to(root / "work"):
        raise ValueError("Capture folder must be under project work")
    rows = list(csv.DictReader((folder / "capture-timestamps.csv").open(encoding="utf-8")))
    times = [float(row["elapsedSeconds"]) for row in rows]
    if len(rows) < 2 or any(b <= a for a, b in zip(times, times[1:])):
        raise ValueError("Capture times must be increasing")
    fps = (len(times) - 1) / (times[-1] - times[0])
    output = folder / "player-motion.mp4"
    if output.exists():
        raise FileExistsError(output)
    frames = [folder / row["frame"] for row in rows]
    for path in frames:
        if path.parent.resolve() != folder:
            raise ValueError("Frame path escapes capture folder")
    with Image.open(frames[0]) as first:
        dimensions = first.size
    hashes = [hashlib.sha256(path.read_bytes()).hexdigest() for path in frames]
    writer = cv2.VideoWriter(str(output), cv2.VideoWriter_fourcc(*"mp4v"), fps, dimensions)
    try:
        if not writer.isOpened():
            raise RuntimeError("Motion encoder unavailable")
        for path in frames:
            frame = cv2.imread(str(path))
            if frame is None or (frame.shape[1], frame.shape[0]) != dimensions:
                raise ValueError("Frame dimensions differ")
            writer.write(frame)
    finally:
        writer.release()
    reader = cv2.VideoCapture(str(output))
    decoded = 0
    try:
        while True:
            success, frame = reader.read()
            if not success:
                break
            if (frame.shape[1], frame.shape[0]) != dimensions:
                raise ValueError("Decoded dimensions differ")
            decoded += 1
    finally:
        reader.release()
    if decoded != len(frames):
        raise ValueError("Decoded frame count differs")
    if hashes != [hashlib.sha256(path.read_bytes()).hexdigest() for path in frames]:
        raise ValueError("Original Player frames changed")
    report = {
        "source": str(folder.relative_to(root)),
        "frames": decoded,
        "dimensions": dimensions,
        "encodedFps": fps,
        "captureSeconds": times[-1] - times[0],
        "largestCaptureGapSeconds": max(b - a for a, b in zip(times, times[1:])),
        "sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
        "notes": "Fixed rate uses mean measured capture interval; original timestamps preserve exact timing. "
        "Capture overhead is excluded from the ordinary Player FPS measurement.",
    }
    (folder / "motion-report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
