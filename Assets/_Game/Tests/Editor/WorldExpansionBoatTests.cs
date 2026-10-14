using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class WorldExpansionBoatTests
    {
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("WorldExpansionBoatTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void YachtHasLowHullColliderAndClearOutsideDoor()
        {
            WorldExpansionBoat boat = WorldExpansionBoat.Build(root.transform, new Vector3(4, 0, 7));
            Assert.That(boat, Is.Not.Null);

            BoxCollider collider = boat.GetComponentInChildren<BoxCollider>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.size.x, Is.GreaterThan(4f));
            Assert.That(collider.size.y, Is.GreaterThan(1f));
            Assert.That(collider.size.z, Is.GreaterThan(8f));
            Assert.That(collider.bounds.Contains(boat.DoorOutside), Is.False);
            Assert.That(boat.DoorsOpen, Is.True);
            Assert.That(boat.DoorInsideLocal.y, Is.GreaterThan(.5f));
        }
    }
}
