using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class CityCarryTests
    {
        GameObject root;
        static IEnumerable<string> Goods => Definitions.Items.Select(item => item.id);

        [SetUp]
        public void SetUp() => root = new GameObject("CityCarryTests");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void AllGoodsMoneyAndCrateUseOneTexturedLitMaterialAndOneDrawMesh()
        {
            Material common = null;
            foreach (string id in Goods.Concat(new[] { "cash", "crate" }))
            {
                var first = HandItemModels.Build(id, root.transform);
                var second = HandItemModels.Build(id, root.transform);
                var mesh = first.GetComponent<MeshFilter>().sharedMesh;
                var renderer = first.GetComponent<MeshRenderer>();
                if (!common) common = renderer.sharedMaterial;
                Assert.That(renderer.sharedMaterial, Is.SameAs(common), id);
                Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(1), id);
                Assert.That(mesh.subMeshCount, Is.EqualTo(1), id);
                Assert.That(mesh, Is.SameAs(second.GetComponent<MeshFilter>().sharedMesh), id);
                Assert.That(mesh.vertexCount, Is.LessThan(10000), id);
                Assert.That(mesh.tangents.Length, Is.EqualTo(mesh.vertexCount), id);
                Assert.That(renderer.shadowCastingMode,
                    Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.On), id);
                Assert.That(mesh.uv.All(uv => uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1), Is.True, id);
            }
            Assert.That(HandItemModels.VisibleSize, Is.GreaterThan(.38f));
            Assert.That(common.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(common.enableInstancing, Is.True);
            Assert.That(common.IsKeywordEnabled("_NORMALMAP"), Is.True);
            Assert.That(common.IsKeywordEnabled("_METALLICSPECGLOSSMAP"), Is.True);
            foreach (string texture in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
            {
                var map = common.GetTexture(texture) as Texture2D;
                Assert.That(map, Is.Not.Null, texture);
                Assert.That(map.width, Is.GreaterThanOrEqualTo(512), texture);
                Assert.That(map.GetPixels32().Distinct().Take(5).Count(), Is.GreaterThan(4), texture);
            }
        }

        [TestCaseSource(nameof(Goods))]
        public void FortyEightModelsHaveSeparateVisibleSlotsInTwoStaggeredRows(string id)
        {
            var inventory = new Inventory(48, true);
            Assert.That(inventory.TryAdd(id, 48), Is.True);
            int revision = inventory.Revision;
            var stack = root.AddComponent<InventoryStack>();
            stack.Inventory = inventory;
            stack.Pool = new ItemPool(root.transform);
            stack.HeldLayout = true;
            stack.Columns = 3;
            stack.Rows = 2;
            stack.Scale = .85f;
            stack.LayerHeight = .04f;
            stack.Spacing = .14f;
            stack.Refresh();
            var models = root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(models.Length, Is.EqualTo(48));
            Assert.That(stack.VisibleCount, Is.EqualTo(48));
            var bounds = models[0].bounds;
            for (int i = 0; i < models.Length; i++)
            {
                bounds.Encapsulate(models[i].bounds);
                Assert.That(stack.VisibleItemId(i), Is.EqualTo(id));
                for (int j = i + 1; j < models.Length; j++)
                    Assert.That(models[i].bounds.Intersects(models[j].bounds), Is.False,
                        id + " overlaps at " + i + "/" + j);
            }
            Assert.That(bounds.size.x, Is.LessThan(1.9f));
            Assert.That(bounds.size.z, Is.LessThan(1.1f));
            Assert.That(bounds.size.y, Is.LessThan(1.8f));
            var profile = CarryItemLayout.For(id);
            Assert.That(profile.Rows, Is.EqualTo(2));
            Assert.That(models[0].transform.localPosition.x,
                Is.Not.EqualTo(models[profile.Columns].transform.localPosition.x));
            Assert.That(stack.Top, Is.EqualTo(bounds.max.y).Within(.001f));
            Assert.That(inventory.Total, Is.EqualTo(48));
            Assert.That(inventory.Revision, Is.EqualTo(revision));
            int created = stack.Pool.Created;
            stack.Refresh();
            Assert.That(stack.Pool.Created, Is.EqualTo(created));
        }

        [Test]
        public void PartialTopLayerStaysAlignedWithItsSupportingColumns()
        {
            var inventory = new Inventory(48, true);
            inventory.TryAdd("cloth", 9);
            var stack = root.AddComponent<InventoryStack>();
            stack.Inventory = inventory;
            stack.Pool = new ItemPool(root.transform);
            stack.HeldLayout = true;
            stack.Columns = 3;
            stack.Rows = 2;
            stack.Refresh();
            var models = root.GetComponentsInChildren<MeshRenderer>();
            Vector3 lower = models[2].transform.localPosition;
            Vector3 upper = models[8].transform.localPosition;
            Assert.That(upper.x, Is.EqualTo(lower.x));
            Assert.That(upper.z, Is.EqualTo(lower.z));
            inventory.TryAdd("cloth", 1);
            stack.Refresh();
            Assert.That(models[8].transform.localPosition, Is.EqualTo(upper));
        }

        [Test]
        public void SuddenTurnsAndTeleportsKeepTheStackAtTheHands()
        {
            var anchor = new GameObject("Hands").transform;
            anchor.SetParent(root.transform, false);
            Vector3 origin = new(0, .86f, .78f);
            anchor.localPosition = origin;
            var sway = anchor.gameObject.AddComponent<CarrySway>();
            sway.Actor = root.transform;
            var update = typeof(CarrySway).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            update.Invoke(sway, null);
            root.transform.rotation = Quaternion.Euler(0, 180, 0);
            update.Invoke(sway, null);
            Assert.That(Vector3.Distance(anchor.position, root.transform.TransformPoint(origin)),
                Is.LessThanOrEqualTo(.121f));
            Assert.That(Quaternion.Angle(root.transform.rotation, anchor.rotation), Is.LessThanOrEqualTo(8.01f));
            root.transform.position = new Vector3(60, 0, -20);
            update.Invoke(sway, null);
            Assert.That(Vector3.Distance(anchor.position, root.transform.TransformPoint(origin)), Is.LessThan(.02f));
        }
    }
}
