using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class WorldCutawayVolumeTests
    {
        GameObject root;
        WorldCutawayVolume volume;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("WorldCutawayVolumeTest");
            volume = root.AddComponent<WorldCutawayVolume>();
            volume.enterHysteresisSeconds = 0f;
            volume.exitHysteresisSeconds = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void InsideHidesRendererAndShellObjectThenRestoresBoth()
        {
            GameObject shellObject = new GameObject("UpperShell");
            shellObject.transform.SetParent(root.transform, false);
            MeshRenderer shellRenderer = shellObject.AddComponent<MeshRenderer>();
            volume.upperShellRenderers.Add(shellRenderer);
            volume.upperShellObjects.Add(shellObject);

            volume.SetPlayerInside(true);

            Assert.That(volume.IsRevealed, Is.True);
            Assert.That(shellRenderer.enabled, Is.False);
            Assert.That(shellObject.activeSelf, Is.False);

            volume.SetPlayerInside(false);

            Assert.That(volume.IsRevealed, Is.False);
            Assert.That(shellRenderer.enabled, Is.True);
            Assert.That(shellObject.activeSelf, Is.True);
        }

        [Test]
        public void RestoreKeepsRendererInitiallyDisabled()
        {
            MeshRenderer shellRenderer = CreateRenderer("DisabledShell");
            shellRenderer.enabled = false;
            volume.RegisterRenderer(shellRenderer);

            volume.SetPlayerInside(true);
            volume.SetPlayerInside(false);

            Assert.That(shellRenderer.enabled, Is.False);
        }

        [Test]
        public void HysteresisDelaysTransitionsUntilTheRequestIsSettled()
        {
            MeshRenderer shellRenderer = CreateRenderer("DelayedShell");
            volume.RegisterRenderer(shellRenderer);
            volume.enterHysteresisSeconds = 1f;
            volume.exitHysteresisSeconds = 1f;

            volume.SetPlayerInside(true);
            Assert.That(volume.IsRevealed, Is.False);
            Assert.That(shellRenderer.enabled, Is.True);

            volume.enterHysteresisSeconds = 0f;
            volume.SetPlayerInside(true);
            Assert.That(volume.IsRevealed, Is.True);
            Assert.That(shellRenderer.enabled, Is.False);

            volume.SetPlayerInside(false);
            Assert.That(volume.IsRevealed, Is.True);
            volume.exitHysteresisSeconds = 0f;
            volume.SetPlayerInside(false);
            Assert.That(volume.IsRevealed, Is.False);
            Assert.That(shellRenderer.enabled, Is.True);
        }

        [Test]
        public void RegisterAndUnregisterFollowCurrentRevealState()
        {
            volume.SetPlayerInside(true);
            MeshRenderer shellRenderer = CreateRenderer("LateShell");

            volume.RegisterRenderer(shellRenderer);

            Assert.That(shellRenderer.enabled, Is.False);
            Assert.That(volume.upperShellRenderers.Contains(shellRenderer), Is.True);

            volume.UnregisterRenderer(shellRenderer);

            Assert.That(shellRenderer.enabled, Is.True);
            Assert.That(volume.upperShellRenderers.Contains(shellRenderer), Is.False);
        }

        MeshRenderer CreateRenderer(string name)
        {
            GameObject shell = new GameObject(name);
            shell.transform.SetParent(root.transform, false);
            return shell.AddComponent<MeshRenderer>();
        }
    }
}
