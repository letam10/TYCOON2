using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityBuildingGeometryTests
    {
        GameObject root;
        CityStaticGeometry geometry;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("BuildingGeometryTest");
            geometry = new CityStaticGeometry(root.transform);
        }

        [TearDown]
        public void TearDown()
        {
            geometry.Finish();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void PitchedRoofHasContinuousSlopedCoverageFromEavesToRidge()
        {
            CityBuildingDetails.PitchedRoof(geometry, Vector3.zero, 12, 10, 5, "#58685E", "#D6C9AB");
            var field = typeof(CityStaticGeometry).GetField("batches", BindingFlags.Instance | BindingFlags.NonPublic);
            var batches = (Dictionary<(Material, int, int), List<CombineInstance>>)field.GetValue(geometry);
            var solids = batches.Values.SelectMany(x => x).ToArray();
            var unitBox = new Bounds(Vector3.zero, Vector3.one);
            // Điểm mẫu phủ cả đỉnh lẫn giữa các hàng ngói; mái bậc thang cũ có khe ở đây.
            for (int sample = 0; sample <= 80; sample++)
            {
                float z = -5.4f + sample * 10.8f / 80;
                float y = 5 + 2.2f * (1 - Mathf.Abs(z) / 5.4f);
                foreach (float x in new[] { -5.8f, 0, 5.8f })
                {
                    Vector3 point = new(x, y, z);
                    Assert.That(solids.Any(s => unitBox.Contains(s.transform.inverse.MultiplyPoint3x4(point))),
                        Is.True, "Roof gap at " + point);
                }
            }
            Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty,
                "Roof decoration above walking height does not need physics shapes.");
        }

        [Test]
        public void HouseFoundationSupportsWallsAndPorchWithoutBlockingOutsideItsFootprint()
        {
            Invoke("House", geometry, Vector3.zero, 12f, 10f, 7f, "#D6C9AB", "#58685E", true);
            Physics.SyncTransforms();
            var foundation = root.transform.Find("BuildingFoundationCollision").GetComponent<BoxCollider>();
            var wall = root.transform.Find("BuildingCollision").GetComponent<BoxCollider>();
            Assert.That(foundation.size, Is.EqualTo(new Vector3(12.6f, .34f, 10.6f)));
            Assert.That(foundation.bounds.min.y, Is.EqualTo(.06f).Within(.0001f));
            Assert.That(foundation.bounds.max.y, Is.EqualTo(wall.bounds.min.y).Within(.0001f));
            var porch = root.transform.Find("PorchStepCollision").GetComponent<BoxCollider>();
            Assert.That(porch.bounds.min.y, Is.EqualTo(.06f).Within(.0001f));
            foreach (var collider in root.GetComponentsInChildren<BoxCollider>())
            {
                Assert.That(collider.bounds.min.x, Is.GreaterThanOrEqualTo(-8.5f));
                Assert.That(collider.bounds.max.x, Is.LessThanOrEqualTo(8.5f));
                Assert.That(collider.bounds.min.z, Is.GreaterThanOrEqualTo(-8));
                Assert.That(collider.bounds.max.z, Is.LessThanOrEqualTo(8));
            }
        }

        [Test]
        public void GreenhouseBedsRestOnSolidFloorAndLeaveCentralAisleOpen()
        {
            Invoke("Greenhouse", geometry, Vector3.zero);
            Physics.SyncTransforms();
            var floor = root.transform.Find("GreenhouseFloorCollision").GetComponent<BoxCollider>();
            var beds = root.GetComponentsInChildren<BoxCollider>()
                .Where(x => x.name == "GreenhouseBedCollision").ToArray();
            Assert.That(beds, Has.Length.EqualTo(6));
            foreach (var bed in beds)
            {
                Assert.That(bed.size, Is.EqualTo(new Vector3(2.4f, .5f, 4.3f)));
                Assert.That(bed.bounds.min.y, Is.EqualTo(floor.bounds.max.y).Within(.0001f));
                Assert.That(Mathf.Abs(bed.bounds.center.x) - bed.bounds.extents.x, Is.GreaterThan(1.2f));
            }
            for (float z = -8; z <= 8; z += .5f)
                Assert.That(Physics.CheckCapsule(new Vector3(0, .5f, z), new Vector3(0, 1.7f, z), .3f),
                    Is.False, "Central greenhouse aisle is blocked at z=" + z);
        }

        static void Invoke(string name, params object[] parameters)
        {
            typeof(CityArchitecture).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, parameters);
        }
    }
}
