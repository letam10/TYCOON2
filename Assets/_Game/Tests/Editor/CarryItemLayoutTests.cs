using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class CarryItemLayoutTests
    {
        static IEnumerable<string> Goods()
        {
            foreach (var item in Definitions.Items) yield return item.id;
        }

        [TestCaseSource(nameof(Goods))]
        public void EveryFullHandHasSixModelsPerLayerAndFitsTheCarryEnvelope(string id)
        {
            var root = new GameObject("FullCarryGrid");
            try
            {
                var model = HandItemModels.Build(id, root.transform);
                var profile = CarryItemLayout.For(id);
                var bounds = profile.Bounds(model.GetComponent<MeshFilter>().sharedMesh);
                Assert.That(profile.Columns, Is.EqualTo(3));
                Assert.That(profile.Rows, Is.EqualTo(2));
                Bounds total = new(profile.Position(0, 48, bounds) + bounds.center, bounds.size);
                for (int i = 0; i < 48; i++)
                {
                    var placed = new Bounds(profile.Position(i, 48, bounds) + bounds.center, bounds.size);
                    total.Encapsulate(placed);
                    Assert.That(placed.min.y,
                        Is.EqualTo(i / 6 * (bounds.size.y + profile.Spacing.y)).Within(.0001f));
                    Assert.That(profile.Position(i, 48, bounds).x,
                        Is.EqualTo(profile.Position(i % 6, 48, bounds).x).Within(.0001f));
                }
                Assert.That(total.size.x, Is.LessThan(1.9f));
                Assert.That(total.size.y, Is.LessThan(1.8f));
                Assert.That(total.min.y, Is.EqualTo(0).Within(.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase("carrot")]
        [TestCase("wheat")]
        public void LongProduceLiesFlatAndIsSmallerThanMeat(string id)
        {
            var root = new GameObject("ProfileTest");
            try
            {
                var model = HandItemModels.Build(id, root.transform);
                var mesh = model.GetComponent<MeshFilter>().sharedMesh;
                var profile = CarryItemLayout.For(id);
                var held = profile.Bounds(mesh);
                Assert.That(held.size.y, Is.LessThan(held.size.z * .55f));
                Assert.That(CarryItemLayout.ModelSize(id), Is.LessThan(CarryItemLayout.ModelSize("beef")));
                Assert.That(profile.Rows, Is.EqualTo(2));
                Assert.That(profile.Columns, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StorageProduceLiesFlatAndTouchesSupportAtEveryLayer()
        {
            var root = new GameObject("StorageLayoutTest");
            try
            {
                var stack = root.AddComponent<InventoryStack>();
                stack.Inventory = new Inventory(48);
                stack.Inventory.TryAdd("carrot", 12);
                stack.Pool = new ItemPool(root.transform);
                stack.Columns = 3;
                stack.Rows = 2;
                stack.Refresh();
                Assert.That(stack.HeldLayout, Is.False);
                var models = root.GetComponentsInChildren<MeshRenderer>();
                Assert.That(models.Length, Is.EqualTo(12));
                Assert.That(Quaternion.Angle(models[0].transform.localRotation, Quaternion.Euler(90, 0, 0)),
                    Is.LessThan(.001f));
                Assert.That(models[0].transform.localPosition.x, Is.EqualTo(models[3].transform.localPosition.x));
                Assert.That(models[0].transform.localPosition.z, Is.EqualTo(models[6].transform.localPosition.z));
                Assert.That(models[0].bounds.min.y, Is.EqualTo(0).Within(.0001f));
                Assert.That(models[6].bounds.min.y, Is.EqualTo(models[0].bounds.max.y + .006f).Within(.0001f));
                Assert.That(models[0].bounds.size.y, Is.LessThan(models[0].bounds.size.z * .55f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
