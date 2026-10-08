using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon.Tests
{
    public sealed class CityObstacleNavigationTests
    {
        [Test]
        public void BakedPathDoesNotAskCapsuleToClimbAcrossIrrigationTrough()
        {
            var root = new GameObject("IrrigationPathTest");
            var actor = new GameObject("PhysicalWalker");
            try
            {
                Box("Ground", new(0, -.1f, 0), new(18, .2f, 14));
                Box("IrrigationTrough", new(0, .14f, 0), new(8, .2f, .55f));
                Box("IrrigationValve", new(3.5f, .7f, .7f), new(.2f, 1.4f, .2f));
                Physics.SyncTransforms();
                var body = actor.AddComponent<CharacterController>();
                ActorPhysicalMotion.Configure(body);
                Assert.That(NavMesh.GetSettingsByID(0).agentClimb, Is.LessThanOrEqualTo(body.stepOffset));
                var surface = root.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.overrideVoxelSize = true;
                surface.voxelSize = .16f;
                surface.BuildNavMesh();
                var path = new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(new(3, 0, 3), new(3, 0, -3),
                    NavMesh.AllAreas, path), Is.True);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                Assert.That(path.corners.Length, Is.GreaterThan(2));
                actor.transform.position = path.corners[0];
                foreach (var corner in path.corners)
                {
                    for (int step = 0; step < 500; step++)
                    {
                        var delta = Vector3.ProjectOnPlane(corner - actor.transform.position, Vector3.up);
                        if (delta.magnitude < .12f) break;
                        body.Move(Vector3.ClampMagnitude(delta, .05f) + Vector3.down * .03f);
                    }
                    Assert.That(Vector3.ProjectOnPlane(corner - actor.transform.position,
                        Vector3.up).magnitude, Is.LessThan(.15f), "Capsule reaches baked corner");
                }
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(root);
            }

            void Box(string name, Vector3 center, Vector3 size)
            {
                var box = new GameObject(name, typeof(BoxCollider));
                box.transform.SetParent(root.transform, false);
                box.transform.localPosition = center;
                box.GetComponent<BoxCollider>().size = size;
            }
        }
    }
}
