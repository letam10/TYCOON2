"""Release smoke tests; fixtures stay under work/ and never touch the real Git index."""
import hashlib
import json
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

import package_city


ROOT = Path(__file__).resolve().parents[1]


class PackageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.fixture = Path(tempfile.mkdtemp(prefix="package-city-test-", dir=ROOT / "work"))
        cls.root = cls.fixture / "checkout"
        cls.build = cls.root / "work" / "build"
        cls.evidence = cls.root / "work" / "evidence"
        cls.build.mkdir(parents=True)
        cls.evidence.mkdir()
        cls.put(cls.build, "City.exe", b"fixture executable")
        cls.put(cls.build, "UnityPlayer.dll", b"fixture player library")
        cls.put(cls.build, "MonoBleedingEdge/EmbedRuntime/mono.dll", b"fixture mono")
        cls.put(cls.build, "City_Data/globalgamemanagers", b"fixture data")
        cls.put(cls.build, "City.pdb", b"debug")
        cls.put(cls.build, "City_BackUpThisFolder_ButDontShip/private.cpp", b"backup")
        cls.put(cls.evidence, "play.png", b"fixture capture")
        cls.put(cls.evidence, "frame-times.csv", b"frame,ms\n1,6.0\n")
        cls.put(cls.evidence, "acceptance.json", b'{"passed":true}')
        cls.put(cls.evidence, "savegame.json", b"private save")
        cls.put(cls.evidence, "journal/events.json", b"private journal")
        cls.put(cls.evidence, "private-progress/frames.png", b"private progress")
        for name in package_city.RELEASE_DOCS:
            cls.put(cls.root, name, b"fixture documentation")
        names = package_city.git(ROOT, "ls-files", "-z", "*.png").decode().split("\0")
        candidates = sorted((ROOT / name for name in names if name), key=lambda path: path.stat().st_size)
        cls.asset = next(path for path in candidates if not path.read_bytes().startswith(package_city.LFS_HEADER))
        cls.asset_bytes = cls.asset.read_bytes()
        cls.put(cls.root, "Assets/sample.png", cls.asset_bytes)
        cls.put(cls.root, "Assets/code.cs", b"class Fixture {}\n")
        print(f"Retained fixture: {cls.fixture}")
        print(f"Real checkout asset: {cls.asset}")

    @staticmethod
    def put(root, name, content):
        path = root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)

    @staticmethod
    def fixture_git(root, *args):
        if args[0] == "ls-files":
            return b"Assets/code.cs\0Assets/sample.png\0"
        if args[0] == "rev-parse":
            return b"0123456789abcdef0123456789abcdef01234567\n"
        if args[0] == "status":
            return b""
        raise AssertionError(args)

    def release(self, name):
        output = self.root / "work" / name
        with patch.object(package_city, "git", self.fixture_git):
            manifest = package_city.package(self.root, self.build, self.evidence, output)
        return output, manifest

    def test_archives_manifest_and_reproducibility(self):
        output, manifest = self.release("release-a")
        with zipfile.ZipFile(output / "Windows.zip") as archive:
            names = archive.namelist()
            self.assertIn("City.exe", names)
            self.assertTrue(all(name in names for name in package_city.RELEASE_DOCS))
            self.assertFalse(any("backup" in name.lower() or name.endswith(".pdb") for name in names))
        with zipfile.ZipFile(output / "Source.zip") as archive:
            self.assertEqual(archive.read("Assets/sample.png"), self.asset_bytes)
            self.assertFalse(archive.read("Assets/sample.png").startswith(package_city.LFS_HEADER))
        with zipfile.ZipFile(output / "Evidence.zip") as archive:
            self.assertEqual(set(archive.namelist()), {"play.png", "frame-times.csv", "acceptance.json"})
        for name, record in manifest["packages"].items():
            self.assertEqual(record["sha256"], hashlib.sha256((output / name).read_bytes()).hexdigest())
            with zipfile.ZipFile(output / name) as archive:
                self.assertIsNone(archive.testzip())
                self.assertEqual(record["file_count"], len(archive.infolist()))
                self.assertEqual(record["uncompressed_bytes"], sum(item.file_size for item in archive.infolist()))
        self.assertEqual(json.loads((output / "manifest.json").read_text()), manifest)
        _, repeated = self.release("release-b")
        self.assertEqual(manifest, repeated)
        before = (output / "Windows.zip").read_bytes()
        with self.assertRaises(FileExistsError):
            self.release("release-a")
        self.assertEqual((output / "Windows.zip").read_bytes(), before)

    def test_outside_and_overlapping_paths_rejected(self):
        for build, evidence, output in (
            (self.root, self.evidence, self.root / "work/out"),
            (self.build, self.root, self.root / "work/out"),
            (self.build, self.evidence, self.root / "outside"),
            (self.build, self.evidence, self.build / "nested"),
            (self.build, self.evidence, self.root / "work"),
        ):
            with self.subTest(output=output, build=build), self.assertRaises(ValueError):
                package_city.package(self.root, build, evidence, output)

    def test_pointer_and_incomplete_runtime_rejected(self):
        pointer_root = self.fixture / "pointer-checkout"
        self.put(pointer_root, "Assets/code.cs", b"fixture")
        self.put(pointer_root, "Assets/sample.png", package_city.LFS_HEADER + b"\noid sha256:abcd\n")
        with patch.object(package_city, "git", self.fixture_git), self.assertRaisesRegex(ValueError, "LFS pointer"):
            package_city.source_files(pointer_root)
        with self.assertRaises(ValueError):
            package_city.windows_keys(["City.exe", "UnityPlayer.dll", "City_Data/data"])


if __name__ == "__main__":
    unittest.main()
