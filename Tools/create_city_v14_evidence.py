"""Prepare fresh V14 evidence locally; never modify historical packages or private saves."""
import argparse
import hashlib
import json
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image

from package_city import EVIDENCE_SUFFIXES, RELEASE_DOCS, checked, private_evidence


V14 = Path("work/city-model-v14-20261010")
V13 = Path("work/city-model-v13-20261010/Evidence")
RUNS = tuple(Path("work/city-expansion-20261008") / name for name in (
    "ordinary-play-20261010T085522987Z", "ordinary-play-20261010T081545725Z",
))
HARDWARE = Path("work/fps-hardware-20261010")
ACCEPTANCE = Path("QA/city-model-v14-acceptance-20261010.json")
BLOCK_BYTES = 1024 * 1024


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def fingerprint(path):
    digest = hashlib.sha256()
    size = 0
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(BLOCK_BYTES), b""):
            size += len(block)
            digest.update(block)
    return {"bytes": size, "sha256": digest.hexdigest()}


def eligible(relative):
    return (relative.suffix.lower() in EVIDENCE_SUFFIXES and not private_evidence(relative)
            and relative.name.casefold() != "agents.md"
            and "tools" not in [part.casefold() for part in relative.parts]
            and not relative.name.startswith("evidence-index"))


def validate(root):
    tour = checked(root / RUNS[0], root)
    report = read_json(tour / "ordinary-play-report.json")
    gate = read_json(tour / "capture-validity.json")
    require(report.get("passed") is True and report.get("runtimeErrors") == []
            and report.get("failures") == [], "Current tour did not pass without runtime errors")
    require(report.get("mode") == "ordinary gameplay with virtual gamepad, live simulation and persistence",
            "Current evidence must be an ordinary gameplay tour")
    require(gate.get("passed") is True and gate.get("imageCount") == 42
            and gate.get("failedImageCount") == 0, "Expected 42 passing capture checks")
    names = report.get("screenshots", [])
    rows = gate.get("images", [])
    require(len(names) == len(set(names)) == len(rows) == 42, "Expected 42 distinct tour images")
    require({row["file"] for row in rows} == set(names), "Capture gate differs from tour screenshots")
    require(checked(Path(gate["runDirectory"]), root) == tour, "Capture gate belongs to another run")
    for row in rows:
        relative = Path(row["file"])
        require(len(relative.parts) == 1 and eligible(relative), "Invalid screenshot path")
        path = checked(tour / relative, tour)
        require(row.get("accepted") is True, f"Capture rejected: {relative}")
        with Image.open(path) as capture:
            capture.verify()
        with Image.open(path) as capture:
            require(capture.size == (row["width"], row["height"]), f"Capture size changed: {relative}")
            pixels = capture.convert("RGB")
            extrema = pixels.getextrema()
            require(max(high for _, high in extrema) > 8 and any(low != high for low, high in extrema),
                    f"Blank capture: {relative}")
    tests = ET.parse(checked(root / V14 / "full-tests-final.xml", root)).getroot()
    expected = {"total": 896, "passed": 896, "failed": 0, "skipped": 0, "inconclusive": 0}
    require(tests.get("result") == "Passed"
            and all(int(tests.get(key, -1)) == value for key, value in expected.items()),
            "Native test totals must be 896 passed, zero failed, skipped or inconclusive")
    cases = list(tests.iter("test-case"))
    require(len(cases) == 896 and all(case.get("result") == "Passed" for case in cases),
            "Native test cases do not match passing totals")
    build = read_json(checked(root / V14 / "Candidate/build-report.json", root))
    require(build.get("result") == "Succeeded" and build.get("errors") == 0, "Current build failed")
    acceptance = read_json(checked(root / ACCEPTANCE, root))
    require(acceptance.get("objectiveComplete") is False
            and acceptance.get("fps", {}).get("stable165") is False
            and acceptance["fps"].get("physicalPresentation165HzVerified") is False
            and report.get("stable165FrameBudget") is False, "Do not promote performance acceptance")
    for entry in acceptance.get("sourceEvidence", []):
        require(fingerprint(checked(root / entry["path"], root))["sha256"] == entry["sha256"],
                f"Acceptance source hash changed: {entry['path']}")
    return report, expected, build


def select_files(root):
    selected = {}

    def add_tree(folder, prefix):
        folder = checked(folder, root)
        require(folder.is_dir(), f"Missing evidence directory: {folder}")
        for source in sorted(folder.rglob("*")):
            checked(source, folder)
            relative = source.relative_to(folder)
            target = prefix / relative
            if source.is_file() and eligible(target):
                selected[target.as_posix()] = source

    add_tree(root / V13, Path())
    for folder in (*RUNS, HARDWARE):
        add_tree(root / folder, folder)
    required = (*RELEASE_DOCS, (V14 / "full-tests-final.xml").as_posix(),
                (V14 / "full-tests-final.log").as_posix(), (V14 / "Candidate/build-report.json").as_posix(),
                (V14 / "runtime-source-comparison.json").as_posix())
    for name in required:
        path = checked(root / name, root)
        require(path.is_file() and eligible(Path(name)), f"Missing or ineligible required evidence: {name}")
        selected[name] = path
    diagnosis = Path("Docs/V14_HITCH_DIAGNOSIS.md")
    if checked(root / diagnosis, root).is_file():
        selected[diagnosis.as_posix()] = root / diagnosis
    require(len({name.casefold() for name in selected}) == len(selected), "Duplicate Windows evidence paths")
    return selected


def write_json(path, data):
    text = json.dumps(data, ensure_ascii=False, indent=2) + "\n"
    require(len(text.splitlines()) < 300, f"Generated metadata exceeds line limit: {path}")
    with path.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(text)


def prepare(root: Path) -> dict:
    root = root.resolve(strict=True)
    destination = checked(root / V14 / "Evidence", root)
    if destination.exists():
        raise FileExistsError(f"Refusing to overwrite existing evidence: {destination}")
    report, tests, build = validate(root)
    selected = select_files(root)
    required_bytes = sum(path.stat().st_size for path in selected.values())
    require(shutil.disk_usage(root).free > required_bytes + 64 * BLOCK_BYTES,
            f"Insufficient free space for {required_bytes} evidence bytes plus metadata reserve")
    windows = []
    for name, path in sorted(selected.items()):
        if name.startswith(HARDWARE.as_posix() + "/") and path.name == "hardware-report.json":
            data = read_json(path)
            windows.append({"path": name, **{key: data[key] for key in (
                "scope", "processId", "sampleCount", "measuredSeconds", "firstSampleUtc", "lastSampleUtc",
                "presentation165HzVerified", "cpuTemperatureAvailable", "cpuActualClockAvailable",
            ) if key in data}})
    # Tạo mới và giữ nguyên phần đã sao chép nếu lỗi để không xóa nhầm bằng chứng.
    destination.mkdir(parents=True, exist_ok=False)
    records = []
    for name, source in sorted(selected.items()):
        target = checked(destination / name, destination)
        target.parent.mkdir(parents=True, exist_ok=True)
        before = source.stat()
        digest = hashlib.sha256()
        size = 0
        with source.open("rb") as origin, target.open("xb") as output:
            for block in iter(lambda: origin.read(BLOCK_BYTES), b""):
                output.write(block)
                digest.update(block)
                size += len(block)
        after = source.stat()
        require((before.st_size, before.st_mtime_ns) == (after.st_size, after.st_mtime_ns)
                and size == before.st_size, f"Source changed during copy: {source}")
        expected = {"bytes": size, "sha256": digest.hexdigest()}
        require(fingerprint(target) == expected, f"Copied evidence verification failed: {name}")
        records.append({"path": name, **expected})
    summary = {
        "schema": 14, "objectiveComplete": False, "functionalPlayerPassed": True,
        "stable165": False, "physicalPresentation165HzVerified": False,
        "tests": tests, "build": build, "captureCount": 42,
        "tour": {"path": RUNS[0].as_posix(), "durationSeconds": report["durationSeconds"],
                 "averageFps": report["averageFps"], "p95FrameMs": report["p95FrameMs"],
                 "p99FrameMs": report["p99FrameMs"], "worstFrameMs": report["worstFrameMs"]},
        "stationaryDiagnostic": {"path": RUNS[1].as_posix(), "scope": "Separate short standing run"},
        "hardwareWindows": windows,
        "scope": "Full inherited model evidence plus current functional, native test, build and hardware evidence",
        "limitations": [
            "Inherited V13 evidence retains historical labels and acceptance fields.",
            "Current RELEASE_DOCS and QA acceptance are copied byte for byte.",
            "Hardware windows remain separate; none represents the entire tour.",
            "Engine timing and nonblank images do not verify physical display cadence.",
            "Stable 165 FPS and actual 165 Hz presentation remain unaccepted; full objective remains open.",
        ],
    }
    summary_path = destination / "v14-evidence-summary.json"
    write_json(summary_path, summary)
    records.append({"path": summary_path.name, **fingerprint(summary_path)})
    records.sort(key=lambda entry: entry["path"])
    chunks = []
    # Tách mục lục để từng file dễ kiểm tra và luôn dưới 300 dòng.
    for offset in range(0, len(records), 40):
        part = records[offset:offset + 40]
        path = destination / f"evidence-index-{offset // 40 + 1:03d}.json"
        write_json(path, {"schema": 1, "files": part})
        chunks.append({"path": path.name, "fileCount": len(part), **fingerprint(path)})
    index = {
        "schema": 14,
        "scope": "All copied evidence and V14 summary; chunks hashed here; root index excludes its own hash",
        "pathBase": "Evidence", "fileCount": len(records),
        "bytes": sum(entry["bytes"] for entry in records),
        "stable165": False, "physicalPresentation165HzVerified": False,
        "chunks": chunks,
    }
    write_json(destination / "evidence-index.json", index)
    return {"evidenceDirectory": str(destination), **index}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    print(json.dumps(prepare(args.root), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
