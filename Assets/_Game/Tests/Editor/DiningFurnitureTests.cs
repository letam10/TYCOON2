using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class DiningFurnitureTests
    {
        GameObject root;
        GameCatalog previous;
        static System.Collections.Generic.IEnumerable<string> Dishes => Definitions.KitchenRecipes
            .Select(x => Definitions.Recipe(x).output);

        [SetUp]
        public void SetUp()
        {
            previous = Art.Catalog;
            Art.Catalog = Resources.Load<GameCatalog>("GameCatalog");
            root = new GameObject("DiningFurnitureTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Art.Catalog = previous;
        }

        [TestCase(DiningTableShape.Round, "Chair")]
        [TestCase(DiningTableShape.Square, "Bench")]
        public void EachTableHasTwoNearFacingSeatsAndGroundedSolidFurniture(DiningTableShape shape, string name)
        {
            var furniture = DiningFurnitureBuilder.Build(root.transform, shape);
            Assert.That(furniture.Seats, Has.Length.EqualTo(2));
            var table = furniture.transform.Find("Table");
            var tableBounds = TownFurnitureCollision.BoundsIn(table, furniture.transform);
            Assert.That(tableBounds.max.y, Is.EqualTo(furniture.TableTop).Within(.0002f));
            foreach (var seat in furniture.Seats)
            {
                Assert.That(seat.parent.name, Is.EqualTo(name));
                Assert.That(seat.localPosition.y, Is.EqualTo(DiningFurnitureBuilder.CushionTop));
                var direction = furniture.transform.position - seat.position;
                direction.y = 0;
                Assert.That(Vector3.Dot(seat.forward, direction.normalized), Is.GreaterThan(.999f));
                var bounds = TownFurnitureCollision.BoundsIn(seat.parent, furniture.transform);
                float near = Mathf.Min(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x));
                Assert.That(near - tableBounds.extents.x, Is.InRange(.17f, .19f));
                Assert.That(bounds.min.y, Is.EqualTo(0).Within(.0002f));
                if (shape == DiningTableShape.Round) Assert.That(bounds.size.z, Is.LessThan(.9f));
                else Assert.That(bounds.size.z, Is.GreaterThan(1.7f));
            }
            Physics.SyncTransforms();
            Assert.That(furniture.GetComponentsInChildren<BoxCollider>(), Has.Length.EqualTo(3));
            foreach (var collider in furniture.GetComponentsInChildren<BoxCollider>())
                Assert.That(collider.bounds.min.y, Is.EqualTo(DiningFurnitureBuilder.Ground).Within(.0002f));
            var point = furniture.Approach.position + Vector3.up * .35f;
            Assert.That(Physics.CheckCapsule(point, point + Vector3.up * 1.2f, .3f), Is.False);
            Assert.That(furniture.GetComponentsInChildren<MeshRenderer>(), Has.Length.LessThanOrEqualTo(8));
        }

        [Test]
        public void CounterHasThreeHigherStoolsCloseToTheCounterAndFacingIt()
        {
            DiningFurnitureBuilder.Counter(root.transform);
            var furniture = root.GetComponentInChildren<DiningFurniture>();
            Assert.That(furniture.Shape, Is.EqualTo(DiningTableShape.Counter));
            Assert.That(furniture.Seats, Has.Length.EqualTo(3));
            foreach (var seat in furniture.Seats)
            {
                Assert.That(seat.localPosition.y, Is.EqualTo(.82f));
                Assert.That(Vector3.Dot(seat.forward, Vector3.right), Is.GreaterThan(.999f));
                var bounds = TownFurnitureCollision.BoundsIn(seat.parent, furniture.transform);
                Assert.That(-.5f - bounds.max.x, Is.EqualTo(.18f).Within(.001f));
                Assert.That(bounds.min.y, Is.EqualTo(0).Within(.0002f));
            }
            Assert.That(furniture.TableTop - furniture.Seats[0].localPosition.y, Is.InRange(.20f, .30f));
            Assert.That(furniture.GetComponentsInChildren<MeshRenderer>(), Has.Length.LessThanOrEqualTo(11));
        }

        [TestCaseSource(nameof(Dishes))]
        public void DishesAndPlatesRestOnTheCurrentTableTop(string item)
        {
            var table = root.AddComponent<TableStation>();
            table.Id = "table_0";
            table.Inventory = new Inventory(1);
            WorldDressing.Table(table);
            var view = root.GetComponentInChildren<TableMealView>();
            var dish = view.BuildMeal(item);
            foreach (var model in new[] { dish, view.Dirty, view.Empty })
            {
                var bounds = TownFurnitureCollision.BoundsIn(model.transform, view.transform);
                Assert.That(bounds.min.y, Is.EqualTo(view.MealPoint.y + .003f).Within(.0002f));
            }
            Assert.That(table.Seat, Is.EqualTo(root.GetComponentInChildren<DiningFurniture>().Approach.position));
        }

        [Test]
        public void OwnedBatchesHaveValidGeometryAndAreReleasedWithTheFurniture()
        {
            DiningFurnitureBuilder.Build(root.transform, DiningTableShape.Round);
            var meshes = root.GetComponentsInChildren<DiningMeshLifetime>().SelectMany(x => x.Meshes).ToArray();
            foreach (var mesh in meshes)
            {
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                Assert.That(mesh.vertices.All(x => float.IsFinite(x.sqrMagnitude)), Is.True);
                Assert.That(mesh.normals.All(x => float.IsFinite(x.sqrMagnitude) && x.sqrMagnitude > .5f), Is.True);
            }
            Object.DestroyImmediate(root);
            root = null;
            Assert.That(meshes.All(x => !x), Is.True);
        }
    }
}
