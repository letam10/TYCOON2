"""Compare fixed shadows in ground coordinates across actual Player camera movement."""
import argparse
import csv
import json
from pathlib import Path

import cv2
import numpy as np


def analyze(folder):
    rows = list(csv.DictReader((folder / "shadow-stability.csv").open(encoding="utf-8")))
    destination = np.float32([[0, 191], [239, 191], [239, 0], [0, 0]])
    x = np.linspace(-5, 5, 240)[None, :]
    z = np.linspace(4, -4, 192)[:, None]
    # Loại vùng thân khối hộp có thể che mặt đất khi camera thay đổi.
    ground = ~((x > -1.3) & (x < 2.8) & (z > -1.3) & (z < 3.2))
    results = {}
    for variant in dict.fromkeys(row["variant"] for row in rows):
        selected = [row for row in rows if row["variant"] == variant]
        frames = []
        valid = ground.copy()
        for row in selected:
            path = folder / row["frame"]
            if path.parent.resolve() != folder:
                raise ValueError("Frame escapes evidence folder")
            frame = cv2.imread(str(path))
            if frame is None:
                raise ValueError("Missing Player frame: " + str(path))
            height, width = frame.shape[:2]
            source = np.float32([[float(row[f"x{i}"]), float(row[f"y{i}"])] for i in range(4)])
            matrix = cv2.getPerspectiveTransform(source, destination)
            gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
            projected = cv2.warpPerspective(gray, matrix, (240, 192))
            screen_mask = np.zeros((height, width), np.uint8)
            screen_mask[int(height * .13):int(height * .85), int(width * .03):int(width * .97)] = 255
            valid &= cv2.warpPerspective(screen_mask, matrix, (240, 192)) > 254
            frames.append(projected)
        if len(frames) < 30 or valid.sum() < 500:
            raise ValueError("Insufficient common ground samples")
        stack = np.float32(frames)
        median = np.median(stack, axis=0)
        shadow = valid & (median < np.median(median[valid]) - 15)
        if shadow.sum() < 50:
            raise ValueError("Insufficient visible shadow samples")
        differences = np.abs(np.diff(stack, axis=0))[:, shadow]
        results[variant] = {
            "frames": len(frames),
            "commonGroundPixels": int(valid.sum()),
            "shadowPixels": int(shadow.sum()),
            "shadowTemporalStdP95": float(np.percentile(stack[:, shadow].std(axis=0), 95)),
            "shadowFrameDifferenceP95": float(np.percentile(differences, 95)),
            "largestMeanFrameDifference": float(differences.mean(axis=1).max()),
            "captureSeconds": float(selected[-1]["elapsedSeconds"]),
        }
        cv2.imwrite(str(folder / f"shadow-rectified-{variant}.png"), median.astype(np.uint8))
    report = {
        "method": "Actual screen frames rectified with recorded ground projection; static caster and light.",
        "limitations": "Controlled planar receiver excludes cube occlusion and HUD; not a full city FPS test.",
        "variants": results,
    }
    (folder / "shadow-stability-analysis.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(analyze(args.evidence.resolve(strict=True)), indent=2))


if __name__ == "__main__":
    main()
