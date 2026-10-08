using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon.Tests
{
    public sealed class ActorPhysicalMotionTests
    {
        [Test]
        public void NpcCannotPassThroughSolidWallAndReportsItsActualSpeed()
        {
            var actor = new GameObject("NpcCollisionTest");
            var wall = new GameObject("SolidProp", typeof(BoxCollider));
            try
            {
                wall.transform.position = new Vector3(1, .9f, 0);
                wall.GetComponent<BoxCollider>().size = new Vector3(.2f, 2, 5);
                var motion = ActorPhysicalMotion.Attach(actor.AddComponent<NavMeshAgent>());
                Physics.SyncTransforms();
                motion.ApplyStep(Vector3.right * 4, .1f);
                Assert.That(actor.transform.position.x, Is.LessThan(.7f));
                motion.ApplyStep(Vector3.right, .1f);
                Assert.That(motion.Speed, Is.LessThan(.1f));
                Assert.That(motion.DistanceMoved, Is.GreaterThan(.2f));
                Assert.That(motion.Agent.updatePosition, Is.False);
                Assert.That(motion.Agent.updateRotation, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void SeatedDriverSuspendsBothNavigationAndCollision()
        {
            var actor = new GameObject("DriverCollisionTest");
            try
            {
                var agent = Navigation.Agent(actor);
                var motion = actor.GetComponent<ActorPhysicalMotion>();
                Navigation.Suspend(agent);
                Assert.That(agent.enabled, Is.False);
                Assert.That(motion.Body.enabled, Is.False);
                Assert.That(Navigation.ActualSpeed(agent), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }

        [Test]
        public void CollisionConfigurationLeavesSidewalkStepAndDoesNotDoubleAttach()
        {
            var actor = new GameObject("ActorBodyTest");
            try
            {
                var agent = Navigation.Agent(actor);
                var first = actor.GetComponent<ActorPhysicalMotion>();
                Assert.That(ActorPhysicalMotion.Attach(agent), Is.SameAs(first));
                Assert.That(actor.GetComponents<CharacterController>().Length, Is.EqualTo(1));
                Assert.That(first.Body.stepOffset, Is.InRange(.12f, .2f));
                Assert.That(first.Body.radius, Is.LessThan(agent.radius));
                Assert.That(first.Body.skinWidth, Is.GreaterThanOrEqualTo(first.Body.radius * .1f));
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }
    }
}
