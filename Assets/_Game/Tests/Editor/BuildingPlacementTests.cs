using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class BuildingPlacementTests
    {
        GameObject root;
        GameCatalog previous;

        [SetUp]
        public void SetUp()
        {
            previous = Art.Catalog;
            Art.Catalog = Resources.Load<GameCatalog>("GameCatalog");
            root = new GameObject("BuildingPlacementTest");
            root.transform.position = new Vector3(1000, 0, 1000);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Art.Catalog = previous;
        }

        [Test]
        public void BusinessWallsAreGroundedContinuousAndMatchVisibleBounds()
        {
            Dress("04 SIÊU THỊ");
            var walls = root.GetComponentsInChildren<BoxCollider>()
                .Where(x => x.name is "BrickPlinth" or "RearPlaster" or "SidePlinth" or "SidePlaster")
                .ToArray();
            Assert.That(walls, Has.Length.EqualTo(8));
            foreach (var wall in walls)
            {
                var mesh = wall.GetComponent<MeshFilter>().sharedMesh.bounds;
                Assert.That((mesh.size - wall.size).magnitude, Is.LessThan(.0001f), wall.name);
                Assert.That((mesh.center - wall.center).magnitude, Is.LessThan(.0001f), wall.name);
                bool plinth = wall.name.Contains("Plinth");
                Assert.That(wall.bounds.min.y, Is.EqualTo(plinth ? .06f : .7f).Within(.0001f));
                Assert.That(wall.bounds.max.y, Is.EqualTo(plinth ? .7f : 3).Within(.0001f));
                Assert.That(Physics.CheckCapsule(wall.bounds.center + Vector3.down * .2f,
                    wall.bounds.center + Vector3.up * .2f, .3f), Is.True);
            }
        }

        [TestCase("04 SIÊU THỊ")]
        [TestCase("05 TIỆM BÁNH")]
        [TestCase("06 NHÀ HÀNG")]
        public void BusinessDoorwaysRetainFourMetresOfCapsuleClearance(string label)
        {
            Dress(label);
            foreach (float offset in new[] { -1.6f, 0, 1.6f })
            {
                ClearCapsule(new Vector3(offset, .36f, 10));
                ClearCapsule(new Vector3(11, .36f, offset));
            }
            var walls = root.GetComponentsInChildren<BoxCollider>()
                .Where(x => x.name is "RearPlaster" or "SidePlaster");
            foreach (var wall in walls)
            {
                var local = wall.transform.localPosition;
                float inner = wall.name == "RearPlaster"
                    ? Mathf.Abs(local.x) - wall.size.x * .5f
                    : Mathf.Abs(local.z) - wall.size.z * .5f;
                Assert.That(inner, Is.EqualTo(2).Within(.0001f));
            }
            if (label.Contains("SIÊU THỊ"))
                for (float z = 7; z <= 10; z += .5f) ClearCapsule(new Vector3(0, .36f, z));
        }

        [Test]
        public void FurnitureCollidersFollowIndividualPartsAndStayInsideBusinessFloor()
        {
            Dress("06 NHÀ HÀNG");
            var fixtures = root.transform.Find("DistrictFixtures");
            foreach (var collider in fixtures.GetComponentsInChildren<BoxCollider>())
            {
                var mesh = collider.GetComponent<MeshFilter>().sharedMesh.bounds;
                Assert.That((mesh.size - collider.size).magnitude, Is.LessThan(.0001f), collider.name);
                var local = collider.bounds.center - root.transform.position;
                Assert.That(Mathf.Abs(local.x) + collider.bounds.extents.x, Is.LessThan(11));
                Assert.That(Mathf.Abs(local.z) + collider.bounds.extents.z, Is.LessThan(10));
            }
            var feet = fixtures.GetComponentsInChildren<BoxCollider>()
                .Where(x => x.name is "CabinetBody" or "BenchLeg");
            foreach (var foot in feet) Assert.That(foot.bounds.min.y, Is.EqualTo(.06f).Within(.0001f));
            var planter = root.GetComponentsInChildren<BoxCollider>().Single(x => x.name == "PlanterStone");
            Assert.That(planter.bounds.min.y, Is.EqualTo(.06f).Within(.0001f));
        }

        [Test]
        public void ImportedBoundsUseLocalGeometryDespiteNestedRotationAndScale()
        {
            var model = TownModelParts.Root("RotatedFurniture", new Vector3(2, 3, 4), root.transform);
            model.localRotation = Quaternion.Euler(0, 37, 0);
            model.localScale = new Vector3(1.7f, .8f, 1.2f);
            var mesh = TownModelParts.Box("Body", new Vector3(.2f, -.4f, .1f), new Vector3(2, 1.6f, .8f),
                "wood", "#9C8463", model);
            TownFurnitureCollision.Ground(model, .06f);
            var collider = TownFurnitureCollision.Fit(model);
            Physics.SyncTransforms();
            Assert.That((collider.center - mesh.transform.localPosition).magnitude, Is.LessThan(.0001f));
            Assert.That((collider.size - new Vector3(2, 1.6f, .8f)).magnitude, Is.LessThan(.0001f));
            Assert.That(mesh.GetComponent<Renderer>().bounds.min.y, Is.EqualTo(.06f).Within(.0001f));
        }

        [TestCase("cold_cabinet")]
        [TestCase("kitchen_sink")]
        [TestCase("prep_table")]
        [TestCase("dining_chair")]
        public void ImportedFurnitureStandsOnFloorWithMatchingCollision(string key)
        {
            var model = TownFurnitureCollision.Model(key, Vector3.zero, root.transform, .8f, 27);
            Physics.SyncTransforms();
            var collider = model.GetComponent<BoxCollider>();
            var bounds = TownFurnitureCollision.BoundsIn(model.transform, model.transform);
            Assert.That((bounds.center - collider.center).magnitude, Is.LessThan(.0001f));
            Assert.That((bounds.size - collider.size).magnitude, Is.LessThan(.0001f));
            Assert.That(collider.bounds.min.y, Is.EqualTo(.06f).Within(.0002f));
            Assert.That(model.GetComponentsInChildren<Collider>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void DiningFurnitureLeavesTheExistingCustomerSeatReachable()
        {
            var table = root.AddComponent<TableStation>();
            WorldDressing.Table(table);
            Physics.SyncTransforms();
            ClearCapsule(new Vector3(1.4f, .37f, 0));
            ClearCapsule(new Vector3(1.4f, .37f, -.6f));
            var furniture = root.transform.Find("DiningFurniture");
            Assert.That(furniture.GetComponentsInChildren<BoxCollider>(), Has.Length.EqualTo(2));
            foreach (var collider in furniture.GetComponentsInChildren<BoxCollider>())
                Assert.That(collider.bounds.min.y, Is.EqualTo(.06f).Within(.0001f));
        }

        void Dress(string label)
        {
            Art.Box("BusinessFloor", new Vector3(0, .04f, 0), new Vector3(22, .04f, 20),
                "#C3B4A0", root.transform);
            TownArchitecture.Dress(root.transform, new Vector2(22, 20), label);
            Physics.SyncTransforms();
        }

        void ClearCapsule(Vector3 point)
        {
            point += root.transform.position;
            Assert.That(Physics.CheckCapsule(point, point + Vector3.up * 1.2f, .3f), Is.False,
                "An entry or central queue lane is blocked at " + point);
        }
    }
}
