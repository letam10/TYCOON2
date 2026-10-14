using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class CityCivicModelTests
    {
        GameObject root;
        GameSession previous;
        GameSession game;

        [SetUp]
        public void SetUp()
        {
            previous = GameSession.Instance;
            root = new GameObject("CivicModelTests");
            game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.IsQa = true;
            game.IsOrdinaryPlayCheck = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            GameSession.Instance = previous;
        }

        [TestCase("Police", 38, 34)]
        [TestCase("Fire", 38, 38)]
        [TestCase("Bar", 32, 30)]
        [TestCase("Arcade", 40, 32)]
        [TestCase("Playground", 40, 32)]
        [TestCase("Monument", 30, 30)]
        public void ModelsStayGroundedInsideTheirReservedFootprints(string model, float width, float depth)
        {
            Build(Model(model));
            AssertFootprint(Vector3.zero, width, depth);
            Assert.That(root.GetComponentsInChildren<Light>(), Is.Empty);
        }

        [TestCase("Police", -15, -9.6f)]
        [TestCase("Fire", -17, -9.6f)]
        [TestCase("Bar", -14, -7.6f)]
        [TestCase("Arcade", -17, -10.6f)]
        public void CentralApproachesRemainClearForPedestrians(string model, float start, float end)
        {
            Build(Model(model));
            for (float z = start; z <= end; z += .2f)
                AssertClear(new Bounds(new(0, 1, z), new(1.6f, 1.8f, .65f)));
        }

        [Test]
        public void BenchBackHasContinuousSupportsFromFeetToTopSlat()
        {
            Build(g => CityProps.Bench(g, Vector3.zero));
            var surfaces = SurfaceColliders();
            foreach (float x in new[] { -.95f, .95f })
            {
                foreach (float y in new[] { .06f, .2f, .4f, .7f, .96f, 1.18f })
                    Assert.That(Hits(surfaces, new Ray(new(x, y, 1), Vector3.back), .75f), Is.True,
                        $"Missing visible back support at x={x}, y={y}");
            }
        }

        [Test]
        public void OddLengthRailingHasARealEndPost()
        {
            Build(g => CityLeisureModels.Railing(g, Vector3.zero, new(5.3f, 0, 0)));
            Assert.That(Hits(SurfaceColliders(), new Ray(new(5.3f, .7f, 1), Vector3.back), 1.1f), Is.True);
        }

        [Test]
        public void ParkFitsItsReservationAndHasAClearLoopAroundTheFountain()
        {
            var space = new CityDressingSpace(game, root.transform);
            var park = typeof(CityLandscape).GetMethod("Park", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(park, Is.Not.Null);
            Build(g => park.Invoke(null, new object[] { g, space }));
            Vector3 center = CityDistricts.ParkCenter;
            AssertFootprint(center, 23, 14);
            Assert.That(root.GetComponentsInChildren<BoxCollider>().Count(x => x.name == "BenchCollision"),
                Is.EqualTo(4));
            for (int angle = 0; angle < 360; angle += 10)
            {
                float radians = angle * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(Mathf.Cos(radians) * 4.2f, 1, Mathf.Sin(radians) * 4.2f);
                AssertClear(new Bounds(point, new(.65f, 1.8f, .65f)));
            }
        }

        [Test]
        public void ChamfersHaveFiniteNormalsAndNoCollapsedFaces()
        {
            Build(g => CityCivicDetails.ChamferedBox(g, Vector3.up * .5f,
                new(2.5f, .1f, .15f), "wood", "#817154"));
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                Assert.That(mesh.normals.All(n => float.IsFinite(n.x) && n.sqrMagnitude > .9f), Is.True);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]];
                    Vector3 b = vertices[triangles[i + 1]];
                    Vector3 c = vertices[triangles[i + 2]];
                    Assert.That(Vector3.Cross(b - a, c - a).sqrMagnitude, Is.GreaterThan(1e-12f));
                }
            }
        }

        void Build(Action<CityStaticGeometry> action)
        {
            var geometry = new CityStaticGeometry(root.transform);
            action(geometry);
            geometry.Finish();
            Physics.SyncTransforms();
        }

        void AssertFootprint(Vector3 center, float width, float depth)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers, Is.Not.Empty);
            foreach (var renderer in renderers)
                Check(renderer.bounds, renderer.name);
            foreach (var collider in root.GetComponentsInChildren<BoxCollider>())
                Check(collider.bounds, collider.name);

            void Check(Bounds bounds, string label)
            {
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-.001f), label);
                Assert.That(Mathf.Abs(bounds.center.x - center.x) + bounds.extents.x,
                    Is.LessThanOrEqualTo(width * .5f + .001f), label);
                Assert.That(Mathf.Abs(bounds.center.z - center.z) + bounds.extents.z,
                    Is.LessThanOrEqualTo(depth * .5f + .001f), label);
            }
        }

        void AssertClear(Bounds pedestrian)
        {
            foreach (var collider in root.GetComponentsInChildren<BoxCollider>())
                Assert.That(collider.bounds.Intersects(pedestrian), Is.False,
                    $"{collider.name} blocks pedestrian at {pedestrian.center}");
        }

        MeshCollider[] SurfaceColliders()
        {
            var surfaces = root.GetComponentsInChildren<MeshFilter>().Select(filter =>
            {
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                return collider;
            }).ToArray();
            Physics.SyncTransforms();
            return surfaces;
        }

        static bool Hits(MeshCollider[] surfaces, Ray ray, float distance)
        {
            return surfaces.Any(collider => collider.Raycast(ray, out _, distance));
        }

        static Action<CityStaticGeometry> Model(string model)
        {
            return model switch
            {
                "Police" => CityCivicModels.Police,
                "Fire" => CityCivicModels.Fire,
                "Bar" => CityCivicModels.Bar,
                "Arcade" => CityCivicModels.Arcade,
                "Playground" => CityLeisureModels.Playground,
                "Monument" => CityLeisureModels.Monument,
                _ => throw new ArgumentOutOfRangeException(nameof(model))
            };
        }
    }
}
