using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tycoon.Tests
{
    public sealed class VisualMaterialTests
    {
        [Test]
        public void RecreatesDestroyedSharedPropMesh()
        {
            var root = new GameObject("MeshLifecycle");
            try
            {
                Vector3 size = new(.31f, .73f, 1.17f);
                var first = TownModelParts.Box("Before", Vector3.zero, size, "wood", "#ABCD12", root.transform);
                var previous = first.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(previous);
                var second = TownModelParts.Box("After", Vector3.zero, size, "wood", "#ABCD12", root.transform);
                var current = second.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(current, Is.Not.Null);
                Assert.That(current.vertexCount, Is.GreaterThan(0));
                Assert.That(current, Is.Not.SameAs(previous));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RecreatesDestroyedGeneratedResourcesAfterSceneLifecycle()
        {
            var paint = TownSurfaceMaterials.Paint("#ABCD12");
            Object.DestroyImmediate(paint);
            Assert.That(TownSurfaceMaterials.Paint("#ABCD12"), Is.Not.Null);
            var surface = TownSurfaceMaterials.Get("grass", "#BACD13");
            var texture = surface.GetTexture("_SurfaceMap");
            Object.DestroyImmediate(texture);
            var refreshed = TownSurfaceMaterials.Get("grass", "#BACD13");
            Assert.That(refreshed, Is.SameAs(surface));
            Assert.That(refreshed.GetTexture("_SurfaceMap"), Is.Not.Null);
            Assert.That(refreshed.GetTexture("_SurfaceMap"), Is.Not.SameAs(texture));
            Object.DestroyImmediate(surface);
            Assert.That(TownSurfaceMaterials.Get("grass", "#BACD13"), Is.Not.Null);
        }

        [TestCase("plaster")]
        [TestCase("brick")]
        [TestCase("wood")]
        [TestCase("soil")]
        [TestCase("grass")]
        [TestCase("paving")]
        [TestCase("asphalt")]
        [TestCase("metal")]
        [TestCase("roof")]
        [TestCase("glass")]
        [TestCase("ceramic")]
        [TestCase("stone")]
        public void WorldSurfaceHasReferencedShaderAndHeightDetail(string surface)
        {
            var material = TownSurfaceMaterials.Get(surface, "#B8B2A4");
            Assert.That(material.shader.name, Is.EqualTo("TYCOON/Town Surface"));
            Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False);
            Assert.That(material.GetTexture("_SurfaceMap"), Is.Not.Null);
            Assert.That(material.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0));
            Assert.That(material.FindPass("DepthOnly"), Is.GreaterThanOrEqualTo(0));
            Assert.That(material.FindPass("DepthNormals"), Is.GreaterThanOrEqualTo(0));
            Assert.That(TownSurfaceMaterials.Get(surface, "#B8B2A4"), Is.SameAs(material));
        }

        [Test]
        public void PhysicalMetalAndWaterDifferFromRoughSoil()
        {
            var metal = TownSurfaceMaterials.Get("metal");
            var soil = TownSurfaceMaterials.Get("soil");
            var water = TownSurfaceMaterials.Get("water");
            Assert.That(metal.GetFloat("_Metallic"), Is.GreaterThan(.6f));
            Assert.That(soil.GetFloat("_Metallic"), Is.Zero);
            Assert.That(water.shader.name, Is.EqualTo("TYCOON/Town Water"));
            Assert.That(ShaderUtil.ShaderHasError(water.shader), Is.False);
            Assert.That(water.GetFloat("_Smoothness"), Is.GreaterThan(metal.GetFloat("_Smoothness")));
            Assert.That(metal.GetFloat("_Smoothness"), Is.GreaterThan(soil.GetFloat("_Smoothness")));
        }

        [TestCase("carry10", "farm")]
        [TestCase("milk_line", "livestock")]
        [TestCase("animal_worker", "livestock")]
        [TestCase("conveyor_processing", "processing")]
        [TestCase("storage_supermarket_capacity2", "supermarket")]
        [TestCase("oven_speed2", "bakery")]
        [TestCase("waiter", "restaurant")]
        public void UpgradeBanksBelongToTheirLogicalDistrict(string id, string district)
        {
            Assert.That(TownUpgradeLayout.District(Definitions.Upgrade(id)), Is.EqualTo(district));
        }

        [Test]
        public void PropFinishDoesNotMutateImportedMaterialOrFoodAppearance()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var original = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            original.SetColor("_BaseColor", Color.green);
            var renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = original;
            TownPropSurfaces.Apply(root, "carrot");
            Assert.That(renderer.sharedMaterial, Is.SameAs(original));
            TownPropSurfaces.Apply(root, "mill");
            Assert.That(renderer.sharedMaterial, Is.Not.SameAs(original));
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("TYCOON/Town Surface"));
            Assert.That(original.GetColor("_BaseColor"), Is.EqualTo(Color.green));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(original);
        }
    }
}
