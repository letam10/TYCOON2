"""Synthetic delivery checks; all fixture files remain beneath workspace work/."""
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import curate_model_evidence as curator
import package_city as packer


ROOT = Path(__file__).resolve().parents[1]


class DeliveryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="v10-delivery-test-", dir=ROOT / "work")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / "checkout"
        self.build = self.root / "work/build"
        self.models = self.root / "work/models"
        self.ordinary = self.root / "work/ordinary"
        self.comparison = self.root / "work/comparison"
        self.evidence = self.root / "work/evidence"
        self.output = self.root / "work/release"
        self.source_list = self.root / "work/source-list.txt"
        self.tests = self.root / "work/current-full-suite.xml"
        for name, content in {
            "City.exe": b"synthetic exe", "UnityPlayer.dll": b"synthetic dll",
            "GameAssembly.dll": b"synthetic runtime", "City_Data/globalgamemanagers": b"synthetic data",
            "City.pdb": b"debug", "leaked.cs": b"class Source {}",
        }.items():
            self.put(self.build / name, content)
        for folder, name in ((self.models, "items-vehicles-v8-report.json"),
                             (self.ordinary, "ordinary-play-report.json")):
            self.put(folder / name, b'{"passed":true,"runtimeErrors":[]}')
            self.put(folder / "screen.png", b"unaltered synthetic pixels\x00\xff")
            self.put(folder / "private-checkpoint.json", b"private")
            self.put(folder / "saves/state.json", b"save")
            self.put(folder / "journal.json", b"journal")
            self.put(folder / "progress/state.json", b"progress")
        self.put(self.comparison / "fps.csv", b"variant,fps\nbaseline,140\ncandidate,166\n")
        self.put(self.tests, b'<test-run result="Passed" failed="0" passed="7"/>')
        for name in packer.RELEASE_DOCS:
            self.put(self.root / name, b"synthetic v10 document")
        self.put(self.root / "Assets/new.cs", b"new untracked source\r\n\x00\xff")
        self.put(self.root / "Assets/tracked.cs", b"tracked source\n")
        self.put(self.source_list, b"Assets/new.cs\nAssets/tracked.cs\n")

    @staticmethod
    def put(path, content):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)

    @staticmethod
    def git(root, *args):
        if args[0] == "ls-files":
            return b"Assets/tracked.cs\0"
        if args[0] == "rev-parse":
            return b"1234567890abcdef1234567890abcdef12345678\n"
        if args[0] == "status":
            return b" M Assets/tracked.cs\n"
        raise AssertionError(args)

    def curate(self, comparison=None):
        return curator.curate(self.root, self.models, self.ordinary, self.evidence, self.tests, comparison)

    def package(self, source_list=None, output=None):
        with patch.object(packer, "git", self.git):
            return packer.package(self.root, self.build, self.evidence, output or self.output, source_list)

    def test_untracked_sources_exact_bytes_manifest_crc_and_runtime(self):
        self.curate(self.comparison)
        manifest = self.package(self.source_list)
        self.assertEqual(manifest["source_list"], "work/source-list.txt")
        self.assertEqual(manifest["source_file_count"], 2)
        self.assertEqual(manifest["commit"], self.git(self.root, "rev-parse").decode().strip())
        self.assertTrue(manifest["tracked_checkout_dirty"])
        with zipfile.ZipFile(self.output / "Source.zip") as archive:
            self.assertEqual(set(archive.namelist()), {"Assets/new.cs", "Assets/tracked.cs"})
            self.assertEqual(archive.read("Assets/new.cs"), (self.root / "Assets/new.cs").read_bytes())
        with zipfile.ZipFile(self.output / "Windows.zip") as archive:
            self.assertEqual(packer.windows_keys(archive.namelist())["executable"], "City.exe")
            self.assertNotIn("leaked.cs", archive.namelist())
            self.assertNotIn("City.pdb", archive.namelist())
            self.assertTrue(set(packer.RELEASE_DOCS).issubset(archive.namelist()))
        for name, record in manifest["packages"].items():
            self.assertEqual(record["sha256"], hashlib.sha256((self.output / name).read_bytes()).hexdigest())
            self.assertTrue(record["crc_verified"])
            with zipfile.ZipFile(self.output / name) as archive:
                self.assertIsNone(archive.testzip())
        with self.assertRaises(FileExistsError):
            self.package(self.source_list)

    def test_without_list_keeps_tracked_selection(self):
        self.curate()
        manifest = self.package()
        self.assertIsNone(manifest["source_list"])
        with zipfile.ZipFile(self.output / "Source.zip") as archive:
            self.assertEqual(archive.namelist(), ["Assets/tracked.cs"])

    def test_real_git_head_dirty_marker_and_untracked_source(self):
        self.curate()
        packer.git(self.root, "init", "-q")
        packer.git(self.root, "add", "Assets/tracked.cs")
        packer.git(self.root, "-c", "user.name=Delivery Fixture", "-c", "user.email=fixture@example.invalid",
                   "-c", "commit.gpgsign=false", "commit", "-qm", "Synthetic fixture")
        self.put(self.root / "Assets/tracked.cs", b"dirty checkout bytes")
        self.assertNotIn(b"Assets/new.cs", packer.git(self.root, "ls-files", "-z"))
        manifest = packer.package(self.root, self.build, self.evidence, self.output, self.source_list)
        self.assertEqual(manifest["commit"], packer.git(self.root, "rev-parse", "HEAD").decode().strip())
        self.assertTrue(manifest["tracked_checkout_dirty"])
        with zipfile.ZipFile(self.output / "Source.zip") as archive:
            self.assertEqual(archive.read("Assets/tracked.cs"), b"dirty checkout bytes")
            self.assertEqual(archive.read("Assets/new.cs"), (self.root / "Assets/new.cs").read_bytes())

    def test_source_list_rejects_escape_missing_blank_absolute_and_duplicates(self):
        self.put(self.root.parent / "outside.cs", b"outside")
        for value in ("../outside.cs\n", "Assets/missing.cs\n", "Assets/new.cs\n\n", "",
                      "Assets/new.cs\nAssets/NEW.cs\n", str(self.root / "Assets/new.cs") + "\n"):
            with self.subTest(value=value):
                self.source_list.write_text(value, encoding="utf-8")
                with self.assertRaises(ValueError):
                    packer.source_files(self.root, self.source_list)

    def test_lfs_pointer_rejected(self):
        self.put(self.root / "Assets/new.cs", packer.LFS_HEADER + b"\noid sha256:abc\n")
        with self.assertRaisesRegex(ValueError, "LFS pointer"):
            packer.source_files(self.root, self.source_list)

    def test_curator_keeps_selected_xml_comparison_docs_checksums_and_excludes_private(self):
        self.curate(self.comparison)
        index = json.loads((self.evidence / "evidence-index.json").read_text(encoding="utf-8"))
        names = {record["path"] for record in index}
        self.assertIn("tests/current-full-suite.xml", names)
        self.assertIn("comparison/fps.csv", names)
        self.assertTrue(set(packer.RELEASE_DOCS).issubset(names))
        self.assertFalse(any(packer.private_evidence(Path(name)) for name in names))
        for record in index:
            content = (self.evidence / record["path"]).read_bytes()
            self.assertEqual(record["sha256"], hashlib.sha256(content).hexdigest())
            self.assertEqual(record["bytes"], len(content))
        self.assertEqual((self.evidence / "models/screen.png").read_bytes(),
                         (self.models / "screen.png").read_bytes())
        self.assertEqual((self.evidence / "tests" / self.tests.name).read_bytes(), self.tests.read_bytes())

    def test_failed_empty_or_non_suite_xml_rejected_before_output(self):
        for content in (b'<test-run failed="1" passed="7"/>', b'<test-run failed="0" passed="0"/>',
                        b'<test-suite failed="0" passed="7"/>', b'<test-run passed="7"/>',
                        b'<test-run failed="0" passed="7" result="Failed"/>'):
            with self.subTest(content=content):
                self.put(self.tests, content)
                with self.assertRaises(ValueError):
                    self.curate()
                self.assertFalse(self.evidence.exists())

    def test_player_failure_or_runtime_errors_rejected(self):
        report = self.models / "items-vehicles-v8-report.json"
        for content in (b'{"passed":false,"runtimeErrors":[]}',
                        b'{"passed":true,"runtimeErrors":["error"]}'):
            self.put(report, content)
            with self.assertRaises(ValueError):
                self.curate()
            self.assertFalse(self.evidence.exists())

    def test_missing_runtime_and_bad_crc_rejected(self):
        self.curate()
        (self.build / "GameAssembly.dll").unlink()
        with self.assertRaisesRegex(ValueError, "runtime"):
            self.package(self.source_list)
        with patch.object(zipfile.ZipFile, "testzip", return_value="corrupt-entry"):
            with self.assertRaisesRegex(ValueError, "CRC verification failed"):
                packer.write_zip(self.root / "work/corrupt.zip", [(self.tests, "tests.xml")])

    def test_output_containment_and_overlap(self):
        self.curate()
        for output in (self.root / "outside", self.build / "nested", self.evidence / "nested", self.root / "work"):
            with self.subTest(output=output), self.assertRaises(ValueError):
                self.package(self.source_list, output)
        with self.assertRaisesRegex(ValueError, "overlap"):
            curator.curate(self.root, self.models, self.ordinary, self.models / "nested", self.tests)


if __name__ == "__main__":
    unittest.main(verbosity=2)
