using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class WorldAssetBudgetAuditTests
    {
        [Test]
        public void InspectCountsRuntimeMeshMaterialsAndUniqueTexturePixels()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Texture2D texture;
            Material material = CreateMaterial(4, 2, out texture);
            try
            {
                MeshRenderer renderer = root.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                int expectedVertices = root.GetComponent<MeshFilter>().sharedMesh.vertexCount;

                WorldAssetBudgetRecord record = WorldAssetBudgetAudit.Inspect(root, expectedVertices, 1, 8);

                Assert.That(record.Passed, Is.True, record.Error);
                Assert.That(record.VertexCount, Is.EqualTo(expectedVertices));
                Assert.That(record.MaterialCount, Is.EqualTo(1));
                Assert.That(record.TextureCount, Is.EqualTo(1));
                Assert.That(record.MaxTextureWidth, Is.EqualTo(4));
                Assert.That(record.MaxTextureHeight, Is.EqualTo(2));
                Assert.That(record.TexturePixelCount, Is.EqualTo(8));
                Assert.That(record.BoundsFinite, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InspectReportsMissingRendererMaterialAndMesh()
        {
            GameObject root = new GameObject("MissingRendererRoot");
            GameObject child = new GameObject("MissingMeshChild");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshRenderer>();
            try
            {
                WorldAssetBudgetRecord empty = WorldAssetBudgetAudit.Inspect(root, 100, 10, 100);
                Assert.That(empty.Passed, Is.False);
                Assert.That(empty.MissingRenderer, Is.False);
                Assert.That(empty.HasRenderer, Is.True);
                Assert.That(empty.MissingMaterial, Is.True);
                Assert.That(empty.MissingMeshCount, Is.EqualTo(1));
                StringAssert.Contains("material is missing", empty.Error);
                StringAssert.Contains("mesh is missing", empty.Error);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            GameObject noRenderer = new GameObject("NoRendererRoot");
            try
            {
                WorldAssetBudgetRecord missing = WorldAssetBudgetAudit.Inspect(noRenderer, 100, 10, 100);
                Assert.That(missing.Passed, Is.False);
                Assert.That(missing.MissingRenderer, Is.True);
                Assert.That(missing.HasRenderer, Is.False);
                StringAssert.Contains("renderer is missing", missing.Error);
            }
            finally
            {
                Object.DestroyImmediate(noRenderer);
            }
        }

        [Test]
        public void InspectRejectsBudgetsAndNonFiniteBounds()
        {
            GameObject root = new GameObject("NonFiniteBoundsRoot");
            var filter = root.AddComponent<MeshFilter>();
            root.AddComponent<MeshRenderer>();
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                Vector3.zero,
                Vector3.right,
                Vector3.up
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.bounds = new Bounds(new Vector3(float.NaN, 0f, 0f), Vector3.one);
            filter.sharedMesh = mesh;
            try
            {
                WorldAssetBudgetRecord record = WorldAssetBudgetAudit.Inspect(root, 1, 0, 0);

                Assert.That(record.Passed, Is.False);
                Assert.That(record.NonFiniteBounds, Is.True);
                Assert.That(record.VertexCount, Is.GreaterThan(1));
                StringAssert.Contains("non-finite", record.Error);
                StringAssert.Contains("vertex budget exceeded", record.Error);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ValidateReportsPassFailureNullAndEmptyInputs()
        {
            var passed = new WorldAssetBudgetRecord("Passed") { Passed = true };
            var failed = new WorldAssetBudgetRecord("Failed") { Passed = false, Error = "over budget" };

            Assert.That(WorldAssetBudgetAudit.Validate(new[] { passed }, out string passSummary), Is.True);
            StringAssert.Contains("passed", passSummary);
            Assert.That(WorldAssetBudgetAudit.Validate(new WorldAssetBudgetRecord[] { passed, failed },
                out string failSummary), Is.False);
            StringAssert.Contains("Failed", failSummary);
            Assert.That(WorldAssetBudgetAudit.Validate(new WorldAssetBudgetRecord[] { null },
                out string nullRecordSummary), Is.False);
            StringAssert.Contains("<null-record>", nullRecordSummary);
            Assert.That(WorldAssetBudgetAudit.Validate(null, out string nullSummary), Is.False);
            StringAssert.Contains("records are null", nullSummary);
            Assert.That(WorldAssetBudgetAudit.Validate(new List<WorldAssetBudgetRecord>(),
                out string emptySummary), Is.False);
            StringAssert.Contains("no records", emptySummary);
        }

        static Material CreateMaterial(int width, int height, out Texture2D texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            var material = new Material(shader);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            string property = material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            material.SetTexture(property, texture);
            return material;
        }
    }
}
