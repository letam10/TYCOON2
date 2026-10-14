"""Create verified local release ZIPs from checkout bytes, never Git LFS pointers."""
import argparse
import hashlib
import json
import re
import subprocess
import zipfile
from pathlib import Path, PureWindowsPath


EVIDENCE_SUFFIXES = {
    ".png", ".jpg", ".jpeg", ".gif", ".mp4", ".webm", ".avi", ".mov", ".mkv", ".json", ".log", ".csv",
    ".txt", ".md", ".xml",
}
LFS_HEADER = b"version https://git-lfs.github.com/spec/v1"
PACKAGE_NAMES = ("Windows.zip", "Source.zip", "Evidence.zip", "manifest.json")
RELEASE_DOCS = (
    "QA/city-model-v14-acceptance-20261010.json", "Docs/V14_PLAYER_GUIDE.md",
    "Docs/V14_PERFORMANCE_FINDINGS.md", "Docs/V14_CHECKPOINT_HISTORY.md",
    "Docs/V14_HARDWARE_AND_FPS.md",
    "Docs/V14_HITCH_DIAGNOSIS.md", "Docs/V14_PROFILE_092458.md",
    "Docs/V14_REQUIREMENT_EVIDENCE.md", "Docs/V14_HARDWARE_SENSOR_AVAILABILITY.md",
    "QA/city-model-v13-acceptance-20261010.json", "Docs/V13_PLAYER_GUIDE.md",
    "Docs/V13_OCEAN_CONTINUITY.md",
    "QA/city-model-v12-acceptance-20261010.json", "Docs/V12_PLAYER_GUIDE.md",
    "Docs/V12_LABEL_PERFORMANCE_ACCEPTANCE.md", "Docs/V12_PACING_REVIEW.md",
    "QA/city-model-v11-acceptance-20261010.json", "Docs/V11_CITY_MODEL_ACCEPTANCE.md",
    "Docs/V11_PLAYER_GUIDE.md", "Docs/SHADOW_FIX_20261010.md",
    "Docs/V11_RETAIL_MODELS.md", "Docs/V11_CIVIC_MODELS.md",
    "Docs/CARRY_CONTAINERS_V10.md", "Docs/INDUSTRIAL_MODELS_V10.md", "Docs/RENDER_OPTIMIZATION_V10.md",
    "Docs/Art/V10_GOODS_REVIEW.md", "Docs/Art/V10_WORLD_REVIEW.md", "README.md",
)
SOURCE_SUFFIXES = {".cs", ".py", ".csproj", ".sln", ".cpp", ".c", ".h", ".hpp", ".meta", ".asmdef"}


def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args])


def within(path, root):
    return path == root or root in path.parents


def checked(path, root):
    resolved = path.resolve()
    if not within(resolved, root):
        raise ValueError(f"Path escapes {root}: {path}")
    return resolved


def input_directory(value, work):
    path = checked(Path(value), work)
    if path == work or not path.is_dir():
        raise ValueError(f"Expected an existing directory below work/: {path}")
    return path


def private_evidence(relative):
    # Loại cả thư mục và tên file để không đưa tiến trình riêng vào gói bằng chứng.
    parts = re.split(r"[^a-z0-9]+", relative.as_posix().lower())
    return any(part.startswith(("save", "journal", "private", "progress")) for part in parts)


def debug_file(relative):
    for part in relative.parts:
        name = part.lower()
        if "backup" in name or "donotship" in name or name in {"debug", "symbols", "obj"}:
            return True
    return relative.suffix.lower() in {".pdb", ".mdb", ".debug", ".bak", ".tmp", ".log"}


def collect(folder, include):
    files = []
    for path in sorted(folder.rglob("*")):
        checked(path, folder)
        if path.is_file() and include(path.relative_to(folder)):
            files.append((path, path.relative_to(folder).as_posix()))
    return files


def source_files(root, source_list=None):
    if source_list is None:
        names = list(filter(None, git(root, "ls-files", "-z").decode("utf-8").split("\0")))
    else:
        names = checked(Path(source_list), root).read_text(encoding="utf-8-sig").splitlines()
        if not names or any(not name.strip() for name in names):
            raise ValueError("Source list must contain paths with no blank lines")
    files = []
    seen = set()
    for name in sorted(names):
        if Path(name).is_absolute() or PureWindowsPath(name).drive or name.startswith(("/", "\\")):
            raise ValueError(f"Source path must be relative to workspace: {name}")
        path = checked(root / name, root)
        name = path.relative_to(root).as_posix()
        if name.casefold() in seen:
            raise ValueError(f"Duplicate source path: {name}")
        seen.add(name.casefold())
        if not path.is_file():
            raise ValueError(f"Source file is missing or is a submodule: {name}")
        with path.open("rb") as source:
            if source.read(len(LFS_HEADER)) == LFS_HEADER:
                raise ValueError(f"Unexpanded Git LFS pointer: {name}; run git lfs pull first")
        files.append((path, name))
    return files


def windows_keys(names):
    names = set(names)
    candidates = sorted(name for name in names if "/" not in name and name.lower().endswith(".exe"))
    for exe in candidates:
        data = exe[:-4] + "_Data/"
        data_files = [name for name in names if name.startswith(data)]
        runtime = "GameAssembly.dll" in names or any(name.startswith("MonoBleedingEdge/") for name in names)
        if "UnityPlayer.dll" in names and data_files and runtime:
            return {"executable": exe, "data_directory": data, "player_library": "UnityPlayer.dll"}
    raise ValueError("Windows build must contain its EXE, matching _Data/, UnityPlayer.dll and Mono or IL2CPP runtime")


def write_zip(destination, files):
    names = [name for _, name in files]
    if len({name.casefold() for name in names}) != len(names):
        raise ValueError(f"Duplicate ZIP entry in {destination.name}")
    # Mốc thời gian và thứ tự cố định giúp cùng dữ liệu tạo cùng gói trên cùng Python/zlib.
    hashes = {}
    with zipfile.ZipFile(destination, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path, name in sorted(files, key=lambda entry: entry[1]):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            info.create_system = 3
            digest = hashlib.sha256()
            with path.open("rb") as source, archive.open(info, "w", force_zip64=True) as target:
                for block in iter(lambda: source.read(1024 * 1024), b""):
                    digest.update(block)
                    target.write(block)
            hashes[name] = digest.hexdigest()
    with zipfile.ZipFile(destination) as archive:
        failure = archive.testzip()
        if failure:
            raise ValueError(f"ZIP CRC verification failed: {destination}: {failure}")
        if set(archive.namelist()) != set(names):
            raise ValueError(f"ZIP entries differ from input: {destination}")
        for name, expected in hashes.items():
            digest = hashlib.sha256()
            with archive.open(name) as source:
                for block in iter(lambda: source.read(1024 * 1024), b""):
                    digest.update(block)
            if digest.hexdigest() != expected:
                raise ValueError(f"ZIP SHA256 verification failed: {destination}: {name}")
        sizes = sum(item.file_size for item in archive.infolist())
    digest = hashlib.sha256()
    with destination.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return {"file_count": len(files), "uncompressed_bytes": sizes,
            "bytes": destination.stat().st_size, "sha256": digest.hexdigest(), "crc_verified": True}


def package(root, build, evidence, output, source_list=None):
    root = root.resolve()
    work = checked(root / "work", root)
    build = input_directory(build, work)
    evidence = input_directory(evidence, work)
    output = checked(Path(output), work)
    if output == work:
        raise ValueError("Output must be a subdirectory of work/")
    if any(within(output, item) or within(item, output) for item in (build, evidence)):
        raise ValueError("Output must not overlap build or evidence directories")
    if any((output / name).exists() for name in PACKAGE_NAMES):
        raise FileExistsError("A release package already exists; choose a new output directory")
    windows = collect(build, lambda path: not debug_file(path) and path.suffix.lower() not in SOURCE_SUFFIXES)
    keys = windows_keys([name for _, name in windows])
    for name in RELEASE_DOCS:
        path = checked(root / name, root)
        if path.is_file():
            windows.append((path, name))
    source_list = checked(Path(source_list), root) if source_list is not None else None
    sources = source_files(root, source_list)
    captures = collect(evidence, lambda path: path.suffix.lower() in EVIDENCE_SUFFIXES
                       and not private_evidence(path))
    if not captures:
        raise ValueError("Evidence directory has no eligible files")
    commit = git(root, "rev-parse", "HEAD").decode().strip()
    status = git(root, "status", "--porcelain", "--untracked-files=no").decode().strip()
    output.mkdir(parents=True, exist_ok=True)
    manifest = {"commit": commit, "tracked_checkout_dirty": bool(status), "windows": keys, "packages": {}}
    manifest["source_list"] = source_list.relative_to(root).as_posix() if source_list else None
    manifest["source_file_count"] = len(sources)
    for name, files in zip(PACKAGE_NAMES, (windows, sources, captures)):
        manifest["packages"][name] = write_zip(output / name, files)
    with zipfile.ZipFile(output / "Windows.zip") as archive:
        windows_keys(archive.namelist())
    with (output / "manifest.json").open("x", encoding="utf-8", newline="\n") as target:
        json.dump(manifest, target, ensure_ascii=False, indent=2, sort_keys=True)
        target.write("\n")
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True, help="Windows build directory below workspace work/")
    parser.add_argument("--evidence", required=True, help="Evidence directory below workspace work/")
    parser.add_argument("--output", required=True, help="Package directory below work/; existing packages are rejected")
    parser.add_argument("--source-list", type=Path, help="UTF-8 file listing workspace-relative source paths")
    args = parser.parse_args()
    try:
        root = Path(git(Path.cwd(), "rev-parse", "--show-toplevel").decode().strip())
        manifest = package(root, args.build, args.evidence, args.output, args.source_list)
    except (OSError, ValueError, subprocess.CalledProcessError, zipfile.BadZipFile) as error:
        parser.exit(1, f"Packaging failed: {error}\n")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
