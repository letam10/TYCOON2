using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class ActorAnimationSmoothingTests
    {
        GameObject actor;
        Animator animator;
        ActorAnimationSmoothing smoothing;

        [SetUp]
        public void CreateActor()
        {
            actor = new GameObject("AnimationSmoothingTestActor");
            animator = actor.AddComponent<Animator>();
            smoothing = actor.AddComponent<ActorAnimationSmoothing>();
        }

        [TearDown]
        public void DestroyActor()
        {
            Object.DestroyImmediate(actor);
        }

        [Test]
        public void InitializeDisablesRootMotionAndUsesTransformCulling()
        {
            animator.applyRootMotion = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            smoothing.Initialize(animator, 4f);

            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(animator.cullingMode, Is.EqualTo(AnimatorCullingMode.CullUpdateTransforms));
            Assert.That(smoothing.CurrentSpeed, Is.Zero);
        }

        [Test]
        public void SetMotionUsesPlanarVelocityAndApproachesItSmoothly()
        {
            smoothing.Initialize(animator, 4f);
            Vector3 velocity = new Vector3(3f, 8f, 4f);

            smoothing.SetMotion(velocity, true);
            float firstStep = smoothing.CurrentSpeed;
            for (int index = 0; index < 12; index++) smoothing.SetMotion(velocity, true);

            Assert.That(firstStep, Is.GreaterThan(0f));
            Assert.That(firstStep, Is.LessThan(5f));
            Assert.That(smoothing.CurrentSpeed, Is.GreaterThan(firstStep));
            Assert.That(smoothing.CurrentSpeed, Is.LessThan(5f));
        }

        [Test]
        public void StoppingMotionDampsWithoutGoingNegative()
        {
            smoothing.Initialize(animator, 3f);
            for (int index = 0; index < 12; index++)
                smoothing.SetMotion(Vector3.forward * 3f, true);
            float movingSpeed = smoothing.CurrentSpeed;

            smoothing.SetMotion(Vector3.zero, true);

            Assert.That(smoothing.CurrentSpeed, Is.LessThan(movingSpeed));
            Assert.That(smoothing.CurrentSpeed, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void UngroundedMotionDampsAndDoesNotReenableRootMotion()
        {
            smoothing.Initialize(animator, 3f);
            for (int index = 0; index < 12; index++)
                smoothing.SetMotion(Vector3.right * 3f, true);
            float groundedSpeed = smoothing.CurrentSpeed;
            animator.applyRootMotion = true;

            smoothing.SetMotion(Vector3.right * 3f, false);

            Assert.That(smoothing.CurrentSpeed, Is.LessThan(groundedSpeed));
            Assert.That(animator.applyRootMotion, Is.False);
        }

        [Test]
        public void RepeatingOrClearingAnUnknownActionDoesNotThrow()
        {
            smoothing.Initialize(animator);

            Assert.DoesNotThrow(() => smoothing.SetAction("MissingState"));
            Assert.DoesNotThrow(() => smoothing.SetAction("MissingState"));
            Assert.DoesNotThrow(() => smoothing.SetAction(null));
        }
    }
}