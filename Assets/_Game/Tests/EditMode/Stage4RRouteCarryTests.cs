using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Stage4RRouteCarryTests
    {
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Stage4RRouteCarryTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlannedRouteKeepsExactFiniteEndpointsAndCapsuleClearance()
        {
            Vector3 from = new(141, 0, -40.1f);
            Vector3 destination = new(-65, 0, 60);

            bool planned = CityPedestrianNetwork.TryPlan(
                from,
                destination,
                out List<Vector3> route,
                out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Count, Is.GreaterThan(1));
            Assert.That(route[0], Is.EqualTo(from));
            Assert.That(route[^1], Is.EqualTo(destination));

            for (int index = 0; index < route.Count; index++)
            {
                Vector3 point = route[index];
                Assert.That(float.IsNaN(point.x) || float.IsInfinity(point.x), Is.False);
                Assert.That(float.IsNaN(point.y) || float.IsInfinity(point.y), Is.False);
                Assert.That(float.IsNaN(point.z) || float.IsInfinity(point.z), Is.False);
                if (index > 0)
                    Assert.That(Vector3.Distance(route[index - 1], point), Is.GreaterThan(.01f));
            }

            Assert.That(
                CityPedestrianNetwork.ValidateRoute(route, .38f, out failure),
                Is.True,
                failure);
        }

        [Test]
        public void CapsuleClearanceRejectsReservedRailCrossing()
        {
            Assert.That(
                WorldRouteCatalog.TryGet("rail-mainline", out WorldRouteCorridor rail),
                Is.True);

            Vector3 midpoint = Vector3.Lerp(rail.Nodes[0], rail.Nodes[1], .5f);
            Assert.That(
                WorldRouteCatalog.IsReserved(midpoint, .38f, out string corridorId),
                Is.True);
            Assert.That(corridorId, Is.EqualTo("rail-mainline"));

            var route = new[]
            {
                new Vector3(-120, 0, -64),
                new Vector3(120, 0, -64)
            };

            bool valid = CityPedestrianNetwork.ValidateRoute(
                route,
                .38f,
                out string failure);

            Assert.That(valid, Is.False);
            Assert.That(failure, Does.Contain("reserved corridor"));
        }

        [Test]
        public void RefreshKeepsCarryItemIdsCountAndInventoryRevisionConsistent()
        {
            var inventory = new Inventory(12, true);
            Assert.That(inventory.TryAdd("carrot", 4), Is.True);
            int revision = inventory.Revision;

            var stack = root.AddComponent<InventoryStack>();
            stack.Inventory = inventory;
            stack.Pool = new ItemPool(root.transform);
            stack.HeldLayout = true;
            stack.Columns = 2;
            stack.Rows = 3;
            stack.Refresh();

            Assert.That(stack.VisibleCount, Is.EqualTo(inventory.Total));
            Assert.That(inventory.Revision, Is.EqualTo(revision));
            AssertVisibleIds(stack, "carrot", 4);

            int created = stack.Pool.Created;
            stack.Refresh();

            Assert.That(stack.VisibleCount, Is.EqualTo(4));
            Assert.That(stack.Pool.Created, Is.EqualTo(created));
            Assert.That(inventory.Revision, Is.EqualTo(revision));
            AssertVisibleIds(stack, "carrot", 4);

            Assert.That(inventory.TryRemove("carrot", 1), Is.True);
            int changedRevision = inventory.Revision;
            stack.Refresh();

            Assert.That(changedRevision, Is.GreaterThan(revision));
            Assert.That(stack.VisibleCount, Is.EqualTo(inventory.Total));
            Assert.That(inventory.Revision, Is.EqualTo(changedRevision));
            AssertVisibleIds(stack, "carrot", 3);
        }

        [Test]
        public void CarryAnchorIsAttachedToActorInsideHandEnvelope()
        {
            var actor = new GameObject("Actor");
            actor.transform.SetParent(root.transform, false);

            var anchor = new GameObject("FrontHandCarry");
            anchor.transform.SetParent(actor.transform, false);
            anchor.transform.localPosition = new Vector3(0, .86f, .78f);

            var sway = anchor.AddComponent<CarrySway>();
            sway.Actor = actor.transform;

            Assert.That(anchor.transform.parent, Is.SameAs(actor.transform));
            Assert.That(anchor.transform.localPosition.x, Is.EqualTo(0).Within(.001f));
            Assert.That(anchor.transform.localPosition.y, Is.InRange(.5f, 1.2f));
            Assert.That(anchor.transform.localPosition.z, Is.InRange(.5f, 1.4f));
            Assert.That(anchor.transform.localPosition.magnitude, Is.LessThan(1.5f));

            MethodInfo lateUpdate = typeof(CarrySway).GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lateUpdate, Is.Not.Null);
            lateUpdate.Invoke(sway, null);

            actor.transform.rotation = Quaternion.Euler(0, 180, 0);
            lateUpdate.Invoke(sway, null);

            Vector3 expected = actor.transform.TransformPoint(new Vector3(0, .86f, .78f));
            Assert.That(Vector3.Distance(anchor.transform.position, expected), Is.LessThanOrEqualTo(.121f));
        }

        static void AssertVisibleIds(InventoryStack stack, string expectedId, int count)
        {
            for (int index = 0; index < count; index++)
                Assert.That(stack.VisibleItemId(index), Is.EqualTo(expectedId));
        }
    }
}
