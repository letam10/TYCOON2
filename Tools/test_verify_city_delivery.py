"""Synthetic delivery fixtures; never mutate or execute the real packaged game."""
import io
import json
import struct
import tempfile
import unittest
import zipfile
from pathlib import Path

from PIL import Image

import verify_city_delivery as checker


class DeliveryTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="v14-delivery-check-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        self.base = self.root / checker.BASE
        self.delivery = self.base / "Delivery"
        self.candidate = self.base / "Candidate"
        self.extraction = self.base / "PackageSmoke"
        self.delivery.mkdir(parents=True)
        self.candidate.mkdir()
        self.runtime = {
            "City.exe": b"fixture Player - never execute",
            "UnityPlayer.dll": b"fixture unity library",
            "GameAssembly.dll": b"fixture il2cpp library",
            "City_Data/globalgamemanagers": b"fixture data",
        }
        for name, content in self.runtime.items():
            self.put(self.candidate / name, content)
        self.put(self.candidate / "City.pdb", b"excluded symbol")
        self.put(self.candidate / "City_BackUpThisFolder_ButDontShipItWithYourGame/a.cpp", b"excluded source")
        self.source = {"Assets/main.cs": b"class Fixture {}", "Assets/art.bin": b"expanded asset bytes"}
        for name, content in self.source.items():
            self.put(self.root / name, content)
        self.put(self.root / "README.md", b"fixture guide")
        self.put(self.base / "SourceList.txt", "\n".join(self.source).encode())
        capture = io.BytesIO()
        Image.new("RGB", (3, 2), color=(30, 70, 120)).save(capture, "PNG")
        self.archives = {
            "Windows.zip": self.runtime | {"README.md": b"fixture guide"},
            "Source.zip": self.source.copy(),
            "Evidence.zip": {"capture.png": capture.getvalue(), "qa.json": b'{"passed":true}'},
        }
        self.manifest = {
            "packages": {}, "source_list": (checker.BASE / "SourceList.txt").as_posix(),
            "source_file_count": len(self.source),
            "windows": {"executable": "City.exe", "data_directory": "City_Data/",
                        "player_library": "UnityPlayer.dll"},
        }
        for name, entries in self.archives.items():
            self.write_archive(name, entries)
        self.write_manifest()

    @staticmethod
    def put(path, content):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)

    def write_manifest(self):
        (self.delivery / "manifest.json").write_text(json.dumps(self.manifest), encoding="utf-8")

    def write_archive(self, name, entries):
        path = self.delivery / name
        with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_STORED) as archive:
            for key, content in entries.items():
                archive.writestr(key, content)
        with zipfile.ZipFile(path) as archive:
            self.manifest["packages"][name] = {
                **checker.file_hash(path), "file_count": len(archive.infolist()),
                "uncompressed_bytes": sum(item.file_size for item in archive.infolist()),
                "crc_verified": True,
            }

    def replace_archive(self, name, entries):
        self.write_archive(name, entries)
        self.write_manifest()

    def verify(self):
        return checker.verify(self.root, self.delivery, self.candidate, self.extraction)

    def rejected(self, pattern):
        with self.assertRaisesRegex((ValueError, zipfile.BadZipFile, OSError), pattern):
            self.verify()
        self.assertFalse((self.delivery / "POST_PACKAGE_CHECKS.json").exists())
        self.assertFalse(self.extraction.exists())

    def test_valid_delivery_preserves_packages_and_extracts_exact_bytes(self):
        before = {path.name: checker.file_hash(path) for path in self.delivery.iterdir()}
        report = self.verify()
        self.assertTrue(report["passed"])
        self.assertFalse(report["playerExecuted"])
        self.assertFalse(report["stable165Accepted"])
        self.assertEqual(report["windows"]["runtimeFilesMatched"], 4)
        self.assertEqual(report["windows"]["documentFilesMatched"], 1)
        self.assertEqual(report["source"], {"filesMatched": 2, "lfsPointersFound": 0})
        self.assertEqual(report["evidence"], {"fileCount": 2, "pngCount": 1, "privateFilesFound": 0})
        for name, content in self.archives["Windows.zip"].items():
            self.assertEqual((self.extraction / name).read_bytes(), content)
        for name, expected in before.items():
            self.assertEqual(checker.file_hash(self.delivery / name), expected)
        with self.assertRaisesRegex(ValueError, "Extraction already exists"):
            self.verify()

    def test_archive_hash_and_size_mismatch(self):
        self.manifest["packages"]["Source.zip"]["sha256"] = "0" * 64
        self.write_manifest()
        self.rejected("SHA/size mismatch")

    def test_manifest_file_count_mismatch(self):
        self.manifest["packages"]["Evidence.zip"]["file_count"] += 1
        self.write_manifest()
        self.rejected("totals mismatch")

    def test_manifest_uncompressed_size_mismatch(self):
        self.manifest["packages"]["Windows.zip"]["uncompressed_bytes"] += 1
        self.write_manifest()
        self.rejected("totals mismatch")

    def test_crc_rechecked_even_when_manifest_hash_matches(self):
        path = self.delivery / "Evidence.zip"
        with zipfile.ZipFile(path) as archive:
            offset = archive.getinfo("qa.json").header_offset
        data = bytearray(path.read_bytes())
        name_length, extra_length = struct.unpack_from("<HH", data, offset + 26)
        data[offset + 30 + name_length + extra_length] ^= 1
        path.write_bytes(data)
        self.manifest["packages"]["Evidence.zip"].update(checker.file_hash(path))
        self.write_manifest()
        self.rejected("CRC")

    def test_zip_traversal_rejected_before_extraction(self):
        self.replace_archive("Windows.zip", self.archives["Windows.zip"] | {"../escape.dll": b"x"})
        self.rejected("Unsafe entry")

    def test_windows_unsafe_names(self):
        for name in ("/absolute", "C:/absolute", "dir\\file", "a//b", "a/./b", "a/../b",
                     "a:stream", "CON.txt", "aux", "COM¹.bin", "a. ", "a\x00b"):
            with self.subTest(name=name), self.assertRaises(ValueError):
                checker.safe_name(name)

    def test_case_insensitive_duplicate_rejected(self):
        self.replace_archive("Windows.zip", self.archives["Windows.zip"] | {"city.EXE": b"duplicate"})
        self.rejected("Duplicate ZIP entry")

    def test_embedded_nul_archive_name_rejected(self):
        path = self.delivery / "Evidence.zip"
        path.write_bytes(path.read_bytes().replace(b"qa.json", b"q\x00.json"))
        self.manifest["packages"]["Evidence.zip"].update(checker.file_hash(path))
        self.write_manifest()
        self.rejected("Unsafe entry")

    def test_file_directory_collision_rejected(self):
        self.replace_archive("Evidence.zip", self.archives["Evidence.zip"] | {"a": b"x", "a/b.png": b"x"})
        self.rejected("collision")

    def test_symlink_archive_entry_rejected(self):
        entry = zipfile.ZipInfo("link")
        entry.create_system = 3
        entry.external_attr = 0o120777 << 16
        self.replace_archive("Evidence.zip", {entry: b"target"})
        self.rejected("Non-file ZIP entry")

    def test_source_whitelist_mismatch(self):
        self.replace_archive("Source.zip", self.source | {"Assets/unlisted.cs": b"x"})
        self.rejected("Source file list differs")

    def test_source_checkout_drift(self):
        self.put(self.root / "Assets/main.cs", b"changed after packaging")
        self.rejected("Source checkout bytes differ")

    def test_unexpanded_lfs_pointer(self):
        content = checker.LFS + b"\noid sha256:1234\nsize 99\n"
        self.put(self.root / "Assets/art.bin", content)
        self.replace_archive("Source.zip", self.source | {"Assets/art.bin": content})
        self.rejected("Unexpanded Git LFS pointer")

    def test_runtime_checkout_drift(self):
        self.put(self.candidate / "GameAssembly.dll", b"different build")
        self.rejected("Windows checkout bytes differ")

    def test_document_checkout_drift(self):
        self.put(self.root / "README.md", b"changed guide")
        self.rejected("Windows checkout bytes differ")

    def test_missing_runtime_file(self):
        entries = self.archives["Windows.zip"].copy()
        del entries["GameAssembly.dll"]
        self.replace_archive("Windows.zip", entries)
        self.rejected("Windows file list differs")

    def test_source_save_and_debug_entries_in_windows(self):
        for name in ("City_Data/code.cs", "City_Data/savegame.json", "City.pdb", "private/foo.dll"):
            with self.subTest(name=name):
                self.replace_archive("Windows.zip", self.archives["Windows.zip"] | {name: b"x"})
                self.rejected("Forbidden Windows")

    def test_private_evidence_rejected(self):
        for name in ("savegame.json", "journal/entries.json", "private/qa.json", "progress.bin"):
            with self.subTest(name=name):
                self.replace_archive("Evidence.zip", self.archives["Evidence.zip"] | {name: b"x"})
                self.rejected("Private evidence")

    def test_fake_png_rejected(self):
        self.replace_archive("Evidence.zip", {"capture.png": b"fake PNG"})
        self.rejected("cannot identify image")

    def test_missing_png_rejected(self):
        self.replace_archive("Evidence.zip", {"qa.json": b"{}"})
        self.rejected("real PNG")

    def test_extraction_outside_work_or_overlapping_inputs(self):
        for path in (self.root / "outside", self.root / "work", self.candidate / "nested",
                     self.delivery / "nested"):
            with self.subTest(path=path):
                self.extraction = path
                with self.assertRaises(ValueError):
                    self.verify()
        self.assertFalse((self.delivery / "POST_PACKAGE_CHECKS.json").exists())


if __name__ == "__main__":
    unittest.main(verbosity=2)
