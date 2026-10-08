using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityCrewGeometryTests
    {
        [Test]
        public void MaximumCrewDetailsShareOneMeshAndKeepAllSixParts()
        {
            var first = new GameObject("FirstCrew");
            var second = new GameObject("SecondCrew");
            try
            {
                UpgradeModelParts.Crew(first.transform, 3, 3, 3);
                UpgradeModelParts.Crew(second.transform, 3, 3, 3);
                var renderer = first.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderer.Length, Is.EqualTo(1));
                var mesh = first.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.That(mesh.subMeshCount, Is.EqualTo(1));
                Assert.That(mesh.triangles.Length / 3, Is.EqualTo(6 * 12));
                Assert.That(mesh.colors.Length, Is.EqualTo(0));
                Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.bounds.min.x, Is.LessThan(-.2f));
                Assert.That(mesh.bounds.max.y, Is.GreaterThan(1.38f));
                Assert.That(second.GetComponentInChildren<MeshFilter>().sharedMesh, Is.SameAs(mesh));
                Assert.That(second.GetComponentInChildren<MeshRenderer>().sharedMaterial,
                    Is.SameAs(renderer[0].sharedMaterial));
                Assert.That(first.GetComponentsInChildren<Collider>(), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void InitialCrewHasNoUpgradeDetailsAndTierTwoHasThreeParts()
        {
            var root = new GameObject("InitialCrew");
            try
            {
                UpgradeModelParts.Crew(root.transform, 1, 1, 1);
                Assert.That(root.transform.childCount, Is.Zero);
                UpgradeModelParts.Crew(root.transform, 2, 2, 2);
                Assert.That(root.GetComponentInChildren<MeshFilter>().sharedMesh.triangles.Length / 3,
                    Is.EqualTo(3 * 12));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
