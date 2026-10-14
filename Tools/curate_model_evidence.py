"""Collect local Player captures and reports without including private checkpoints."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET

from package_city import RELEASE_DOCS, checked, input_directory, private_evidence, within


def validate_tests(path):
    summary = ET.parse(path).getroot()
    if summary.tag != "test-run" or int(summary.get("failed", "-1")) != 0:
        raise ValueError("Full-suite XML must have test-run failed=0")
    if int(summary.get("passed", "0")) <= 0 or summary.get("result", "Passed") != "Passed":
        raise ValueError("Full-suite XML must contain passed tests and a passed result")


def curate(root, player, ordinary, output, tests, comparison=None):
    root = root.resolve()
    work = checked(root / "work", root)
    output = checked(Path(output), work)
    if output == work or output.exists():
        raise ValueError("Choose a new evidence directory under work")
    folders = {"models": input_directory(player, work), "ordinary": input_directory(ordinary, work)}
    if comparison is not None:
        folders["comparison"] = input_directory(comparison, work)
    if any(within(output, folder) or within(folder, output) for folder in folders.values()):
        raise ValueError("Evidence output must not overlap input directories")
    model_report = folders["models"] / "city-models-v11-report.json"
    if not model_report.is_file():
        model_report = folders["models"] / "items-vehicles-v8-report.json"
    reports = (model_report,
               folders["ordinary"] / "ordinary-play-report.json")
    for path in reports:
        checked(path, work)
        report = json.loads(path.read_text(encoding="utf-8-sig"))
        if report.get("passed") is not True or report.get("failure") or report.get("runtimeErrors") != []:
            raise ValueError("Player acceptance has not passed or runtimeErrors is not empty")
    tests = Path(tests).resolve(strict=True)
    validate_tests(tests)
    files = []
    suffixes = {".png", ".jpg", ".jpeg", ".mp4", ".webm", ".json", ".csv", ".log", ".txt", ".xml", ".md"}
    for name, folder in folders.items():
        for path in sorted(folder.rglob("*")):
            checked(path, folder)
            if not path.is_file() or path.suffix.lower() not in suffixes or path.name.startswith("frame-"):
                continue
            files.append((path, Path(name) / path.relative_to(folder)))
    files.append((tests, Path("tests") / tests.name))
    for name in RELEASE_DOCS:
        path = checked(root / name, root)
        if not path.is_file():
            raise ValueError(f"Required v10 document is missing: {name}")
        files.append((path, Path(name)))
    files = [(path, relative) for path, relative in files if not private_evidence(relative)]
    if not any(relative.parts[0] == "tests" for _, relative in files):
        raise ValueError("Test XML filename must not be a private evidence name")
    output.mkdir(parents=True)
    entries = []
    for path, relative in files:
        destination = output / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, destination)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        if hashlib.sha256(destination.read_bytes()).hexdigest() != digest:
            raise ValueError("Evidence copy differs")
        entries.append({"path": relative.as_posix(), "sha256": digest, "bytes": path.stat().st_size})
    (output / "evidence-index.json").write_text(json.dumps(entries, indent=2), encoding="utf-8")
    return {"output": str(output), "files": len(entries), "bytes": sum(entry["bytes"] for entry in entries)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player", type=Path, required=True)
    parser.add_argument("--ordinary", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--tests", type=Path, required=True, help="Passed full-suite test-run XML")
    parser.add_argument("--comparison", type=Path, help="Optional A/B FPS evidence directory under work/")
    args = parser.parse_args()
    try:
        result = curate(Path(__file__).resolve().parents[1], args.player, args.ordinary,
                        args.output, args.tests, args.comparison)
    except (OSError, ValueError, ET.ParseError) as error:
        parser.exit(1, f"Evidence curation failed: {error}\n")
    print(json.dumps(result, indent=2))

if __name__ == "__main__":
    main()
