using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class OceanContinuityTests
    {
        [TestCase(60, 0)]
        [TestCase(200, -240)]
        [TestCase(200, 240)]
        public void DistantCoastalViewsRemainOverWater(float beyondX, float offsetZ)
        {
            var root = new GameObject("OceanContinuity");
            try
            {
                CityOceanEdge.Build(root.transform);
                var bounds = CityDistricts.WorldBounds;
                Vector3 point = new(bounds.max.x + beyondX, .0525f, bounds.center.z + offsetZ);
                bool covered = root.GetComponentsInChildren<MeshRenderer>().Any(renderer =>
                    renderer.sharedMaterial.shader.name == "TYCOON/Town Water"
                    && renderer.bounds.Contains(point));
                Assert.That(covered, Is.True, "Far coastal cameras must not reveal an empty rectangular background.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OpenSeaUsesTheSameBaseTintAsTheHarborAndMarina()
        {
            var root = new GameObject("OceanTint");
            try
            {
                CityOceanEdge.Build(root.transform);
                Color tint = Art.Hex(CityOceanHorizon.Tint);
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>()
                    .Where(renderer => renderer.sharedMaterial.shader.name == "TYCOON/Town Water"))
                {
                    Color actual = renderer.sharedMaterial.GetColor("_BaseColor");
                    Assert.That(Vector4.Distance(actual, tint), Is.LessThan(1e-5f));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(263, -260, "TYCOON/Town Water")]
        [TestCase(263, 260, "TYCOON/Town Water")]
        [TestCase(200, -260, "TYCOON/Town Surface")]
        [TestCase(200, 260, "TYCOON/Town Surface")]
        public void LateralCoastViewsHaveDecorativeContext(float x, float offsetZ, string shader)
        {
            var root = new GameObject("OceanLateralContext");
            try
            {
                CityOceanEdge.Build(root.transform);
                Vector3 point = new(x, shader.EndsWith("Water") ? .0525f : 0,
                    CityDistricts.WorldBounds.center.z + offsetZ);
                Assert.That(root.GetComponentsInChildren<MeshRenderer>().Any(renderer =>
                    renderer.sharedMaterial.shader.name == shader && renderer.bounds.Contains(point)), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DecorativeSeaDoesNotExpandPlayableCollisionBoundaries()
        {
            var root = new GameObject("OceanPhysicalBoundaries");
            try
            {
                CityOceanEdge.Build(root.transform);
                var bounds = CityDistricts.WorldBounds;
                var colliders = root.GetComponentsInChildren<BoxCollider>();
                Assert.That(colliders.Length, Is.EqualTo(3));
                foreach (var collider in colliders)
                {
                    Assert.That(collider.bounds.max.x, Is.LessThanOrEqualTo(bounds.max.x + 3.01f));
                    Assert.That(collider.bounds.min.z, Is.GreaterThanOrEqualTo(bounds.min.z - 3.01f));
                    Assert.That(collider.bounds.max.z, Is.LessThanOrEqualTo(bounds.max.z + 3.01f));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
