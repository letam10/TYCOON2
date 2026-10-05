"""Read-only inventory of ASSET; source files are never changed."""
from collections import Counter, defaultdict
from pathlib import Path
import hashlib
import json
import struct
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "work/art-refresh"
MODELS = {".fbx", ".glb", ".gltf", ".obj", ".dae", ".stl"}


def inspect(path, data):
    ext = path.suffix.lower()
    if ext in {".glb", ".gltf"}:
        if ext == ".glb":
            magic, version, length = struct.unpack_from("<4sII", data)
            assert magic == b"glTF" and version == 2 and length == len(data)
            count, kind = struct.unpack_from("<II", data, 12)
            assert kind == 0x4E4F534A
            doc = json.loads(data[20:20 + count])
        else:
            doc = json.loads(data)
        assert doc["asset"]["version"] == "2.0"
        missing = []
        for node in doc.get("buffers", []) + doc.get("images", []):
            uri = node.get("uri", "")
            if uri and not uri.startswith("data:") and not (path.parent / unquote(uri)).is_file():
                missing.append(uri)
        if missing:
            raise ValueError("Missing dependencies: " + ", ".join(missing))
        triangles = 0
        for mesh in doc.get("meshes", []):
            for primitive in mesh["primitives"]:
                index = primitive.get("indices", primitive["attributes"].get("POSITION"))
                n = doc["accessors"][index]["count"] if index is not None else 0
                mode = primitive.get("mode", 4)
                triangles += n // 3 if mode == 4 else max(0, n - 2) if mode in (5, 6) else 0
        return {"meshes": len(doc.get("meshes", [])), "triangles": triangles,
                "skins": len(doc.get("skins", [])), "animations": len(doc.get("animations", [])),
                "materials": len(doc.get("materials", [])), "images": len(doc.get("images", []))}
    if ext == ".fbx":
        assert data.startswith(b"Kaydara FBX Binary") or b"FBX" in data[:256]
        return {"inspection": "header; mesh/rig acceptance only for imported candidates"}
    if ext == ".obj":
        lines = data.decode("utf-8", errors="replace").splitlines()
        vertices = sum(line.startswith("v ") for line in lines)
        triangles = sum(max(0, len(line.split()) - 3) for line in lines if line.startswith("f "))
        assert vertices > 0 and triangles > 0
        return {"vertices": vertices, "triangles": triangles}
    if ext == ".png":
        assert data[:8] == b"\x89PNG\r\n\x1a\n"
        return {"width": struct.unpack_from(">I", data, 16)[0], "height": struct.unpack_from(">I", data, 20)[0]}
    return {}


def main():
    entries, groups, hashes = [], defaultdict(list), defaultdict(list)
    for path in sorted((ROOT / "ASSET").rglob("*")):
        if not path.is_file():
            continue
        data = path.read_bytes()
        relative = path.relative_to(ROOT).as_posix()
        parts = path.relative_to(ROOT / "ASSET").parts
        pack = parts[1] if parts[0] == "Downloaded" else parts[0]
        digest = hashlib.sha256(data).hexdigest()
        entry = {"path": relative, "pack": pack, "bytes": len(data), "sha256": digest}
        try:
            entry.update(inspect(path, data))
        except Exception as error:
            entry["issue"] = str(error)
        entries.append(entry)
        groups[pack].append(entry)
        hashes[digest].append(relative)
    summary = []
    for pack, files in sorted(groups.items()):
        formats = Counter(Path(x["path"]).suffix.lower() for x in files)
        licenses = [x["path"] for x in files if "license" in Path(x["path"]).name.lower()]
        summary.append({"pack": pack, "files": len(files), "bytes": sum(x["bytes"] for x in files),
                        "models": sum(n for ext, n in formats.items() if ext in MODELS),
                        "formats": dict(sorted(formats.items())), "licenses": licenses,
                        "issues": [x for x in files if "issue" in x]})
    report = {"scope": "All files under ASSET, including alternate formats and previews",
              "files": len(entries), "bytes": sum(x["bytes"] for x in entries), "packs": summary,
              "duplicates": [v for v in hashes.values() if len(v) > 1], "entries": entries}
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "library-audit.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps({"files": report["files"], "megabytes": round(report["bytes"] / 1048576, 1),
                      "packs": [{k: p[k] for k in ("pack", "files", "models", "issues")} for p in summary],
                      "duplicateGroups": len(report["duplicates"])}, ensure_ascii=False))


if __name__ == "__main__":
    main()
