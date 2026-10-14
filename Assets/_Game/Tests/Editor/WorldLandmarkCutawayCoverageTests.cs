using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class WorldLandmarkCutawayCoverageTests
    {
        GameObject root;
        GameSession game;
        GameSession previousSession;

        [SetUp]
        public void SetUp()
        {
            previousSession = GameSession.Instance;
            root = new GameObject("WorldLandmarkCutawayCoverageTests");
            game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.IsQa = true;
            game.IsOrdinaryPlayCheck = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            GameSession.Instance = previousSession;
        }

        [TestCase("Bank", 38f, 12f, 38f)]
        [TestCase("Harbor", 62f, 12f, 66f)]
        [TestCase("Marina", 62f, 12f, 44f)]
        public void ActualLandmarksRegisterUpperShellsOnly(
            string id, float footprintWidth, float footprintHeight, float footprintDepth)
        {
            var landmarkObject = new GameObject(id);
            landmarkObject.transform.SetParent(root.transform, false);
            var landmark = landmarkObject.AddComponent<CityLandmark>();
            landmark.Id = id;
            landmark.Footprint = new Vector3(footprintWidth, footprintHeight, footprintDepth);

            var geometry = new CityStaticGeometry(landmarkObject.transform);
            BuildLandmarkModel(id, geometry);
            geometry.Finish();

            WorldCutawayDirector director = WorldCutawayDirector.Build(game, root.transform);
            WorldCutawayVolume volume = landmarkObject.GetComponent<WorldCutawayVolume>();
            Assert.That(director, Is.Not.Null, id);
            Assert.That(volume, Is.Not.Null, id);
            Assert.That(landmark.Footprint.x, Is.GreaterThan(0f), id);
            Assert.That(landmark.Footprint.y, Is.GreaterThan(0f), id);
            Assert.That(landmark.Footprint.z, Is.GreaterThan(0f), id);

            Renderer[] renderers = landmarkObject.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, id + " has no renderers");
            Renderer[] upperShells = renderers.Where(IsUpperShellCandidate).ToArray();
            Renderer[] interior = renderers.Where(renderer => !IsUpperShellCandidate(renderer)).ToArray();
            Assert.That(upperShells, Is.Not.Empty, id + " has no upper shell candidate");
            Assert.That(interior, Is.Not.Empty, id + " has no low interior candidate");

            foreach (Renderer renderer in upperShells)
                Assert.That(volume.upperShellRenderers.Contains(renderer), Is.True, renderer.name);
            foreach (Renderer renderer in interior)
                Assert.That(volume.upperShellRenderers.Contains(renderer), Is.False, renderer.name);
        }

        static void BuildLandmarkModel(string id, CityStaticGeometry geometry)
        {
            switch (id)
            {
                case "Bank":
                    CityCommercialModels.Bank(geometry);
                    break;
                case "Harbor":
                    CityCoastModels.Harbor(geometry);
                    break;
                case "Marina":
                    CityMarinaModels.Build(geometry);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown landmark model");
            }
        }

        static bool IsUpperShellCandidate(Renderer renderer)
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            string material = renderer.sharedMaterial
                ? renderer.sharedMaterial.name.ToLowerInvariant()
                : string.Empty;
            return name.Contains("roof") || name.Contains("upper") || material.Contains("roof")
                || renderer.bounds.center.y > 3.1f;
        }
    }
}
