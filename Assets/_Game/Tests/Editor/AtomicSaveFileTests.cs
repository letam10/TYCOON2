using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class AtomicSaveFileTests
    {
        string directory;
        string destination;
        string temporary;

        [SetUp]
        public void CreateOwnedFiles()
        {
            directory = Path.Combine(Path.GetTempPath(), "tycoon-atomic-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            destination = Path.Combine(directory, "save.json");
            temporary = Path.Combine(directory, "save.json.tmp");
            File.WriteAllText(destination, "old checkpoint");
            File.WriteAllText(temporary, "new checkpoint");
        }

        [TearDown]
        public void RemoveOwnedFiles()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public void ShortReadLockReproducesWindowsFailureAndThenCommitsAtomically()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Ignore("Windows file sharing regression.");
            using var reader = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read);
            IOException original = Assert.Throws<IOException>(() => File.Replace(temporary, destination, null));
            int code = original.HResult & 0xffff;
            TestContext.WriteLine("ReplaceFile error before retry: " + code + " " + original.Message);
            Assert.That(new[] { 32, 33, 1175 }, Does.Contain(code));
            Assert.That(AtomicSaveFile.ReadText(destination), Is.EqualTo("old checkpoint"));
            using var started = new ManualResetEventSlim();
            Task release = Task.Run(() =>
            {
                started.Set();
                Thread.Sleep(30);
                reader.Dispose();
            });
            Assert.That(started.Wait(2000), Is.True);
            try
            {
                AtomicSaveFile.Replace(temporary, destination);
            }
            finally
            {
                release.GetAwaiter().GetResult();
            }
            Assert.That(AtomicSaveFile.ReadText(destination), Is.EqualTo("new checkpoint"));
            Assert.That(File.Exists(temporary), Is.False);
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
        }

        [Test]
        public void PersistentLockReportsFailureAndPreservesBothVersions()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Ignore("Windows file sharing regression.");
            using (var reader = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.Throws<IOException>(() => AtomicSaveFile.Replace(temporary, destination));
                Assert.That(AtomicSaveFile.ReadText(destination), Is.EqualTo("old checkpoint"));
                Assert.That(AtomicSaveFile.ReadText(temporary), Is.EqualTo("new checkpoint"));
            }
        }
    }
}
