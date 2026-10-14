using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityCommercialModelTests
    {
        [Test]
        public void RoundCafeTablesHaveExactlyTwoCloseChairsPerTable()
        {
            var root = new GameObject("CafeTest");
            try
            {
                var g = new CityStaticGeometry(root.transform);
                CityCommercialModels.Cafe(g);
                g.Finish();
                var colliders = root.GetComponentsInChildren<BoxCollider>();
                var tables = colliders.Where(x => x.name == "CafeTableCollision").ToArray();
                var chairs = colliders.Where(x => x.name == "CafeChairCollision").ToArray();
                Assert.That(tables.Length, Is.EqualTo(2));
                Assert.That(chairs.Length, Is.EqualTo(4));
                foreach (var table in tables)
                {
                    var pair = chairs.Where(x => Mathf.Abs(x.bounds.center.x - table.bounds.center.x) < 1.3f)
                        .ToArray();
                    Assert.That(pair.Length, Is.EqualTo(2));
                    Assert.That(pair.All(x => Mathf.Abs(x.bounds.center.z - table.bounds.center.z) < .001f),
                        Is.True);
                    Assert.That(pair.All(x => Mathf.Abs(x.bounds.center.x - table.bounds.center.x) > 1.2f),
                        Is.True);
                    Assert.That(pair.All(x => Mathf.Abs(x.bounds.min.y) < .001f), Is.True);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase("Fishing", 6, 12)]
        [TestCase("Yacht", 7, 15)]
        [TestCase("Cargo", 6, 14)]
        public void BoatsHaveClosedHullFiniteNormalsAndBoundedDetail(string id, float width, float depth)
        {
            var root = new GameObject(id);
            var previous = GameSession.Instance;
            var game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.IsQa = true;
            try
            {
                var g = new CityStaticGeometry(root.transform);
                if (id == "Fishing") CityBoatModels.Fishing(g, Vector3.zero);
                else if (id == "Yacht") CityBoatModels.Yacht(g, Vector3.zero);
                else CityBoatModels.Cargo(g, Vector3.zero);
                g.Finish();
                var renderers = root.GetComponentsInChildren<MeshRenderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(0));
                Assert.That(bounds.size.x, Is.LessThan(width));
                Assert.That(bounds.size.z, Is.LessThan(depth));
                Assert.That(bounds.size.y, Is.LessThan(6));
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    Assert.That(filter.sharedMesh.normals.All(x => float.IsFinite(x.x)
                        && float.IsFinite(x.y) && float.IsFinite(x.z) && x.sqrMagnitude > .5f), Is.True);
                    Assert.That(filter.sharedMesh.vertexCount, Is.LessThan(6000));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                GameSession.Instance = previous;
            }
        }
    }
}
