using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CharacterWalkingTests
    {
        [TestCase("player", "Walk")]
        [TestCase("player", "CarryWalk")]
        [TestCase("customer", "Walk")]
        [TestCase("customer_beach", "Walk")]
        [TestCase("customer_suit", "Walk")]
        public void WalkingClipActuallyMovesLegBonesOnCurrentPrefab(string key, string state)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Art/Imported/Models/" + key + ".prefab");
            var actor = Object.Instantiate(prefab);
            try
            {
                var animator = actor.GetComponentInChildren<Animator>();
                var clip = animator.runtimeAnimatorController.animationClips.First(x =>
                    x.name.Split('|').Last() == state);
                var legs = animator.GetComponentsInChildren<Transform>().Where(x =>
                    x.name.Contains("Leg") || x.name.Contains("Foot")).ToArray();
                Assert.That(legs.Length, Is.GreaterThanOrEqualTo(2), key + " leg bindings");
                clip.SampleAnimation(animator.gameObject, clip.length * .12f);
                var pose = new Dictionary<Transform, Quaternion>();
                foreach (var bone in legs) pose[bone] = bone.localRotation;
                clip.SampleAnimation(animator.gameObject, clip.length * .62f);
                float motion = legs.Sum(x => Quaternion.Angle(pose[x], x.localRotation));
                Assert.That(motion, Is.GreaterThan(12), key + " visible leg motion");
                Assert.That(clip.isLooping, Is.True, key + " walk loop");
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }
    }
}
