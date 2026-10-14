using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class CarryContainerTests
    {
        const float Tolerance = .0001f;
        GameObject root;
        InventoryStack stack;

        static IEnumerable<TestCaseData> FragileCounts()
        {
            foreach (string id in new[] { "egg", "milk", "bottled_milk", "sauce", "soy_sauce" })
                foreach (int count in new[] { 1, 2, 5, 6, 7, 13, 48 })
                    yield return new TestCaseData(id, count);
        }

        static IEnumerable<string> DirectGoods()
        {
            foreach (var item in Definitions.Items)
                if (item.id != "egg" && item.id != "milk" && item.id != "bottled_milk" &&
                    item.id != "sauce" && item.id != "soy_sauce") yield return item.id;
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CarryContainerTest");
            var hand = new GameObject("Hand");
            hand.transform.SetParent(root.transform, false);
            stack = hand.AddComponent<InventoryStack>();
            stack.Inventory = new Inventory(48, true);
            stack.Pool = new ItemPool(root.transform);
            stack.HeldLayout = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [TestCaseSource(nameof(FragileCounts))]
        public void FragileModelsHaveExactCountUprightSupportAndClearCompartments(string id, int count)
        {
            Assert.That(stack.Inventory.TryAdd(id, count), Is.True);
            stack.Refresh();
            var goods = Goods();
            var trays = Trays();
            Assert.That(stack.VisibleCount, Is.EqualTo(count));
            Assert.That(goods.Length, Is.EqualTo(count));
            Assert.That(trays.Length, Is.EqualTo((count + 5) / 6));
            Assert.That(stack.Pool.Created, Is.EqualTo(count));
            var combined = trays[0].bounds;
            foreach (var tray in trays) combined.Encapsulate(tray.bounds);
            for (int i = 0; i < count; i++)
            {
                Assert.That(stack.VisibleItemId(i), Is.EqualTo(id));
                Assert.That(goods[i].name, Is.EqualTo(id));
                Assert.That(Quaternion.Angle(goods[i].transform.localRotation, Quaternion.identity),
                    Is.LessThan(Tolerance));
                Assert.That(goods[i].bounds.min.y,
                    Is.EqualTo(trays[i / 6].bounds.min.y + .012f).Within(Tolerance));
                Assert.That(goods[i].bounds.min.x, Is.GreaterThan(trays[i / 6].bounds.min.x));
                Assert.That(goods[i].bounds.max.x, Is.LessThan(trays[i / 6].bounds.max.x));
                Assert.That(goods[i].bounds.min.z, Is.GreaterThan(trays[i / 6].bounds.min.z));
                Assert.That(goods[i].bounds.max.z, Is.LessThan(trays[i / 6].bounds.max.z));
                combined.Encapsulate(goods[i].bounds);
                for (int j = 0; j < i; j++)
                    Assert.That(Overlaps(goods[i].bounds, goods[j].bounds), Is.False, "Items overlap");
                if (i >= 6)
                {
                    Assert.That(goods[i].bounds.center.x,
                        Is.EqualTo(goods[i % 6].bounds.center.x).Within(Tolerance));
                    Assert.That(goods[i].bounds.center.z,
                        Is.EqualTo(goods[i % 6].bounds.center.z).Within(Tolerance));
                }
            }
            foreach (var tray in trays) AssertClearGeometry(tray, goods);
            Assert.That(stack.Top, Is.EqualTo(combined.max.y).Within(Tolerance));
            Assert.That(stack.RearExtent, Is.EqualTo(-combined.min.z).Within(Tolerance));
            Assert.That(combined.min.y, Is.EqualTo(0).Within(Tolerance));
            Assert.That(combined.size.y, Is.LessThan(1.8f));
            Assert.That(combined.size.x, Is.LessThan(1.9f));
            if (count >= 6)
            {
                Assert.That(goods.Take(6).Select(x => x.bounds.center.x).Distinct().Count(), Is.EqualTo(2));
                Assert.That(goods.Take(6).Select(x => x.bounds.center.z).Distinct().Count(), Is.EqualTo(3));
            }
        }

        [TestCaseSource(nameof(DirectGoods))]
        public void OtherGoodsRemainDirectlyHeld(string id)
        {
            stack.Inventory.TryAdd(id, 7);
            stack.Refresh();
            Assert.That(Trays(true), Is.Empty);
            Assert.That(Goods().Length, Is.EqualTo(7));
            Assert.That(stack.VisibleCount, Is.EqualTo(7));
            Assert.That(Quaternion.Angle(Goods()[0].transform.localRotation, CarryItemLayout.For(id).Rotation),
                Is.LessThan(Tolerance));
        }

        [Test]
        public void StoredFragileGoodsUseNoContainersAndHeldToggleReusesTheSameTray()
        {
            stack.Inventory.TryAdd("egg", 1);
            stack.HeldLayout = false;
            stack.Refresh();
            Assert.That(Trays(true), Is.Empty);
            stack.HeldLayout = true;
            stack.Refresh();
            var tray = Trays()[0];
            stack.HeldLayout = false;
            stack.Refresh();
            Assert.That(Trays(), Is.Empty);
            Assert.That(tray.gameObject.activeSelf, Is.False);
            Assert.That(Goods()[0].bounds.min.y, Is.EqualTo(0).Within(Tolerance));
            stack.HeldLayout = true;
            stack.Refresh();
            Assert.That(Trays()[0], Is.SameAs(tray));
        }

        [Test]
        public void CountChangesSwitchEmptyAndDisableReuseModelsAndContainers()
        {
            stack.Inventory.TryAdd("egg", 13);
            stack.Refresh();
            var trays = Trays();
            var first = Goods()[0];
            var eggMesh = trays[0].GetComponent<MeshFilter>().sharedMesh;
            stack.Refresh();
            Assert.That(Goods()[0], Is.SameAs(first));
            Assert.That(Trays()[0], Is.SameAs(trays[0]));
            stack.Inventory.TryRemove("egg", 12);
            stack.Refresh();
            Assert.That(stack.VisibleCount, Is.EqualTo(1));
            Assert.That(Goods()[0], Is.SameAs(first));
            Assert.That(Trays().Length, Is.EqualTo(1));
            Assert.That(Trays(true).Length, Is.EqualTo(3));
            stack.Inventory.TryAdd("egg", 12);
            stack.Refresh();
            Assert.That(stack.Pool.Created, Is.EqualTo(13));
            CollectionAssert.AreEqual(trays, Trays());
            stack.Inventory.TryRemove("egg", 13);
            stack.Refresh();
            Assert.That(stack.VisibleCount, Is.Zero);
            Assert.That(Goods(), Is.Empty);
            Assert.That(Trays(), Is.Empty);
            Assert.That(stack.Top, Is.Zero);
            Assert.That(stack.RearExtent, Is.Zero);
            stack.Inventory.TryAdd("milk", 1);
            stack.Refresh();
            Assert.That(Trays()[0], Is.SameAs(trays[0]));
            Assert.That(Trays()[0].GetComponent<MeshFilter>().sharedMesh, Is.Not.SameAs(eggMesh));
            stack.Inventory.TryRemove("milk", 1);
            stack.Inventory.TryAdd("bread", 1);
            stack.Refresh();
            Assert.That(Trays(), Is.Empty);
            Assert.That(Goods().Single().name, Is.EqualTo("bread"));
            stack.Inventory.TryRemove("bread", 1);
            stack.Inventory.TryAdd("egg", 13);
            stack.Refresh();
            int created = stack.Pool.Created;
            var beforeDisable = Goods();
            stack.gameObject.SetActive(false);
            // EditMode không tự chạy callback của MonoBehaviour runtime: gọi đúng hàm cleanup để kiểm tra.
            typeof(InventoryStack).GetMethod("OnDisable",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(stack, null);
            Assert.That(stack.VisibleCount, Is.Zero);
            Assert.That(Trays(true).All(x => !x.gameObject.activeSelf), Is.True);
            Assert.That(stack.Top, Is.Zero);
            Assert.That(stack.RearExtent, Is.Zero);
            stack.gameObject.SetActive(true);
            stack.Refresh();
            Assert.That(stack.VisibleCount, Is.EqualTo(13));
            Assert.That(stack.Pool.Created, Is.EqualTo(created));
            CollectionAssert.AreEquivalent(beforeDisable, Goods());
            CollectionAssert.AreEqual(trays, Trays());
            Assert.That(Trays()[0].GetComponent<MeshFilter>().sharedMesh, Is.SameAs(eggMesh));
        }

        [Test]
        public void ContainersShareMeshAndMaterialAcrossOwners()
        {
            stack.Inventory.TryAdd("sauce", 7);
            stack.Refresh();
            var other = new GameObject("WorkerHand");
            other.transform.SetParent(root.transform, false);
            var worker = other.AddComponent<InventoryStack>();
            worker.HeldLayout = true;
            worker.Inventory = stack.Inventory;
            worker.Pool = stack.Pool;
            worker.Refresh();
            var source = Trays()[0];
            var target = other.GetComponentsInChildren<MeshRenderer>()
                .First(x => x.name.StartsWith("CarryContainer_"));
            Assert.That(target.sharedMaterial, Is.SameAs(source.sharedMaterial));
            Assert.That(target.GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(source.GetComponent<MeshFilter>().sharedMesh));
            Assert.That(worker.VisibleCount, Is.EqualTo(7));
        }

        MeshRenderer[] Trays(bool inactive = false) => stack.GetComponentsInChildren<MeshRenderer>(inactive)
            .Where(x => x.name.StartsWith("CarryContainer_")).ToArray();

        MeshRenderer[] Goods() => stack.GetComponentsInChildren<MeshRenderer>()
            .Where(x => !x.name.StartsWith("CarryContainer_")).ToArray();

        static bool Overlaps(Bounds a, Bounds b) =>
            a.max.x > b.min.x + Tolerance && a.min.x < b.max.x - Tolerance &&
            a.max.y > b.min.y + Tolerance && a.min.y < b.max.y - Tolerance &&
            a.max.z > b.min.z + Tolerance && a.min.z < b.max.z - Tolerance;

        static void AssertClearGeometry(MeshRenderer tray, MeshRenderer[] goods)
        {
            // Kiểm tra tam giác mesh thực để bắt vách xuyên hàng, không lặp lại công thức chia ô.
            var mesh = tray.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var surface = new Bounds(tray.transform.TransformPoint(vertices[triangles[i]]), Vector3.zero);
                surface.Encapsulate(tray.transform.TransformPoint(vertices[triangles[i + 1]]));
                surface.Encapsulate(tray.transform.TransformPoint(vertices[triangles[i + 2]]));
                foreach (var item in goods)
                    Assert.That(Overlaps(item.bounds, surface), Is.False, "Container intersects " + item.name);
            }
        }
    }
}
