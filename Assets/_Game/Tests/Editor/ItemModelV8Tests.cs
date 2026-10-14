using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class ItemModelV8Tests
    {
        GameObject root;
        static IEnumerable<string> Goods => Definitions.Items.Select(x => x.id).Concat(new[] { "cash", "crate" });

        [SetUp]
        public void SetUp() => root = new GameObject("ItemModelV8Tests");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [TestCaseSource(nameof(Goods))]
        public void GeometryFitsItsExactSizeAndHasCompleteFiniteVertexData(string id)
        {
            var item = HandItemModels.Build(id, root.transform);
            var mesh = item.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.subMeshCount, Is.EqualTo(1), id);
            Assert.That(mesh.normals.Length, Is.EqualTo(mesh.vertexCount), id);
            Assert.That(mesh.tangents.Length, Is.EqualTo(mesh.vertexCount), id);
            Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount), id);
            var bounds = new Bounds(mesh.vertices[0], Vector3.zero);
            foreach (var vertex in mesh.vertices)
            {
                Assert.That(float.IsNaN(vertex.sqrMagnitude) || float.IsInfinity(vertex.sqrMagnitude), Is.False);
                bounds.Encapsulate(vertex);
            }
            Assert.That(bounds.min.y, Is.EqualTo(0).Within(.00001f), id);
            Assert.That(Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z),
                Is.EqualTo(CarryItemLayout.ModelSize(id)).Within(.00001f), id);
            Assert.That(mesh.triangles.All(index => index >= 0 && index < mesh.vertexCount), Is.True, id);
        }

        [TestCaseSource(nameof(Goods))]
        public void EverySourceAndFinalModelHasFiniteNonzeroUnitNormals(string id)
        {
            var shapes = new GameObject("NormalSources");
            shapes.transform.SetParent(root.transform, false);
            typeof(HandItemModels).GetMethod("BuildShapes", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { id, shapes.transform });
            foreach (var filter in shapes.GetComponentsInChildren<MeshFilter>())
            {
                var source = filter.sharedMesh;
                Assert.That(source.triangles.Distinct().Count(), Is.EqualTo(source.vertexCount), source.name);
                AssertUnitNormals(source);
            }
            var final = HandItemModels.Build(id, root.transform).GetComponent<MeshFilter>().sharedMesh;
            AssertUnitNormals(final);
        }

        static void AssertUnitNormals(Mesh mesh)
        {
            var normals = mesh.normals;
            Assert.That(normals.Length, Is.EqualTo(mesh.vertexCount), mesh.name);
            for (int i = 0; i < normals.Length; i++)
            {
                float length = normals[i].magnitude;
                Assert.That(float.IsNaN(length) || float.IsInfinity(length), Is.False, mesh.name + "/" + i);
                Assert.That(length, Is.EqualTo(1).Within(.0001f), mesh.name + "/" + i);
            }
        }

        [TestCaseSource(nameof(Goods))]
        public void PartialLayersKeepSupportAndAllItemsKeepTheSameOrientation(string id)
        {
            var item = HandItemModels.Build(id, root.transform);
            var mesh = item.GetComponent<MeshFilter>().sharedMesh;
            var profile = CarryItemLayout.For(id);
            var bounds = profile.Bounds(mesh);
            Assert.That(profile.Columns, Is.EqualTo(2));
            Assert.That(profile.Rows, Is.EqualTo(3));
            foreach (int count in new[] { 1, 5, 7, 11, 24, 47, 48 })
            {
                for (int i = 0; i < count; i++)
                {
                    var p = profile.Position(i, count, bounds);
                    Assert.That(p.y + bounds.min.y,
                        Is.EqualTo(i / 6 * (bounds.size.y + profile.Spacing.y)).Within(.00001f));
                    if (i >= 6)
                    {
                        var lower = profile.Position(i - 6, count, bounds);
                        Assert.That(p.x, Is.EqualTo(lower.x).Within(.00001f));
                        Assert.That(p.z, Is.EqualTo(lower.z).Within(.00001f));
                    }
                    for (int j = 0; j < i; j++)
                    {
                        var other = profile.Position(j, count, bounds);
                        Assert.That(new Bounds(p + bounds.center, bounds.size)
                            .Intersects(new Bounds(other + bounds.center, bounds.size)), Is.False, id);
                    }
                }
            }
        }

        [TestCase("meal")]
        [TestCase("pasta")]
        [TestCase("soy_vegetables")]
        [TestCase("corn_soup")]
        [TestCase("beef_soy")]
        [TestCase("egg_sandwich")]
        [TestCase("cake_batter")]
        public void PreparedFoodKeepsItsDishUpright(string id)
        {
            Assert.That(Vector3.Dot(CarryItemLayout.For(id).Rotation * Vector3.up, Vector3.up),
                Is.GreaterThan(.999f));
        }

        [Test]
        public void PastaHasCurvedStrandsWithContinuousIntermediateDirections()
        {
            typeof(HandItemModels).GetMethod("Pasta", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { root.transform });
            var strand = root.transform.Find("Curved pasta ribbon").GetComponent<MeshFilter>().sharedMesh;
            var vertices = strand.vertices;
            Assert.That(vertices.Select(v => Mathf.Round(v.x * 1000)).Distinct().Count(), Is.GreaterThan(20));
            Assert.That(vertices.Select(v => Mathf.Round(v.z * 1000)).Distinct().Count(), Is.GreaterThan(20));
            Assert.That(strand.normals.Select(v => Mathf.Round(v.x * 100)).Distinct().Count(), Is.GreaterThan(12));
        }

        [Test]
        public void CrateHasAnOpenCargoCavityAboveItsFloor()
        {
            typeof(HandItemModels).GetMethod("Crate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { root.transform });
            var cavity = new Bounds(new Vector3(0, .26f, 0), new Vector3(.40f, .30f, .28f));
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                Assert.That(renderer.bounds.Intersects(cavity), Is.False, renderer.name);
        }
    }
}
