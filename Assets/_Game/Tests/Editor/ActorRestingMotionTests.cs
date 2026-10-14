using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon.Tests
{
    public sealed class ActorRestingMotionTests
    {
        GameObject actor;
        GameObject floor;
        ActorPhysicalMotion motion;

        [SetUp]
        public void CreateGroundedActor()
        {
            actor = new GameObject("RestingNpc");
            floor = new GameObject("Ground", typeof(BoxCollider));
            floor.transform.position = new Vector3(0, -.1f, 0);
            floor.GetComponent<BoxCollider>().size = new Vector3(10, .2f, 10);
            motion = ActorPhysicalMotion.Attach(actor.AddComponent<NavMeshAgent>());
            Physics.SyncTransforms();
            motion.ApplyStep(Vector3.down * .2f, .1f);
            Assert.That(motion.Body.isGrounded, Is.True);
        }

        [TearDown]
        public void DestroyScene()
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(floor);
        }

        [Test]
        public void RestingActorKeepsCollisionAndBeginsWalkingIntoWallImmediately()
        {
            var wall = new GameObject("Wall", typeof(BoxCollider));
            try
            {
                wall.transform.position = new Vector3(1, .9f, 0);
                wall.GetComponent<BoxCollider>().size = new Vector3(.2f, 2, 5);
                Physics.SyncTransforms();
                for (int i = 0; i < 5; i++)
                    motion.ApplyStep(Vector3.down * .012f, .006f);
                Assert.That(motion.Body.enabled, Is.True);
                Assert.That(motion.Speed, Is.Zero);
                motion.ApplyStep(new Vector3(4, -.012f, 0), .006f);
                Assert.That(actor.transform.position.x, Is.InRange(.2f, .7f));
                Assert.That(motion.Speed, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void RemovedSupportIsDetectedWithinEightyMilliseconds()
        {
            float initialHeight = actor.transform.position.y;
            floor.GetComponent<BoxCollider>().enabled = false;
            Physics.SyncTransforms();
            for (int i = 0; i < 14; i++)
                motion.ApplyStep(Vector3.down * .012f, .006f);
            Assert.That(motion.Body.isGrounded, Is.False);
            Assert.That(actor.transform.position.y, Is.LessThan(initialHeight));
        }

        [Test]
        public void ExternalRepositionAndUpwardMovementAreNeverSkipped()
        {
            actor.transform.position += Vector3.up;
            Physics.SyncTransforms();
            float raised = actor.transform.position.y;
            motion.ApplyStep(Vector3.down * .012f, .006f);
            Assert.That(actor.transform.position.y, Is.LessThan(raised));
            float lowered = actor.transform.position.y;
            motion.ApplyStep(Vector3.up * .2f, .006f);
            Assert.That(actor.transform.position.y, Is.GreaterThan(lowered));
        }
    }
}
