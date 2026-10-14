"""Validate and index actual V11 Player captures without claiming artistic approval."""
import argparse
import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--player", type=Path, required=True)
    parser.add_argument("--rides", type=Path, required=True)
    parser.add_argument("--ordinary", type=Path, required=True)
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--tests", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    sources = {}

    def record(path):
        path = path.resolve(strict=True)
        if not path.is_relative_to(root):
            raise ValueError("Evidence must remain in checkout")
        key = path.relative_to(root).as_posix()
        sources[key] = {"bytes": path.stat().st_size,
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
        return key

    def read(path):
        record(path)
        return json.loads(path.read_text(encoding="utf-8-sig"))

    player = read(args.player / "city-models-v11-report.json")
    rides = read(args.rides / "items-vehicles-v8-report.json")
    ordinary = read(args.ordinary / "ordinary-play-report.json")
    water = read(args.player / "water-detail-performance.json")
    live = read(args.rides / "vehicle-live-facts.json")
    build = read(args.build / "build-report.json")
    record(args.tests)
    tests = ET.parse(args.tests).getroot().attrib
    for report in (player, rides, ordinary):
        if not report.get("passed") or report.get("failure") or report.get("runtimeErrors"):
            raise ValueError("Player contains failure")
    if int(tests.get("failed", 0)) or int(tests.get("skipped", 0)) or build["result"] != "Succeeded":
        raise ValueError("Native tests or build did not pass")
    if ordinary.get("mode") != "ordinary gameplay with virtual gamepad, live simulation and persistence":
        raise ValueError("A diagnostic cannot replace actual ordinary city tour")
    names = set(player["screenshots"])
    surrounding = ("front", "right", "rear", "left", "three-quarter", "rear-quarter",
                   "front-left", "rear-right", "top")
    placed = ("front", "right", "rear", "left", "top")
    landmarks = ("PoliceStation", "FireStation", "SeasideBar", "EntertainmentHall", "Playground",
                 "FoundersMonument", "Harbor", "Beach", "CoffeeHouse", "Bank", "Hotel", "Theater", "Marina")

    def require(prefix, angles):
        for angle in angles:
            if f"{prefix}-{angle}.png" not in names:
                raise ValueError("Missing actual angle: " + prefix + "-" + angle)

    for landmark in landmarks:
        require("city-single-" + landmark, surrounding)
        require("city-placed-" + landmark, placed)
    for prop in ("Bench", "Planter", "Lamp", "Signal", "Railing", "FishingBoat", "Yacht", "CargoBoat",
                 "BeachUmbrella", "BeachLounger"):
        require("city-prop-" + prop, surrounding)
    require("retail-prop-supply_crate", surrounding)
    for setting in ("Park", "StreetFurniture", "TrafficSignal"):
        require("city-placed-" + setting, placed)
    owners = sorted(name.removeprefix("world-model-").removesuffix("-front.png")
                    for name in names if name.startswith("world-model-") and name.endswith("-front.png"))
    if not owners:
        raise ValueError("Missing station props")
    for owner in owners:
        require("world-single-" + owner, surrounding)
        require("world-model-" + owner, placed)
    vehicles = [ride for group in ("trains", "publicVehicles", "roadVehicles") for ride in live[group]]
    if len(vehicles) != 11 or not all(x["boardings"] > 0 and x["alightings"] > 0 for x in vehicles):
        raise ValueError("Missing eleven completed real boarding and alighting sequences")
    if [run["detail"] for run in water["runs"]] != [0, 1, 1, 0]:
        raise ValueError("Water must have ABBA sequence")
    for run in water["runs"]:
        if run["seconds"] < 15 or run["frames"] <= 0:
            raise ValueError("Water measurement was incomplete")
    for folder, report in ((args.player, player), (args.ordinary, ordinary)):
        for name in report["screenshots"]:
            path = folder / name
            if path.parent.resolve() != folder.resolve():
                raise ValueError("Screenshot path escapes evidence directory")
            with Image.open(path) as capture:
                capture.verify()
            with Image.open(path) as capture:
                extrema = capture.convert("RGB").getextrema()
                if max(high for _, high in extrema) < 8:
                    raise ValueError("Black image: " + name)
            record(path)
    record(args.build / "TYCOON2.exe")
    motion = args.player / "water-motion-frames"
    record(motion / "capture-timestamps.csv")
    record(motion / "player-motion.mp4")
    record(motion / "motion-report.json")
    output = args.out.resolve()
    if not output.is_relative_to(root / "work"):
        raise ValueError("Output must remain under work")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps({
        "schema": 1,
        "scope": "V11 captures, native test/build and real Player evidence; artistic review is separate",
        "tests": tests,
        "build": build,
        "landmarks": list(landmarks),
        "stationOwners": owners,
        "modelScreenshots": len(names),
        "vehicleCount": len(vehicles),
        "water": water,
        "ordinary": ordinary,
        "sources": sources,
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(output)


if __name__ == "__main__":
    main()
