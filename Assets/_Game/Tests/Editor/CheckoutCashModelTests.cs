using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Tycoon.Tests
{
    public sealed class CheckoutCashModelTests
    {
        GameObject root;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("CashModelTest");
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void CashStandHasGroundedColliderAtExistingCollectionPoint()
        {
            var stand = CheckoutCashModels.Stand(root.transform);
            var bounds = stand.GetComponent<BoxCollider>().bounds;
            Assert.That(bounds.min.y, Is.EqualTo(0).Within(.0001f));
            Assert.That(bounds.max.y, Is.EqualTo(.7f).Within(.0001f));
            Assert.That(bounds.size.x, Is.EqualTo(1.1f).Within(.0001f));
            Assert.That(bounds.size.z, Is.EqualTo(.7f).Within(.0001f));
            Assert.That(bounds.center.x, Is.EqualTo(2.7f).Within(.0001f));
            Assert.That(bounds.center.z, Is.EqualTo(.6f).Within(.0001f));
        }

        [Test]
        public void TwentyCashBundlesRestOnStandAndShareGeometry()
        {
            var stand = CheckoutCashModels.Stand(root.transform).GetComponent<BoxCollider>().bounds;
            Mesh shared = null;
            float previousTop = stand.max.y;
            for (int index = 0; index < 20; index++)
            {
                var bill = CheckoutCashModels.Bill(root.transform);
                bill.transform.localPosition = CheckoutCashModels.Position(index, bill);
                var mesh = bill.GetComponent<MeshFilter>().sharedMesh;
                shared ??= mesh;
                Assert.That(mesh, Is.SameAs(shared));
                var bounds = bill.GetComponent<Renderer>().bounds;
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(stand.min.x - .0001f));
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(stand.max.x + .0001f));
                Assert.That(bounds.min.z, Is.GreaterThanOrEqualTo(stand.min.z - .0001f));
                Assert.That(bounds.max.z, Is.LessThanOrEqualTo(stand.max.z + .0001f));
                if (index < 2)
                    Assert.That(bounds.min.y, Is.EqualTo(stand.max.y).Within(.0001f));
                if (index > 1)
                    Assert.That(bounds.min.y, Is.GreaterThan(previousTop));
                if (index % 2 == 1)
                    previousTop = bounds.max.y;
            }
        }

        [Test]
        public void CollectionPointLeavesRoomForPlayerCapsule()
        {
            var stand = CheckoutCashModels.Stand(root.transform).GetComponent<BoxCollider>();
            Vector3 point = CheckoutCashModels.CollectionOffset + Vector3.up * .35f;
            Vector3 nearest = stand.ClosestPoint(point);
            Assert.That(Vector3.Distance(point, nearest), Is.GreaterThanOrEqualTo(.4f));
        }

        [Test]
        public void CollectionPointHasConnectedGroundInsteadOfWalkableCrateRoof()
        {
            Art.Box("Floor", Vector3.down * .05f, new Vector3(20, .1f, 20), "#888888", root.transform, true);
            CheckoutCashModels.Stand(root.transform);
            Art.Box("CheckoutCollision", new Vector3(.3f, .55f, 0), new Vector3(2.1f, 1.1f, .9f),
                "#888888", root.transform, true);
            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true;
            surface.voxelSize = .16f;
            surface.BuildNavMesh();
            try
            {
                Assert.That(NavMesh.SamplePosition(CheckoutCashModels.CollectionOffset, out var hit,
                    1, NavMesh.AllAreas), Is.True);
                Assert.That(hit.position.y, Is.EqualTo(0).Within(.1f));
                var path = new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(new Vector3(2.7f, 0, -3), hit.position,
                    NavMesh.AllAreas, path), Is.True);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            }
            finally
            {
                surface.RemoveData();
            }
        }
    }
}
