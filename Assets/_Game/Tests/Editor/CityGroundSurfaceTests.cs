using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityGroundSurfaceTests
    {
        [TestCase("StarterField")]
        [TestCase("PenFloor")]
        public void NaturalFarmAndAnimalGroundKeepGrassInsteadOfSolidBrownFloor(string name)
        {
            var root = new GameObject("FarmGrassTest");
            var plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plane.transform.SetParent(root.transform, false);
            plane.name = name;
            try
            {
                var renderer = plane.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = TownSurfaceMaterials.Paint("#A2C587");
                TownSurfaceMaterials.Apply(root.transform);
                Assert.That(renderer.sharedMaterial.GetFloat("_SurfaceKind"), Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GrassCoversOriginalWorldSurfaceWithoutAddingGeometryOrColliders()
        {
            var root = new GameObject("GroundSurfaceTest");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Grass";
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(8, -.13f, 22);
            ground.transform.localScale = new Vector3(CityDistricts.GroundWidth, .25f, CityDistricts.GroundDepth);
            var renderer = ground.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = TownSurfaceMaterials.Paint("#94B985");
            var mesh = ground.GetComponent<MeshFilter>().sharedMesh;
            var collider = ground.GetComponent<Collider>();
            try
            {
                TownSurfaceMaterials.Apply(root.transform);
                Assert.That(root.GetComponentsInChildren<MeshFilter>(), Has.Length.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<Collider>(), Has.Length.EqualTo(1));
                Assert.That(ground.GetComponent<Collider>(), Is.SameAs(collider));
                Assert.That(ground.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
                Assert.That(renderer.bounds.size.x, Is.EqualTo(CityDistricts.GroundWidth).Within(.01f));
                Assert.That(renderer.bounds.size.z, Is.EqualTo(CityDistricts.GroundDepth).Within(.01f));
                var material = renderer.sharedMaterial;
                Assert.That(material.GetFloat("_SurfaceKind"), Is.EqualTo(1));
                Assert.That(material.GetFloat("_GravelDensity"), Is.GreaterThan(0));
                var soil = material.GetColor("_SoilColor");
                Assert.That(Vector4.Distance(soil, Art.Hex("#877354")), Is.LessThan(.000001f));
                var texture = (Texture2D)material.GetTexture("_SurfaceMap");
                Assert.That(texture.mipmapCount, Is.GreaterThan(1));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
                Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
                Assert.That(material.GetFloat("_BumpScale"), Is.LessThanOrEqualTo(.02f));
                Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase("grass", 1)]
        [TestCase("asphalt", 2)]
        [TestCase("paving", 3)]
        [TestCase("soil", 0)]
        [TestCase("wood", 0)]
        public void ProceduralGroundPatternsKeepSurfaceIdentityAndCachedMaterials(string surface, int kind)
        {
            var material = TownSurfaceMaterials.Get(surface, "#77916B");
            Assert.That(material.GetFloat("_SurfaceKind"), Is.EqualTo(kind));
            Assert.That(material, Is.SameAs(TownSurfaceMaterials.Get(surface, "#77916B")));
            Assert.That(material.shader.name, Is.EqualTo("TYCOON/Town Surface"));
            Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False);
        }

        [Test]
        public void ExistingBatchedStreetMaterialReceivesCracksWithoutChangingItsSurfaceId()
        {
            var road = TownSurfaceMaterials.Get("stone", "#666C6C");
            var rock = TownSurfaceMaterials.Get("stone", "#92958A");
            Assert.That(road.name, Is.EqualTo("TownSurface_stone:#666C6C"));
            Assert.That(road.GetFloat("_SurfaceKind"), Is.EqualTo(2));
            Assert.That(rock.GetFloat("_SurfaceKind"), Is.Zero);
        }

    }
}
