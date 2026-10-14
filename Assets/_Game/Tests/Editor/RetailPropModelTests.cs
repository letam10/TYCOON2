using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class RetailPropModelTests
    {
        readonly List<GameObject> owners = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var owner in owners) Object.DestroyImmediate(owner);
            owners.Clear();
        }

        [TestCase("storage_rack")]
        [TestCase("display_counter")]
        [TestCase("checkout_counter")]
        [TestCase("supply_crate")]
        public void GroundedGeometryMatchesAdvertisedSizeAndRenderingBudget(string key)
        {
            var model = Create(key);
            Assert.That(RetailPropModels.Supports(key), Is.True);
            Assert.That(model.transform.localPosition, Is.EqualTo(Vector3.zero));
            Bounds bounds = GeometryBounds(model);
            Assert.That(bounds.min.y, Is.EqualTo(0).Within(.0001f));
            Vector3 error = bounds.size - RetailPropModels.Size(key);
            Assert.That(error.magnitude, Is.LessThan(.0002f), "Size must describe actual geometry");
            AssertGeometry(model);
        }

        [TestCase("storage_rack")]
        [TestCase("display_counter")]
        [TestCase("checkout_counter")]
        [TestCase("supply_crate")]
        public void RepeatedInstancesShareMeshesAndMaterialsAfterTheFirstOwnerIsDestroyed(string key)
        {
            var first = Create(key);
            var meshes = first.GetComponentsInChildren<MeshFilter>().Select(x => x.sharedMesh).ToArray();
            var materials = first.GetComponentsInChildren<Renderer>().Select(x => x.sharedMaterial).ToArray();
            owners.Remove(first);
            Object.DestroyImmediate(first);
            var second = Create(key);
            Assert.That(second.GetComponentsInChildren<MeshFilter>().Select(x => x.sharedMesh),
                Is.EqualTo(meshes));
            Assert.That(second.GetComponentsInChildren<Renderer>().Select(x => x.sharedMaterial),
                Is.EqualTo(materials));
            Assert.That(meshes.All(x => x), Is.True, "Shared mesh must outlive an individual station");
        }

        [TestCase("display_counter", 0, -.25f, 1.2f, 3.4f, 1.04f)]
        [TestCase("checkout_counter", -.25f, .1f, 1.05f, .96f, 1.1f)]
        public void ActualTrianglesSupportEveryStockCornerAndCenterAtTheRequiredHeight(
            string key, float x, float z, float width, float depth, float height)
        {
            var model = Create(key);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var point = new Vector2(x + i * width * .5f, z + j * depth * .5f);
                    Assert.That(HighestSurface(model, point), Is.EqualTo(height).Within(.0001f),
                        "The stock plane must be supported and unobstructed: " + point);
                }
        }

        [Test]
        public void StorageStaysBehindTheRealFloorStockAndLeavesUpgradeShelfClearance()
        {
            var rack = Create("storage_rack");
            Bounds bounds = GeometryBounds(rack);
            Assert.That(bounds.min.z + .65f, Is.GreaterThan(.2f));
            foreach (var filter in rack.GetComponentsInChildren<MeshFilter>())
            {
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    bool insideUpgrade = Mathf.Abs(vertex.x) < 1.225f && Mathf.Abs(vertex.z) < .325f;
                    if (!insideUpgrade) continue;
                    Assert.That(vertex.y < .805f || vertex.y > .895f, Is.True,
                        "First upgrade shelf intersects base geometry");
                    Assert.That(vertex.y < 1.205f || vertex.y > 1.295f, Is.True,
                        "Second upgrade shelf intersects base geometry");
                }
            }
        }

        [Test]
        public void ClosedCrateHasLidAcrossTheCenterAndCorners()
        {
            var crate = Create("supply_crate");
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    Assert.That(HighestSurface(crate, new Vector2(x * .48f, z * .48f)),
                        Is.InRange(.64f, .7001f));
        }

        [Test]
        public void SafeIsGroundedFacesPositiveZAndAddsNoBehaviorOrColliders()
        {
            var parent = new GameObject("SafeTest");
            owners.Add(parent);
            var model = SafeModelDetails.Build(parent.transform);
            Bounds bounds = GeometryBounds(model);
            Assert.That(bounds.min.y, Is.EqualTo(0).Within(.0001f));
            Assert.That(bounds.size.x, Is.EqualTo(1.6f).Within(.0001f));
            Assert.That(bounds.max.y, Is.EqualTo(1.48f).Within(.0001f));
            Assert.That(bounds.min.z, Is.EqualTo(-.65f).Within(.0001f));
            Assert.That(bounds.max.z, Is.InRange(.74f, .77f), "Handle must face local +Z");
            AssertGeometry(model);
            var second = SafeModelDetails.Build(parent.transform);
            Assert.That(second.GetComponentsInChildren<MeshFilter>().Select(x => x.sharedMesh),
                Is.EqualTo(model.GetComponentsInChildren<MeshFilter>().Select(x => x.sharedMesh)));
        }

        GameObject Create(string key)
        {
            var root = RetailPropModels.Build(key);
            owners.Add(root);
            return root;
        }

        static void AssertGeometry(GameObject model)
        {
            Assert.That(model.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(model.GetComponentsInChildren<MonoBehaviour>(), Is.Empty);
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.InRange(2, 6));
            Assert.That(renderers.All(x => x.sharedMaterial && x.sharedMaterial.enableInstancing), Is.True);
            int triangles = 0;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                triangles += indices.Length / 3;
                foreach (var vertex in vertices)
                    Assert.That(float.IsNaN(vertex.sqrMagnitude) || float.IsInfinity(vertex.sqrMagnitude), Is.False);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = vertices[indices[i]];
                    Vector3 b = vertices[indices[i + 1]];
                    Vector3 c = vertices[indices[i + 2]];
                    Assert.That(Vector3.Cross(b - a, c - a).sqrMagnitude, Is.GreaterThan(1e-16f));
                }
            }
            Assert.That(triangles, Is.InRange(100, 6000));
        }

        static Bounds GeometryBounds(GameObject model)
        {
            bool initialized = false;
            var bounds = new Bounds();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = model.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if (!initialized)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                    else bounds.Encapsulate(point);
                }
            Assert.That(initialized, Is.True);
            return bounds;
        }

        // Chiếu tam giác xuống XZ để kiểm tra mặt đỡ thật, không dựa vào tên node hay bounding box.
        static float HighestSurface(GameObject model, Vector2 point)
        {
            float highest = float.NegativeInfinity;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var vertices = filter.sharedMesh.vertices;
                var indices = filter.sharedMesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = vertices[indices[i]];
                    Vector3 b = vertices[indices[i + 1]];
                    Vector3 c = vertices[indices[i + 2]];
                    float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                    if (Mathf.Abs(denominator) < 1e-9f) continue;
                    float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.y - c.z))
                        / denominator;
                    float v = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.y - c.z))
                        / denominator;
                    if (u < -.00001f || v < -.00001f || u + v > 1.00001f) continue;
                    highest = Mathf.Max(highest, u * a.y + v * b.y + (1 - u - v) * c.y);
                }
            }
            return highest;
        }
    }
}
