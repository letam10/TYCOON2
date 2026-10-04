"""Public author downloads. Sources remain unchanged; outputs have hashes."""
from pathlib import Path
import concurrent.futures
import hashlib
import html
import json
import re
import sys
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "work/python-download"))
PACKS = {
    "characters": "https://quaternius.com/packs/ultimatemodularcharacters.html",
    "crops": "https://quaternius.com/packs/ultimatecrops.html",
    "animals": "https://quaternius.com/packs/farmanimal.html",
    "food": "https://quaternius.com/packs/ultimatefood.html",
    "buildings": "https://quaternius.com/packs/farmbuildings.html",
}

def read_url(url):
    request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 TYCOON2 asset pipeline"})
    with urllib.request.urlopen(request, timeout=45) as response:
        return response.read()

def discover():
    def resolve(pair):
        key, url = pair
        page = html.unescape(read_url(url).decode("utf-8", "replace"))
        links = list(dict.fromkeys(re.findall(r"https://drive\.google\.com/drive/folders/[A-Za-z0-9_-]+[^'\"<> ]*", page)))
        if "CC0" not in page:
            raise RuntimeError("Missing CC0 author evidence: " + url)
        return {"key": key, "source_page": url, "license": "CC0-1.0", "license_url": "https://creativecommons.org/publicdomain/zero/1.0/", "downloads": links}
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        records = list(pool.map(resolve, PACKS.items()))
    target = ROOT / "ASSET/source-manifest.json"
    target.write_text(json.dumps(records, indent=2), encoding="utf-8")
    print(json.dumps(records))

def inventory():
    import gdown
    records = json.loads((ROOT / "ASSET/source-manifest.json").read_text(encoding="utf-8"))
    output = []
    for record in records:
        folder = next(iter(record["downloads"]), None)
        if not folder:
            output.append({"key": record["key"], "files": [], "error": "No published Drive folder"})
            continue
        try:
            entries = gdown.download_folder(folder, quiet=True, skip_download=True, remaining_ok=True)
            files = [{"id": entry.id, "path": entry.path} for entry in entries or []]
            output.append({"key": record["key"], "files": files, "folder": folder})
        except Exception as error:
            output.append({"key": record["key"], "files": [], "error": str(error)})
        print(json.dumps({"key": record["key"], "files": len(output[-1].get("files", [])), "error": output[-1].get("error", "")}), flush=True)
    (ROOT / "ASSET/download-inventory.json").write_text(json.dumps(output, indent=2), encoding="utf-8")

def download():
    import gdown
    records = json.loads((ROOT / "ASSET/download-inventory.json").read_text(encoding="utf-8"))
    output = []
    for record in records:
        files = record.get("files", [])
        archives = [entry for entry in files if entry["path"].lower().endswith(".zip")]
        wanted = {
            "crops": ("carrot_4", "carrot_crop", "tomato_4", "tomato_crop", "wheat_4", "wheat_crop"),
            "animals": ("cow",),
            "food": ("bread", "bread_cut", "cheese", "cake", "egg", "milk", "tomato", "carrot"),
            "buildings": ("chickencoop", "openbarn", "smallbarn", "fence", "well", "windmill"),
        }
        candidates = archives or [
            entry for entry in files if
            Path(entry["path"]).name.lower().startswith("license") or
            (entry["path"].lower().endswith((".png", ".jpg")) and "texture" in entry["path"].lower()) or
            (entry["path"].lower().endswith(".fbx") and
             (record["key"] == "characters" or Path(entry["path"]).stem.lower() in wanted.get(record["key"], ())))
        ]
        for entry in candidates:
            relative = Path(entry["path"])
            if relative.is_absolute() or ".." in relative.parts:
                raise RuntimeError("Unsafe source path: " + str(relative))
            target = ROOT / "ASSET/Downloaded" / record["key"] / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                result = gdown.download(id=entry["id"], output=str(target), quiet=True)
                if not result:
                    raise RuntimeError("Failed author download: " + entry["id"])
            output.append({"pack": record["key"], "path": str(target.relative_to(ROOT)), "bytes": target.stat().st_size, "sha256": hashlib.sha256(target.read_bytes()).hexdigest(), "drive_id": entry["id"]})
            print(json.dumps(output[-1]), flush=True)
    (ROOT / "ASSET/download-report.json").write_text(json.dumps(output, indent=2), encoding="utf-8")

def fonts():
    directory = ROOT / "ASSET/Fonts"
    directory.mkdir(parents=True, exist_ok=True)
    sources = {
        "NotoSans-Regular.ttf": "https://raw.githubusercontent.com/notofonts/noto-fonts/main/hinted/ttf/NotoSans/NotoSans-Regular.ttf",
        "LICENSE": "https://raw.githubusercontent.com/notofonts/noto-fonts/main/LICENSE",
    }
    for name, url in sources.items():
        target = directory / name
        if not target.exists():
            target.write_bytes(read_url(url))
        print(name, target.stat().st_size, flush=True)

if __name__ == "__main__":
    {"discover": discover, "inventory": inventory, "download": download, "fonts": fonts}[sys.argv[1]]()
