using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class UpgradeVisualMotionTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject root;
        UpgradeVisualMotion motion;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("UpgradeVisualMotionTests");
            root.SetActive(false);
            motion = root.AddComponent<UpgradeVisualMotion>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ConstructionKeepsStaggerAndFinalPoseThenStopsWritingStaticParts()
        {
            var first = AddPiece("Support", new Vector3(1, 2, 3));
            var last = AddPiece("Panel", new Vector3(4, 5, 6));
            var firstPosition = first.localPosition;
            var lastPosition = last.localPosition;
            var scale = new Vector3(2, 3, 4);
            first.localScale = scale;
            last.localScale = scale;
            motion.Initialize(null, false, 0);
            BeginConstruction();

            PoseAt(0);
            AssertVector(first.localPosition, firstPosition + Vector3.up * .22f);
            AssertVector(last.localPosition, lastPosition + Vector3.up * .22f);
            AssertVector(first.localScale, scale * .72f);
            AssertVector(last.localScale, scale * .72f);

            PoseAt(.52f);
            AssertVector(first.localPosition, firstPosition);
            AssertVector(first.localScale, scale);
            Assert.That(last.localPosition.y, Is.GreaterThan(lastPosition.y));
            first.hasChanged = false;

            PoseAt(.75f);
            Assert.That(first.hasChanged, Is.False, "The first settled piece must stop receiving writes.");
            AssertVector(last.localPosition, lastPosition);
            AssertVector(last.localScale, scale);
            last.hasChanged = false;

            PoseAt(1);
            Assert.That(first.hasChanged, Is.False);
            Assert.That(last.hasChanged, Is.False);
        }

        [TestCase("MotorRotor")]
        [TestCase("SprinklerHead")]
        public void SettledRotorKeepsTurningWithoutRewritingPositionOrScale(string rotorName)
        {
            var support = AddPiece("Support", Vector3.up);
            var rotor = AddPiece(rotorName, Vector3.right);
            var rotation = Quaternion.Euler(12, 25, 38);
            rotor.localRotation = rotation;
            motion.Initialize(null, false, 2);
            support.hasChanged = false;
            var position = new Vector3(7, 8, 9);
            var scale = new Vector3(3, 4, 5);
            rotor.localPosition = position;
            rotor.localScale = scale;

            SetField("phase", 60f);
            PoseAt(1);

            Assert.That(motion.enabled, Is.True);
            Assert.That(support.hasChanged, Is.False);
            AssertVector(rotor.localPosition, position);
            AssertVector(rotor.localScale, scale);
            Assert.That(Quaternion.Angle(rotor.localRotation,
                rotation * Quaternion.AngleAxis(60, Vector3.up)), Is.LessThan(.001f));

            SetField("phase", 120f);
            PoseAt(1);
            Assert.That(Quaternion.Angle(rotor.localRotation,
                rotation * Quaternion.AngleAxis(120, Vector3.up)), Is.LessThan(.001f));
            Assert.That(support.hasChanged, Is.False);
        }

        [Test]
        public void ReinitializeAfterRemovingRotorDisablesMotionAndCapturesNewPieces()
        {
            var rotor = AddPiece("MotorRotor", Vector3.up);
            motion.Initialize(null, false, 3);
            Assert.That(motion.enabled, Is.True);
            Object.DestroyImmediate(rotor.gameObject);
            var panel = AddPiece("Panel", new Vector3(3, 2, 1));
            var position = panel.localPosition;
            panel.localScale = Vector3.one * 2;

            motion.Initialize(null, false, 0);

            Assert.That(motion.enabled, Is.False);
            AssertVector(panel.localPosition, position);
            AssertVector(panel.localScale, Vector3.one * 2);
            panel.hasChanged = false;
            PoseAt(1);
            Assert.That(panel.hasChanged, Is.False);

            AddPiece("SprinklerHead", Vector3.zero);
            motion.Initialize(null, false, 1);
            Assert.That(motion.enabled, Is.True);
        }

        [Test]
        public void PausedSessionDoesNotAdvanceConstructionOrRotor()
        {
            var game = root.AddComponent<GameSession>();
            typeof(GameSession).GetProperty("IsRestoring").SetValue(game, true);
            var rotor = AddPiece("MotorRotor", Vector3.up);
            motion.Initialize(game, false, 3);
            BeginConstruction();
            PoseAt(.2f);
            SetField("phase", 30f);
            rotor.hasChanged = false;

            Invoke("Update");

            Assert.That(rotor.hasChanged, Is.False);
            Assert.That(GetField("elapsed"), Is.EqualTo(.2f));
            Assert.That(GetField("phase"), Is.EqualTo(30f));
        }

        Transform AddPiece(string name, Vector3 position)
        {
            var piece = new GameObject(name).transform;
            piece.SetParent(root.transform, false);
            piece.localPosition = position;
            return piece;
        }

        void BeginConstruction()
        {
            // EditMode khởi tạo ngay tư thế cuối; mở lại trạng thái dựng để kiểm tra các mốc thời gian.
            var settled = (bool[])GetField("settled");
            Array.Clear(settled, 0, settled.Length);
            SetField("elapsed", 0f);
        }

        void PoseAt(float elapsed)
        {
            SetField("elapsed", elapsed);
            Invoke("ApplyPose");
        }

        void Invoke(string method)
        {
            typeof(UpgradeVisualMotion).GetMethod(method, PrivateInstance).Invoke(motion, null);
        }

        object GetField(string name)
        {
            return typeof(UpgradeVisualMotion).GetField(name, PrivateInstance).GetValue(motion);
        }

        void SetField(string name, object value)
        {
            typeof(UpgradeVisualMotion).GetField(name, PrivateInstance).SetValue(motion, value);
        }

        static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(.00001f));
        }
    }
}
