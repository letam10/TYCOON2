"""Record verified local model, settings, dining and ordinary Player evidence."""
import argparse
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image
from package_city import debug_file


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--player", type=Path, required=True)
    parser.add_argument("--ordinary", type=Path, required=True)
    parser.add_argument("--before", type=Path, required=True)
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--tests", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    sources = {}

    def record(path):
        path = path.resolve(strict=True)
        if not path.is_relative_to(root):
            raise ValueError("Evidence must remain inside the checkout")
        key = path.relative_to(root).as_posix()
        sources[key] = {"sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "bytes": path.stat().st_size}
        return key

    def read(path):
        record(path)
        return json.loads(path.read_text(encoding="utf-8-sig"))

    player = read(args.player / "items-vehicles-v8-report.json")
    live = read(args.player / "vehicle-live-facts.json")
    ordinary = read(args.ordinary / "ordinary-play-report.json")
    before = read(args.before / "ordinary-play-report.json")
    build = read(args.build / "build-report.json")
    tests_path = args.tests
    record(tests_path)
    tests = ET.parse(tests_path).getroot().attrib
    for data in (player, ordinary):
        if not data.get("passed") or data.get("runtimeErrors", []) or data.get("failure", ""):
            raise ValueError("Player evidence contains failure")
    ordinary_screenshots = []
    for name in ordinary["screenshots"]:
        with Image.open(args.ordinary / name) as capture:
            extremes = capture.convert("RGB").getextrema()
            if max(high for _, high in extremes) < 8:
                raise ValueError("Black capture cannot prove visible rendering: " + name)
            dimensions = capture.size
        ordinary_screenshots.append({"path": record(args.ordinary / name),
                                     "width": dimensions[0], "height": dimensions[1]})
    if ordinary.get("mode") != "ordinary gameplay with virtual gamepad, live simulation and persistence":
        raise ValueError("A short diagnostic cannot replace full ordinary Player acceptance")
    if int(tests["failed"]) or build["result"] != "Succeeded":
        raise ValueError("Tests or build failed")
    for group in ("trains", "publicVehicles", "roadVehicles"):
        if not all(ride["boardings"] > 0 and ride["alightings"] > 0 for ride in live[group]):
            raise ValueError("Missing completed real vehicle ride")
    if sum(len(live[group]) for group in ("trains", "publicVehicles", "roadVehicles")) != 11:
        raise ValueError("Wrong live vehicle count")
    screenshots = []
    names = set(player["screenshots"])
    goods = sorted(name.removeprefix("item-v8-").removesuffix("-48.png")
                   for name in names if name.startswith("item-v8-") and name.endswith("-48.png"))
    angles = ("front", "rear", "left", "right", "three-quarter", "top", "rear-quarter", "front-left", "rear-right")
    if len(goods) != 27:
        raise ValueError("All27goods must be captured")
    for item in goods:
        for angle in angles:
            for prefix in (f"item-single-{item}", f"item-angles-{item}-48"):
                if prefix + "-" + angle + ".png" not in names:
                    raise ValueError("Missing actual model angle")
    kinds = ("Sedan", "Taxi", "Bus", "Coach", "SmallTruck", "CargoTruck", "Tanker", "Luxury", "Sports", "Train")
    for kind in kinds:
        for angle in angles:
            if f"vehicle-angles-{kind}-{angle}.png" not in names:
                raise ValueError("Missing actual vehicle angle")
    placed_vehicles = sorted(name.removeprefix("vehicle-placed-").removesuffix("-front.png")
                             for name in names if name.startswith("vehicle-placed-") and name.endswith("-front.png"))
    if len(placed_vehicles) != 11:
        raise ValueError("Missing eleven actual placed vehicles")
    for vehicle in placed_vehicles:
        for angle in ("front", "right", "rear", "left", "top"):
            if f"vehicle-placed-{vehicle}-{angle}.png" not in names:
                raise ValueError("Missing placed vehicle angle")
    for shape in ("Round", "Square"):
        if f"dining-customer-{shape}-three-quarter.png" not in names:
            raise ValueError("Missing actual seated customer")
    world_owners = sorted(name.removeprefix("world-model-").removesuffix("-front.png")
                          for name in names if name.startswith("world-model-") and name.endswith("-front.png"))
    if not world_owners:
        raise ValueError("Missing world props")
    for owner in world_owners:
        for angle in angles:
            if f"world-single-{owner}-{angle}.png" not in names:
                raise ValueError("Missing unoccluded world model angle")
    for owner in world_owners:
        for angle in ("front", "right", "rear", "left", "top"):
            if f"world-model-{owner}-{angle}.png" not in names:
                raise ValueError("Missing placed model angle")
    for landmark in ("PoliceStation", "FireStation", "SeasideBar", "EntertainmentHall",
                     "Playground", "FoundersMonument", "Harbor", "Beach"):
        for angle in angles:
            if f"city-single-{landmark}-{angle}.png" not in names:
                raise ValueError("Missing isolated landmark angle")
        for angle in ("front", "right", "rear", "left", "top"):
            if f"city-placed-{landmark}-{angle}.png" not in names:
                raise ValueError("Missing placed landmark angle")
    for name in player["screenshots"]:
        path = args.player / name
        with Image.open(path) as picture:
            dimensions = picture.size
            picture.verify()
        screenshots.append({"path": record(path), "width": dimensions[0], "height": dimensions[1]})
    for name in ("ordinary-play-timeline.csv", "ordinary-play-hitches.csv", "operation-timings.csv",
                 "city-mobility-evidence.json", "ordinary-play-actors.json"):
        path = args.ordinary / name
        if path.exists():
            record(path)
    record(args.build / "TYCOON2.exe")
    build_files = []
    for path in sorted(args.build.rglob("*")):
        if path.is_file() and not debug_file(path.relative_to(args.build)):
            build_files.append(record(path))
    fields = (
        "durationSeconds frames averageFps p95FrameMs p99FrameMs worstFrameMs averageCpuMs averageGpuMs "
        "fullCitySeconds fullCityAverageFps workers customers walkedMetres successfulInteractions "
        "runtimeErrors width height gpu fixture timingSamples displaySync displayRefreshHz configuredFpsLimit "
        "gpuOcclusionEnabled shadowCascades renderScale stable165FrameBudget"
    ).split()
    select = lambda data: {key: data[key] for key in fields if key in data}
    report = {
        "schema": 1,
        "date": "2026-10-09",
        "project": "TYCOON2",
        "tests": tests,
        "build": build,
        "buildFiles": build_files,
        "player": {
            "passed": player["passed"],
            "checks": len(player["checks"]),
            "runtimeErrors": player["runtimeErrors"],
            "startedUtc": player["startedUtc"],
            "finishedUtc": player["finishedUtc"],
            "gpu": player["graphicsDevice"],
        },
        "liveVehicles": live,
        "ordinaryPlayer": select(ordinary),
        "ordinaryScreenshots": ordinary_screenshots,
        "historicalDiagnostic": select(before),
        "historicalDiagnosticComparable": False,
        "historicalDiagnosticMode": before.get("mode", "unclassified historical run"),
        "visualCoverage": {
            "goods": 27,
            "cashAndCrate": 2,
            "vehicleKinds": 10,
            "worldOwners": world_owners,
            "angles": list(angles),
            "placedAngles": ["front", "right", "rear", "left", "top"],
            "landmarks": 8,
            "goodsCounts": [1, 24, 48],
            "screenshots": screenshots,
        },
        "limitations": [
            "4K is an actual offscreen render; native UI was exercised at720p1080p1440p.",
            "FPS comes from ordinary simulation1x Player without profiler or bulk image capture.",
            "165FPS is the user target; frame cap and successful tests do not guarantee a stable165FPS.",
            "Visual review covers supplied captures and actual rides; it does not prove all possible camera paths.",
            "Historical diagnostic is not a matched visible full-city FPS baseline and is not a performance gain claim.",
        ],
        "sourceEvidence": sources,
    }
    output = args.output.resolve()
    if not output.is_relative_to(root) or output.exists():
        raise ValueError("New report must be inside checkout and must not overwrite existing evidence")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(output), "checks": report["player"]["checks"],
                      "screenshots": len(screenshots), "fps": ordinary.get("averageFps")}, indent=2))


if __name__ == "__main__":
    main()
