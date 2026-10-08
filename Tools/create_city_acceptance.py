"""Combine reported city acceptance evidence without running the game."""

import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone


ROOT = Path(__file__).resolve().parents[1]
RESOLUTIONS = ((1280, 720), (1920, 1080), (2560, 1440), (3840, 2160))
ORDINARY_FIELDS = (
    "passed mode gpu api width height frames workers customers durationSeconds walkedMetres averageFps "
    "p95FrameMs worstFrameMs averageCpuMs averageGpuMs fullCitySeconds fullCityAverageFps fullCityFrames "
    "timingSamples successfulInteractions near165Fps fixture"
).split()
RESTORE_FIELDS = (
    "sourceLayout restoredLayout sourceItems restoredItems preservedReceipts "
    "sourceCashInHand restoredCashInHand sourceCashInSafe restoredCashInSafe"
).split()
SAMPLE_FIELDS = (
    "screenshot mode requestedWidth requestedHeight attemptedWindowWidth attemptedWindowHeight windowWidth "
    "windowHeight renderedWidth renderedHeight pngWidth pngHeight offscreen nativeWindowValidated renderScale"
).split()
RESTORE_MARKERS = (
    "Giữ nguyên tiền tay và két khi phục hồi",
    "Giữ nguyên tổng từng loại hàng khi phục hồi",
    "Giữ mã giao dịch để chống lặp sau phục hồi",
)


def inside(path):
    path = Path(path).resolve()
    if not path.is_relative_to(ROOT):
        raise ValueError("Input or output is outside the checkout")
    return path


def select(data, fields):
    return {key: data[key] for key in fields if key in data}


def finite(value):
    return type(value) in (int, float) and math.isfinite(value)


def at_least(data, key, minimum):
    return finite(data.get(key)) and data[key] >= minimum


def invalid_constant(value):
    raise ValueError("Nonfinite JSON value: " + value)


class Acceptance:
    def __init__(self):
        self.checks = {}
        self.sources = {}
        self.raw = {}
        self.limitations = [
            "This tool validates supplied reports; it does not run Unity or independently measure gameplay.",
            "165 FPS is the target; the acceptance floor is 148.5 FPS, including the resume full-city interval.",
            "Per-SKU totals and receipt identities rely on explicit successful runtime restore assertions.",
            "The legacy-source hash identifies the supplied file; the restore report contains no source hash.",
            "Source checkout metadata does not prove which revision produced a supplied Player build.",
        ]

    def check(self, name, passed):
        self.checks[name] = bool(passed)

    def record(self, name, path):
        path = inside(path)
        digest = hashlib.sha256()
        with path.open("rb") as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(chunk)
        self.sources[name] = {"path": str(path), "sha256": digest.hexdigest(), "bytes": path.stat().st_size}
        return path

    def read(self, name, path):
        data = json.loads(self.record(name, path).read_text(encoding="utf-8-sig"), parse_constant=invalid_constant)
        if not isinstance(data, dict):
            raise ValueError("Expected a JSON object: " + name)
        return data

    def clean(self, name, data, ordinary=False):
        self.check(name + ".passed", data.get("passed") is True)
        self.check(name + ".runtimeErrors", data.get("runtimeErrors") == [])
        self.check(name + ".failures", data.get("failures", None if ordinary else []) == [])
        self.check(name + ".failure", data.get("failure", "") == "")
        self.check(name + ".errors", data.get("errors", []) == [])

    def ordinary(self, name, folder):
        data = self.read(name, folder / "ordinary-play-report.json")
        self.raw[name] = select(data, ORDINARY_FIELDS)
        self.clean(name, data, ordinary=True)
        self.check(name + ".duration", at_least(data, "durationSeconds", 240))
        self.check(name + ".resolution", (data.get("width"), data.get("height")) == (1920, 1080))
        self.check(name + ".gpu", "RTX 4060" in str(data.get("gpu", "")))
        self.check(name + ".averageFps", at_least(data, "averageFps", 148.5))
        if name == "resume":
            self.check("resume.workers", data.get("workers") == 39)
            self.check("resume.fullCitySeconds", at_least(data, "fullCitySeconds", 180))
            self.check("resume.fullCityAverageFps", at_least(data, "fullCityAverageFps", 148.5))
            self.check("restore.runtimeAssertions", all(x in data.get("completed", []) for x in RESTORE_MARKERS))
        return data

    def qa(self, name, path):
        data = self.read(name, path)
        self.clean(name, data)
        fields = "passed startedUtc finishedUtc graphicsDevice distanceWalked interactions materialSlotsChecked"
        self.raw[name] = select(data, fields.split())
        self.raw[name]["checkCount"] = len(data.get("checks", []))

    def mobility(self, name, folder):
        data = self.read(name + ".mobility", folder / "city-mobility-evidence.json")
        fields = "passed activeWorkers retiringWorkers pendingRetirements professions capability vehicles"
        self.raw[name + ".mobility"] = select(data, fields.split())
        self.check(name + ".mobility.passed", data.get("passed") is True)
        self.check(name + ".mobility.workers", data.get("activeWorkers") == 39)
        self.check(name + ".mobility.retirement", data.get("retiringWorkers") == 0 and
                   data.get("pendingRetirements") == 0)
        self.check(name + ".mobility.professions", data.get("professions") == 26)
        self.check(name + ".mobility.capability", data.get("capability") == 1.5)
        self.check(name + ".mobility.collision", data.get("walkingPassengersHaveCollision") is True)
        vehicles = data.get("vehicles", [])
        self.check(name + ".mobility.rides", len(vehicles) == 3 and
                   all(at_least(v, "boarded", 1) and at_least(v, "alighted", 1) for v in vehicles))

    def resolutions(self, folder):
        data = self.read("resolutions", folder / "physical-resolutions.json")
        samples = data.get("samples", [])
        self.check("resolutions.allSampleFailuresEmpty", all(s.get("failures") == [] for s in samples))
        self.raw["resolutions"] = {"captureMethod": data.get("captureMethod"), "samples": []}
        for width, height in RESOLUTIONS:
            key = f"resolution.{width}x{height}"
            matches = [s for s in samples if (s.get("requestedWidth"), s.get("requestedHeight")) == (width, height)]
            self.check(key + ".presentOnce", len(matches) == 1)
            for sample in matches:
                self.raw["resolutions"]["samples"].append(select(sample, SAMPLE_FIELDS))
                self.check(key + ".failures", sample.get("failures") == [])
                self.check(key + ".renderScale", sample.get("renderScale") == 1)
                dimensions = (width, height)
                self.check(key + ".rendered", (sample.get("renderedWidth"), sample.get("renderedHeight")) == dimensions)
                self.check(key + ".reportedPng", (sample.get("pngWidth"), sample.get("pngHeight")) == dimensions)
                screenshot = sample.get("screenshot")
                path = inside(folder / screenshot) if isinstance(screenshot, str) else None
                valid_png = False
                if path and path.is_file() and path.suffix.lower() == ".png":
                    self.record(key + ".png", path)
                    with path.open("rb") as stream:
                        header = stream.read(24)
                    valid_png = (len(header) == 24 and header[:8] == b"\x89PNG\r\n\x1a\n"
                                 and header[8:16] == b"\x00\x00\x00\rIHDR"
                                 and struct.unpack(">II", header[16:24]) == dimensions)
                self.check(key + ".actualPngDimensions", valid_png)
                if sample.get("nativeWindowValidated") is not True or sample.get("offscreen") is not False:
                    self.limitations.append(
                        f"{width}x{height}: nativeWindowValidated={sample.get('nativeWindowValidated')}, "
                        f"offscreen={sample.get('offscreen')}; no full native-window acceptance claim."
                    )

    def restore(self, folder, legacy_source):
        data = self.read("restore", folder / "city-restore.json")
        self.raw["restore"] = select(data, RESTORE_FIELDS)
        self.record("legacySource", legacy_source)
        self.check("restore.layout", data.get("sourceLayout") == 1 and data.get("restoredLayout") == 2)
        for field in ("Items", "CashInHand", "CashInSafe"):
            left = data.get("source" + field)
            right = data.get("restored" + field)
            self.check("restore." + field, finite(left) and finite(right) and left == right)
        self.check("restore.receipts", type(data.get("preservedReceipts")) is int and data["preservedReceipts"] >= 0)

    def tests_and_build(self, tests, build):
        root = ET.parse(self.record("tests", tests)).getroot()
        fields = "result total passed failed inconclusive skipped duration start-time end-time".split()
        self.raw["tests"] = select(root.attrib, fields)
        self.check("tests.passed", root.get("result") == "Passed" and root.get("failed") == "0")
        self.check("tests.executed", int(root.get("passed", "0")) > 0)
        self.check("tests.noFailedCases", not any(n.get("result") == "Failed" for n in root.iter()))
        data = self.read("build", build / "build-report.json")
        self.raw["build"] = select(data, "result builtUtc bytes seconds errors warnings".split())
        self.check("build.succeeded", data.get("result") == "Succeeded" and data.get("errors") == 0)
        for executable in sorted(build.glob("*.exe")):
            self.record("build." + executable.name, executable)

    def run(self, args):
        fresh = self.ordinary("fresh", args.fresh)
        resume = self.ordinary("resume", args.resume)
        self.mobility("fresh", args.fresh)
        self.mobility("resume", args.resume)
        self.restore(args.resume, args.legacy_source)
        self.qa("visual", args.visual / "physical-visuals-report.json")
        self.qa("migration", args.migration / "physical-migration-report.json")
        self.resolutions(args.visual)
        self.tests_and_build(args.tests, args.build)
        version = ROOT / "ProjectSettings" / "ProjectVersion.txt"
        self.record("projectVersion", version)
        self.raw["software"] = {
            "checkoutUnityVersion": version.read_text(encoding="utf-8-sig").splitlines()[0],
            "freshGraphicsApi": fresh.get("api"),
            "resumeGraphicsApi": resume.get("api"),
        }
        self.raw["hardware"] = {"freshGpu": fresh.get("gpu"), "resumeGpu": resume.get("gpu")}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("fresh", "resume", "visual", "migration", "tests", "build", "legacy-source", "output"):
        parser.add_argument("--" + name, required=True, type=Path)
    args = parser.parse_args()
    try:
        for name, path in vars(args).items():
            setattr(args, name, inside(path))
        allowed = any(args.output.is_relative_to(ROOT / folder) for folder in ("QA", "work"))
        if not allowed or args.output.suffix.lower() != ".json":
            raise ValueError("Output must be a new JSON file in QA or work")
        if args.output.exists():
            raise ValueError("Refusing to overwrite existing output")
    except ValueError as error:
        parser.error(str(error))
    result = Acceptance()
    try:
        result.run(args)
    except (OSError, ValueError, TypeError, KeyError, AttributeError, ET.ParseError) as error:
        result.check("evidence.readableAndValid", False)
        result.limitations.append("Evidence could not be fully processed: " + type(error).__name__)
    # Chỉ xuất số liệu QA cho phép; không sao chép save, journal hoặc dữ liệu bí mật.
    report = {
        "passed": bool(result.checks) and all(result.checks.values()),
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "evidenceKind": "supplied reports; provenance must be reviewed separately",
        "targetFps": 165,
        "minimumAcceptedFps": 148.5,
        "checks": result.checks,
        "software": result.raw.pop("software", {}),
        "hardware": result.raw.pop("hardware", {}),
        "metrics": result.raw,
        "sources": result.sources,
        "limitations": result.limitations,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2, allow_nan=False)
        stream.write("\n")
    print(json.dumps({"passed": report["passed"], "output": str(args.output)}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
