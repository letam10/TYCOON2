"""Verify existing delivery archives and extract Windows without executing Player."""
import argparse
import hashlib
import json
import re
import stat
import zipfile
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image

from package_city import EVIDENCE_SUFFIXES, RELEASE_DOCS, SOURCE_SUFFIXES


BASE = Path("work/city-model-v14-20261010")
PACKAGES = ("Windows.zip", "Source.zip", "Evidence.zip")
BLOCK = 1024 * 1024
LFS = b"version https://git-lfs.github.com/spec/v1"
SOURCE = SOURCE_SUFFIXES | {".ps1", ".sh", ".js", ".ts", ".shader", ".hlsl", ".cginc"}
DEBUG = {".pdb", ".mdb", ".debug", ".bak", ".tmp", ".log"}
DEVICES = {"con", "prn", "aux", "nul", "conin$", "conout$"}
DEVICES |= {f"{prefix}{number}" for prefix in ("com", "lpt") for number in "123456789¹²³"}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def safe_name(name):
    require(isinstance(name, str) and bool(name), "Empty or invalid entry name")
    parts = name.split("/")
    require(not re.search(r'[\\<>:"|?*\x00-\x1f]', name), f"Unsafe entry: {name!r}")
    for part in parts:
        require(part not in {"", ".", ".."} and part.rstrip(" .") == part, f"Unsafe entry: {name!r}")
        require(part.split(".")[0].casefold() not in DEVICES, f"Reserved Windows entry: {name}")
    return name


def inside(path, boundary):
    resolved = path.resolve()
    require(resolved != boundary and boundary in resolved.parents, f"Path escapes boundary: {path}")
    return resolved


def fingerprint(stream):
    digest = hashlib.sha256()
    size = 0
    for block in iter(lambda: stream.read(BLOCK), b""):
        digest.update(block)
        size += len(block)
    return {"bytes": size, "sha256": digest.hexdigest()}


def file_hash(path):
    with path.open("rb") as stream:
        return fingerprint(stream)


def private(name):
    tokens = re.split(r"[^a-z0-9]+", name.casefold())
    return any(token.startswith(("save", "journal", "private", "progress")) for token in tokens)


def debug(name):
    path = Path(name)
    return path.suffix.casefold() in DEBUG or any(
        "backup" in part.casefold() or "donotship" in part.casefold()
        or part.casefold() in {"debug", "symbols", "obj"} for part in path.parts)


def entries(archive):
    names = []
    folded = set()
    for item in archive.infolist():
        require(item.orig_filename == item.filename, "Unsafe entry contains a NUL character")
        name = safe_name(item.filename)
        mode = stat.S_IFMT(item.external_attr >> 16)
        require(mode in {0, stat.S_IFREG} and not item.is_dir(), f"Non-file ZIP entry: {name}")
        require(not item.flag_bits & 1, f"Encrypted ZIP entry: {name}")
        require(name.casefold() not in folded, f"Duplicate ZIP entry: {name}")
        names.append(name)
        folded.add(name.casefold())
    for name in names:
        parents = Path(name).parents
        require(not any(parent.as_posix().casefold() in folded for parent in parents),
                f"File/directory collision: {name}")
    require(bool(names), "Empty archive")
    return names


def inspect_package(path, expected):
    result = file_hash(path)
    require(all(result[key] == expected.get(key) for key in result), f"Archive SHA/size mismatch: {path.name}")
    with zipfile.ZipFile(path) as archive:
        names = entries(archive)
        result["file_count"] = len(names)
        result["uncompressed_bytes"] = sum(item.file_size for item in archive.infolist())
        require(all(result[key] == expected.get(key) for key in result),
                f"Archive totals mismatch: {path.name}")
        # Đọc toàn bộ dữ liệu để zipfile kiểm CRC, không tin cờ CRC trong manifest.
        hashes = {}
        for name in names:
            with archive.open(name) as stream:
                hashes[name] = fingerprint(stream)
    result["crc_verified"] = True
    return result, hashes


def match_files(hashes, files, label):
    require(set(hashes) == set(files), f"{label} file list differs from expected whitelist")
    for name, path in files.items():
        require(path.is_file() and file_hash(path) == hashes[name], f"{label} checkout bytes differ: {name}")


def check_windows(root, candidate, hashes, keys):
    executable = safe_name(keys["executable"])
    require("/" not in executable and executable.lower().endswith(".exe"), "Invalid Player executable")
    data = executable[:-4] + "_Data/"
    require(keys.get("data_directory") == data and keys.get("player_library") == "UnityPlayer.dll",
            "Invalid Windows manifest runtime keys")
    runtime = {}
    for path in candidate.rglob("*"):
        resolved = inside(path, candidate)
        name = path.relative_to(candidate).as_posix()
        if not path.is_file() or debug(name) or path.suffix.lower() in SOURCE:
            continue
        safe_name(name)
        runtime[name] = resolved
    documents = {name: inside(root / name, root) for name in RELEASE_DOCS if (root / name).is_file()}
    require(not set(runtime).intersection(documents), "Runtime/document collision")
    for name in hashes:
        require(not private(name) and not debug(name) and Path(name).suffix.lower() not in SOURCE,
                f"Forbidden Windows source/private/debug entry: {name}")
        if name in documents:
            continue
        top = "/" not in name and (Path(name).suffix.lower() in {".exe", ".dll"} or name == "build-report.json")
        require(top or name.startswith((data, "D3D12/", "MonoBleedingEdge/")), f"Non-runtime entry: {name}")
    require({executable, "UnityPlayer.dll"} <= set(runtime) and any(name.startswith(data) for name in runtime),
            "Incomplete Player runtime")
    require("GameAssembly.dll" in runtime or any(name.startswith("MonoBleedingEdge/") for name in runtime),
            "Missing Mono/IL2CPP runtime")
    match_files(hashes, runtime | documents, "Windows")
    return {"runtimeFilesMatched": len(runtime), "documentFilesMatched": len(documents), "executable": executable}


def check_source(root, delivery, manifest, hashes):
    list_name = safe_name(manifest["source_list"])
    source_list = inside(root / list_name, root)
    require(source_list == delivery.parent / "SourceList.txt", "Unexpected SourceList location")
    names = source_list.read_text(encoding="utf-8-sig").splitlines()
    require(names and len(names) == len({name.casefold() for name in names}), "Empty or duplicate source list")
    files = {safe_name(name): inside(root / name, root) for name in names}
    require(manifest.get("source_file_count") == len(files), "Source count differs from manifest")
    match_files(hashes, files, "Source")
    with zipfile.ZipFile(delivery / "Source.zip") as archive:
        for name in files:
            with archive.open(name) as stream:
                require(stream.read(len(LFS)) != LFS, f"Unexpanded Git LFS pointer: {name}")
    return {"filesMatched": len(files), "lfsPointersFound": 0}


def check_evidence(delivery, hashes):
    png_count = 0
    with zipfile.ZipFile(delivery / "Evidence.zip") as archive:
        for name in hashes:
            require(not private(name), f"Private evidence entry: {name}")
            require(Path(name).suffix.lower() in EVIDENCE_SUFFIXES, f"Unsupported evidence entry: {name}")
            if Path(name).suffix.lower() != ".png":
                continue
            with archive.open(name) as stream, Image.open(stream) as capture:
                require(capture.format == "PNG", f"Fake PNG evidence: {name}")
                capture.verify()
            with archive.open(name) as stream, Image.open(stream) as capture:
                capture.load()
                require(min(capture.size) > 0, f"Empty PNG evidence: {name}")
            png_count += 1
    require(png_count > 0, "Evidence must contain real PNG captures")
    return {"fileCount": len(hashes), "pngCount": png_count, "privateFilesFound": 0}


def extract_windows(delivery, extraction, hashes):
    extraction.mkdir(parents=True, exist_ok=False)
    with zipfile.ZipFile(delivery / "Windows.zip") as archive:
        for name, expected in hashes.items():
            destination = inside(extraction / name, extraction)
            destination.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(name) as source, destination.open("xb") as target:
                for block in iter(lambda: source.read(BLOCK), b""):
                    target.write(block)
            require(file_hash(destination) == expected, f"Extracted bytes differ: {name}")


def verify(root: Path, delivery: Path, candidate: Path, extraction: Path) -> dict:
    root = root.resolve(strict=True)
    work = inside(root / "work", root)
    delivery = inside(delivery, work)
    candidate = inside(candidate, work)
    require(not extraction.exists() and not extraction.is_symlink(), "Extraction already exists")
    extraction = inside(extraction, work)
    for source in (candidate, delivery):
        require(source.is_dir(), f"Missing input directory: {source}")
        require(source != extraction and source not in extraction.parents and extraction not in source.parents,
                "Extraction overlaps input directory")
    report_path = delivery / "POST_PACKAGE_CHECKS.json"
    require(not report_path.exists(), "Report already exists")
    manifest_path = delivery / "manifest.json"
    manifest_before = file_hash(manifest_path)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    require(set(manifest["packages"]) == set(PACKAGES), "Expected exactly three manifest packages")
    packages = {}
    hashes = {}
    for name in PACKAGES:
        packages[name], hashes[name] = inspect_package(delivery / name, manifest["packages"][name])
    windows = check_windows(root, candidate, hashes["Windows.zip"], manifest["windows"])
    source = check_source(root, delivery, manifest, hashes["Source.zip"])
    evidence = check_evidence(delivery, hashes["Evidence.zip"])
    extract_windows(delivery, extraction, hashes["Windows.zip"])
    require(file_hash(manifest_path) == manifest_before, "Manifest changed during verification")
    for name in PACKAGES:
        require(file_hash(delivery / name) == {key: packages[name][key] for key in ("bytes", "sha256")},
                f"Archive changed during verification: {name}")
    windows["extraction"] = extraction.relative_to(root).as_posix()
    report = {
        "schemaVersion": 1, "verifiedUtc": datetime.now(timezone.utc).isoformat(), "passed": True,
        "packages": packages, "windows": windows, "source": source, "evidence": evidence,
        "playerExecuted": False, "stable165Accepted": False,
    }
    with report_path.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2)
        stream.write("\n")
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve(strict=True)
    try:
        report = verify(root, root / BASE / "Delivery", root / BASE / "Candidate", root / BASE / "PackageSmoke")
    except (OSError, ValueError, KeyError, TypeError, zipfile.BadZipFile) as error:
        parser.exit(1, f"Delivery verification failed: {error}\n")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
