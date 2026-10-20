using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Phase3CutawayContractTests
    {
        GameObject root;
        GameSession previousSession;

        [SetUp]
        public void SetUp()
        {
            previousSession = GameSession.Instance;
            root = new GameObject("Phase3CutawayContractTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            GameSession.Instance = previousSession;
        }

        [Test]
        public void CutawayHidesShellKeepsInteriorAndRestoresShellState()
        {
            WorldCutawayVolume volume = root.AddComponent<WorldCutawayVolume>();
            MeshRenderer shell = CreateRenderer("UpperShell");
            MeshRenderer interior = CreateRenderer("GameplayInterior");
            volume.RegisterRenderer(shell);
            volume.RegisterInteriorRenderer(interior);

            volume.SetPlayerInside(true);

            Assert.That(volume.IsRevealed, Is.True);
            Assert.That(shell.enabled, Is.False);
            Assert.That(interior.enabled, Is.True);
            Assert.That(volume.HasInteriorCoverage, Is.True);

            volume.SetPlayerInside(false);

            Assert.That(volume.IsRevealed, Is.False);
            Assert.That(shell.enabled, Is.True);
            Assert.That(interior.enabled, Is.True);
        }

        [Test]
        public void FootprintAndHeightFilterRejectPointsOutsideInteriorVolume()
        {
            WorldCutawayVolume volume = root.AddComponent<WorldCutawayVolume>();
            volume.localFootprint = new Vector3(10f, 6f, 8f);
            volume.localMinHeight = -.5f;
            volume.localMaxHeight = 8f;
            root.transform.position = new Vector3(20f, 0f, -12f);

            Assert.That(volume.ContainsLocalPoint(new Vector3(5f, 1f, 4f)), Is.True);
            Assert.That(volume.ContainsLocalPoint(new Vector3(5.01f, 1f, 0f)), Is.False);
            Assert.That(volume.ContainsLocalPoint(new Vector3(0f, 8.01f, 0f)), Is.False);
            Assert.That(volume.ContainsWorldPoint(root.transform.position + new Vector3(-4f, 1f, -3f)), Is.True);
            Assert.That(volume.ContainsWorldPoint(root.transform.position + new Vector3(0f, 1f, 4.01f)), Is.False);
        }

        [Test]
        public void GameplayBuildingRegistersInteriorCandidateAndUsesFootprintLookup()
        {
            GameSession game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            GameObject building = new GameObject("Bank");
            building.transform.SetParent(root.transform, false);
            CityLandmark landmark = building.AddComponent<CityLandmark>();
            landmark.Id = "Bank";
            landmark.Footprint = new Vector3(20f, 12f, 18f);

            MeshRenderer roof = CreateChildRenderer(building.transform, "BankRoofShell", 5f);
            MeshRenderer counter = CreateChildRenderer(building.transform, "BankInteriorCounter", 1f);

            WorldCutawayDirector director = WorldCutawayDirector.Build(game, root.transform);
            WorldCutawayVolume volume = building.GetComponent<WorldCutawayVolume>();

            Assert.That(director, Is.Not.Null);
            Assert.That(director.RegisteredVolumeCount, Is.EqualTo(1));
            Assert.That(volume, Is.Not.Null);
            Assert.That(volume.localFootprint, Is.EqualTo(landmark.Footprint));
            Assert.That(volume.upperShellRenderers.Contains(roof), Is.True);
            Assert.That(volume.interiorRenderers.Contains(counter), Is.True);
            Assert.That(volume.TryGetComponent<BoxCollider>(out BoxCollider trigger), Is.True);
            Assert.That(director.TryGetVolumeAt(new Vector3(0f, 1f, 0f), out WorldCutawayVolume found), Is.True);
            Assert.That(found, Is.SameAs(volume));
            Assert.That(director.TryGetVolumeAt(new Vector3(11f, 1f, 0f), out _), Is.False);
        }

        MeshRenderer CreateRenderer(string name)
        {
            return CreateChildRenderer(root.transform, name, 0f);
        }

        static MeshRenderer CreateChildRenderer(Transform parent, string name, float y)
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = Vector3.up * y;
            return child.GetComponent<MeshRenderer>();
        }
    }
}
