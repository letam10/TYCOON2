using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class WorldCutawayDirectorTests
    {
        GameObject root;
        GameSession previousSession;

        [SetUp]
        public void SetUp()
        {
            previousSession = GameSession.Instance;
            root = new GameObject("CutawayDirectorTest");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            GameSession.Instance = previousSession;
        }

        [Test]
        public void LandmarkDirectorRegistersUpperShellWithoutHidingInterior()
        {
            var game = root.AddComponent<GameSession>();
            var landmarkObject = new GameObject("Bank");
            landmarkObject.transform.SetParent(root.transform, false);
            var landmark = landmarkObject.AddComponent<CityLandmark>();
            landmark.Id = "Bank";
            landmark.Footprint = new Vector3(38, 12, 38);

            var upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upper.name = "BankRoofShell";
            upper.transform.SetParent(landmarkObject.transform, false);
            upper.transform.localPosition = Vector3.up * 4.2f;
            var interior = GameObject.CreatePrimitive(PrimitiveType.Cube);
            interior.name = "BankInteriorCounter";
            interior.transform.SetParent(landmarkObject.transform, false);
            interior.transform.localPosition = Vector3.up * 1.2f;

            try
            {
                Assert.That(WorldCutawayDirector.Build(game, root.transform), Is.Not.Null);
                WorldCutawayVolume volume = landmarkObject.GetComponent<WorldCutawayVolume>();

                Assert.That(volume, Is.Not.Null);
                Assert.That(volume.upperShellRenderers,
                    Does.Contain(upper.GetComponent<Renderer>()));
                Assert.That(volume.upperShellRenderers,
                    Does.Not.Contain(interior.GetComponent<Renderer>()));
            }
            finally
            {
                Object.DestroyImmediate(upper);
                Object.DestroyImmediate(interior);
            }
        }
    }
}
