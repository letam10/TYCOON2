using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class CarryVisualTests
    {
        GameObject root;
        [SetUp]
        public void SetUp() => root = new GameObject("CarryVisualTest");
        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void EveryCatalogItemHasDistinctBoundedGeometryWithoutCollision()
        {
            var meshes = new HashSet<Mesh>();
            foreach (var item in Definitions.Items)
            {
                Assert.That(HandItemModels.Supports(item.id), Is.True, item.id);
                var model = HandItemModels.Build(item.id, root.transform);
                var mesh = model.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(meshes.Add(mesh), Is.True, item.id + " must have its own geometry");
                Assert.That(mesh.vertexCount, Is.GreaterThan(20), item.id);
                Assert.That(model.GetComponentsInChildren<Collider>(), Is.Empty, item.id);
                var size = mesh.bounds.size;
                Assert.That(Mathf.Max(size.x, size.y, size.z),
                    Is.EqualTo(CarryItemLayout.ModelSize(item.id)).Within(.001f));
                Assert.That(mesh.bounds.min.y, Is.EqualTo(0).Within(.001f), item.id);
                Assert.That(model.GetComponent<MeshRenderer>().sharedMaterials.Length, Is.GreaterThan(0));
            }
            Assert.That(meshes.Count, Is.EqualTo(27));
        }

        [Test]
        public void FortyEightCarryModelsRemainVisibleAndReuseThePool()
        {
            var inventory = new Inventory(48, true);
            Assert.That(inventory.TryAdd("carrot", 48), Is.True);
            var pool = new ItemPool(root.transform);
            var stack = root.AddComponent<InventoryStack>();
            stack.Inventory = inventory;
            stack.Pool = pool;
            stack.HeldLayout = true;
            stack.Columns = 4;
            stack.Rows = 3;
            stack.Scale = .7f;
            stack.Maximum = 48;
            stack.Refresh();
            Assert.That(stack.VisibleCount, Is.EqualTo(48));
            Assert.That(pool.Created, Is.EqualTo(48));
            stack.Refresh();
            Assert.That(pool.Created, Is.EqualTo(48));
            Assert.That(inventory.TryRemove("carrot", 1), Is.True);
            stack.Refresh();
            Assert.That(stack.VisibleCount, Is.EqualTo(47));
            Assert.That(inventory.TryAdd("carrot", 1), Is.True);
            stack.Refresh();
            Assert.That(stack.VisibleCount, Is.EqualTo(48));
            Assert.That(pool.Created, Is.EqualTo(48));
        }

        [TestCase("cash")]
        [TestCase("crate")]
        public void MoneyAndCargoHaveReusableModels(string id)
        {
            var first = HandItemModels.Build(id, root.transform);
            var second = HandItemModels.Build(id, root.transform);
            Assert.That(first.GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(second.GetComponent<MeshFilter>().sharedMesh));
            Assert.That(first.GetComponentsInChildren<Collider>(), Is.Empty);
        }

        [TestCase("farm_level2", "farm_level3")]
        [TestCase("mill_speed2", "mill_speed3")]
        [TestCase("carry10", "carry16")]
        [TestCase("carry16", "carry24")]
        public void PurchaseChainUsesExistingCatalogIds(string current, string next)
        {
            Assert.That(PurchasePad.NextUpgrade(Definitions.Upgrade(current))?.id, Is.EqualTo(next));
        }

        [Test]
        public void FinalPurchaseHasNoNextTierAndRampStopsAtFiveTimes()
        {
            Assert.That(PurchasePad.NextUpgrade(Definitions.Upgrade("carry24")), Is.Null);
            Assert.That(PurchasePad.ContributionRate(1200, 0), Is.EqualTo(100));
            Assert.That(PurchasePad.ContributionRate(1200, 1.25f), Is.EqualTo(300));
            Assert.That(PurchasePad.ContributionRate(1200, 2.5f), Is.EqualTo(500));
            Assert.That(PurchasePad.ContributionRate(1200, 20), Is.EqualTo(500));
        }
    }
}
