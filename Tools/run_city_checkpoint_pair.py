"""Run sequential visible Player comparisons with immutable checkpoint provenance."""
import argparse
import csv
import hashlib
import json
import re
import subprocess
from datetime import datetime, timezone
from pathlib import Path


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def inside(path, root):
    resolved = path.resolve()
    if root not in resolved.parents:
        raise ValueError(f"Path outside workspace: {path}")
    return resolved


def snapshot_timings(directory):
    with (directory / "operation-timings.csv").open(encoding="utf-8-sig", newline="") as source:
        rows = list(csv.DictReader(source))
    return [row for row in rows if row.get("operation") == "Checkpoint.TransactionSnapshot"]


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def run_one(root, exe, checkpoint, expected_hash, label, destination):
    if sha256(checkpoint) != expected_hash:
        raise ValueError("Input checkpoint changed before run")
    assembly = exe.parent / "GameAssembly.dll"
    binary_hash = sha256(assembly)
    command = [
        "pwsh", "-NoProfile", "-File", str(root / "Tools/run_city_play.ps1"),
        "-BuildPath", str(exe.relative_to(root)), "-Visible", "-ShowFps", "-Manual165",
        "-Profile", "-ModOnly", "-LoadPath", str(checkpoint),
    ]
    started = datetime.now(timezone.utc).isoformat()
    # Helper ẩn, nhưng Player được runner hiện hành mở Normal để thực sự render.
    completed = subprocess.run(
        command, cwd=root, text=True, encoding="utf-8", errors="replace",
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
    )
    (destination / f"{label}-runner.log").write_text(completed.stdout, encoding="utf-8")
    matches = re.findall(r"(?m)^([^\r\n]*ordinary-play-\d{8}T\d{9}Z)\s*$", completed.stdout)
    if len(matches) != 1:
        raise ValueError(f"Expected one Player output directory, got {len(matches)}")
    directory = inside(Path(matches[0]), root / "work")
    if sha256(checkpoint) != expected_hash or sha256(assembly) != binary_hash:
        raise ValueError("Input checkpoint or binary changed during run")
    result = {
        "label": label,
        "startedUtc": started,
        "completedUtc": datetime.now(timezone.utc).isoformat(),
        "executable": str(exe.relative_to(root)),
        "assemblySha256": binary_hash,
        "checkpointSha256": expected_hash,
        "exitCode": completed.returncode,
        "outputDirectory": str(directory.relative_to(root)),
        "settings": read_json(directory / "settings.json"),
        "report": read_json(directory / "ordinary-play-report.json"),
        "captureValidity": read_json(directory / "capture-validity.json"),
        "npcEvidence": read_json(directory / "npc-motion-evidence.json"),
        "transactionSnapshotTimings": snapshot_timings(directory),
    }
    (destination / f"{label}.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", required=True, type=Path)
    parser.add_argument("--candidate", required=True, type=Path)
    parser.add_argument("--checkpoint", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    baseline = inside(args.baseline, root)
    candidate = inside(args.candidate, root)
    checkpoint = inside(args.checkpoint, root / "work")
    destination = inside(args.output, root / "work")
    for exe in (baseline, candidate):
        if not exe.is_file() or not (exe.parent / "GameAssembly.dll").is_file():
            parser.error(f"Expected IL2CPP Player: {exe}")
    destination.mkdir(exist_ok=False, parents=True)
    expected_hash = sha256(checkpoint)
    results = []
    for label, exe in (("baseline", baseline), ("candidate", candidate)):
        results.append(run_one(root, exe, checkpoint, expected_hash, label, destination))
    if results[0]["settings"] != results[1]["settings"]:
        raise ValueError("Player settings differ between runs")
    summary = {
        "schemaVersion": 1,
        "mode": "Same checkpoint, sequential visible IL2CPP Player, Manual165, profile, ModOnly",
        "checkpointSha256": expected_hash,
        "sameSettings": True,
        "stable165Accepted": False,
        "runs": results,
    }
    (destination / "comparison.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps({"output": str(destination), "exitCodes": [row["exitCode"] for row in results]}))


if __name__ == "__main__":
    main()
