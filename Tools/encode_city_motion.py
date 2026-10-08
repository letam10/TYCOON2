"""Encode and verify locally captured Player PNG frames without replacing files."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

import cv2
import numpy as np
from PIL import GifImagePlugin, Image


FPS = 10
FRAME_MS = 100
PROJECT_ROOT = Path(__file__).resolve().parent.parent
WORK_ROOT = (PROJECT_ROOT / "work").resolve()
FRAME_PATTERN = re.compile(r"frame-(\d{4,})\.png")


def work_path(value):
    path = Path(value).resolve()
    if not path.is_relative_to(WORK_ROOT):
        raise ValueError(f"Path must be inside {WORK_ROOT}: {path}")
    return path


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def read_frames(directory):
    if not directory.is_dir():
        raise ValueError(f"Frames directory does not exist: {directory}")
    numbered = []
    for path in directory.iterdir():
        match = FRAME_PATTERN.fullmatch(path.name)
        if match and path.is_file():
            work_path(path)
            numbered.append((int(match.group(1)), path))
    numbered.sort(key=lambda entry: entry[0])
    if not numbered:
        raise ValueError("No frame-NNNN.png input files found")
    if len({number for number, _ in numbered}) != len(numbered):
        raise ValueError("Duplicate numeric frame indices")
    frames = []
    dimensions = None
    for number, path in numbered:
        with Image.open(path) as frame:
            if frame.format != "PNG":
                raise ValueError(f"Input is not a PNG: {path}")
            if dimensions is None:
                dimensions = frame.size
            if frame.size != dimensions:
                raise ValueError(f"Frame dimensions differ: {path}")
            frame.verify()
        frames.append({"index": number, "path": path, "sha256": sha256(path)})
    width, height = dimensions
    if width % 2 or height % 2:
        raise ValueError("mp4v requires even frame dimensions to preserve native size")
    return frames, dimensions


def encode(frames, dimensions, mp4_path, gif_file):
    width, height = dimensions
    preview_width = min(width, 960)
    preview_size = (preview_width, max(1, round(height * preview_width / width)))
    writer = cv2.VideoWriter(str(mp4_path), cv2.VideoWriter_fourcc(*"mp4v"), FPS, dimensions)
    try:
        if not writer.isOpened():
            raise RuntimeError("OpenCV could not open the mp4v encoder")
        for index, entry in enumerate(frames):
            with Image.open(entry["path"]) as source:
                rgb = source.convert("RGB")
            writer.write(cv2.cvtColor(np.asarray(rgb), cv2.COLOR_RGB2BGR))
            preview = rgb.resize(preview_size, Image.Resampling.LANCZOS)
            palette_frame = preview.quantize(colors=256)
            if index == 0:
                header, _ = GifImagePlugin.getheader(palette_frame, info={"loop": 0})
                for block in header:
                    gif_file.write(block)
            # Ghi từng frame riêng để Pillow không gộp các ảnh đứng yên giống nhau.
            blocks = GifImagePlugin.getdata(
                palette_frame,
                duration=FRAME_MS,
                disposal=2,
                include_color_table=True,
            )
            for block in blocks:
                gif_file.write(block)
        gif_file.write(b";")
    finally:
        writer.release()
    return preview_size


def verify_mp4(path, dimensions, expected_count):
    capture = cv2.VideoCapture(str(path))
    try:
        if not capture.isOpened():
            raise RuntimeError("Generated MP4 cannot be decoded")
        reported_count = int(capture.get(cv2.CAP_PROP_FRAME_COUNT))
        reported_fps = capture.get(cv2.CAP_PROP_FPS)
        count = 0
        while True:
            success, frame = capture.read()
            if not success:
                break
            if (frame.shape[1], frame.shape[0]) != dimensions:
                raise RuntimeError("Decoded MP4 dimensions differ from source")
            count += 1
        if count != expected_count or reported_count != expected_count:
            raise RuntimeError(f"MP4 frame count mismatch: decoded={count}, reported={reported_count}")
        if abs(reported_fps - FPS) > 0.001:
            raise RuntimeError(f"MP4 frame rate mismatch: {reported_fps}")
        return {"frame_count": count, "dimensions": dimensions, "fps": reported_fps}
    finally:
        capture.release()


def verify_gif(path, dimensions, expected_count):
    durations = []
    with Image.open(path) as animation:
        if animation.n_frames != expected_count:
            raise RuntimeError(f"GIF frame count mismatch: {animation.n_frames}")
        for index in range(animation.n_frames):
            animation.seek(index)
            animation.load()
            if animation.size != dimensions:
                raise RuntimeError("Decoded GIF dimensions differ from preview size")
            duration = animation.info.get("duration")
            if duration != FRAME_MS:
                raise RuntimeError(f"GIF frame {index} duration mismatch: {duration}")
            durations.append(duration)
    return {
        "frame_count": len(durations),
        "dimensions": dimensions,
        "frame_durations_ms": durations,
        "duration_ms": sum(durations),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--frames", required=True, help="PNG frame directory inside project work/")
    parser.add_argument("--output", required=True, help="Output directory inside project work/")
    args = parser.parse_args()
    try:
        source = work_path(args.frames)
        output = work_path(args.output)
        frames, dimensions = read_frames(source)
        paths = {
            "mp4": output / "carry-motion.mp4",
            "gif": output / "carry-motion.gif",
            "manifest": output / "carry-motion-manifest.json",
        }
        for path in paths.values():
            if path.exists() or path.is_symlink():
                raise FileExistsError(f"Refusing to overwrite existing output: {path}")
        output.mkdir(parents=True, exist_ok=True)
        # Đặt chỗ độc quyền; lỗi giữa chừng vẫn giữ file để kiểm tra, không xóa dữ liệu.
        with paths["mp4"].open("xb"):
            pass
        with paths["gif"].open("xb") as gif_file:
            with paths["manifest"].open("x", encoding="utf-8") as manifest_file:
                preview_size = encode(frames, dimensions, paths["mp4"], gif_file)
                gif_file.flush()
                mp4_check = verify_mp4(paths["mp4"], dimensions, len(frames))
                gif_check = verify_gif(paths["gif"], preview_size, len(frames))
                for entry in frames:
                    if sha256(entry["path"]) != entry["sha256"]:
                        raise RuntimeError(f"Source changed during encoding: {entry['path']}")
                manifest = {
                    "schema_version": 1,
                    "source_directory": str(source.relative_to(PROJECT_ROOT)),
                    "fps": FPS,
                    "frame_count": len(frames),
                    "duration_seconds": len(frames) / FPS,
                    "source_dimensions": dimensions,
                    "source_frames": [
                        {"index": entry["index"], "file": entry["path"].name, "sha256": entry["sha256"]}
                        for entry in frames
                    ],
                    "outputs": {},
                }
                for kind, check in (("mp4", mp4_check), ("gif", gif_check)):
                    path = paths[kind]
                    manifest["outputs"][kind] = {
                        "file": path.name,
                        "bytes": path.stat().st_size,
                        "sha256": sha256(path),
                        "verified": check,
                    }
                json.dump(manifest, manifest_file, indent=2)
                manifest_file.write("\n")
        print(f"Verified {len(frames)} frames at {FPS} fps: {output}")
        return 0
    except (OSError, ValueError, RuntimeError, cv2.error) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
