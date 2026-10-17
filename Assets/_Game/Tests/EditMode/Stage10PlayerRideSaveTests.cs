using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Stage10PlayerRideSaveTests
    {
        GameObject root;
        GameObject playerObject;
        GameObject carrierObject;
        GameObject originalParentObject;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Stage10RideWorld");
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject) Object.DestroyImmediate(playerObject);
            if (carrierObject) Object.DestroyImmediate(carrierObject);
            if (originalParentObject) Object.DestroyImmediate(originalParentObject);
            if (root) Object.DestroyImmediate(root);
        }

        [Test]
        public void ActiveRideCaptureStoresStableInteractionAndRemainingDuration()
        {
            PlayerController player = CreatePlayer("Stage10Player");
            TestCarrier carrier = CreateCarrier("Stage10Carrier", "stage10.train.alpha");

            Assert.That(WorldPlayerRide.TryBoard(
                player, carrier, carrier.transform, 20f, "ride", carrier.StableKey, out string boardFailure),
                Is.True, boardFailure);
            Assert.That(WorldPlayerRide.TryCapture(player, out PlayerRideSave save), Is.True);

            Assert.That(save, Is.Not.Null);
            Assert.That(save.version, Is.EqualTo(1));
            Assert.That(save.active, Is.True);
            Assert.That(save.phase, Is.EqualTo(1));
            Assert.That(save.interactionId, Is.EqualTo(carrier.StableKey));
            Assert.That(save.remainingSeconds, Is.GreaterThan(0f));
            Assert.That(float.IsNaN(save.remainingSeconds), Is.False);
            Assert.That(float.IsInfinity(save.remainingSeconds), Is.False);
        }

        [Test]
        public void RestoreReattachesWithoutCountingASecondBoarding()
        {
            PlayerController player = CreatePlayer("Stage10RestorePlayer");
            TestCarrier carrier = CreateCarrier("Stage10RestoreCarrier", "stage10.train.restore");

            Assert.That(WorldPlayerRide.TryBoard(
                player, carrier, carrier.transform, 20f, "ride", carrier.StableKey, out string boardFailure),
                Is.True, boardFailure);
            Assert.That(WorldPlayerRide.TryCapture(player, out PlayerRideSave save), Is.True);
            Assert.That(carrier.Boardings, Is.EqualTo(1));

            WorldPlayerRide.CancelForLoad(player);
            Assert.That(carrier.Alightings, Is.EqualTo(1));
            carrier.Boardings = 0;
            carrier.Alightings = 0;
            WorldPlayerRide oldRide = player.GetComponent<WorldPlayerRide>();
            if (oldRide) Object.DestroyImmediate(oldRide);

            Assert.That(WorldPlayerRide.TryRestore(
                player, save, root.transform, out string restoreFailure),
                Is.True, restoreFailure);

            Assert.That(player.transform.parent, Is.EqualTo(carrier.transform));
            Assert.That(player.Controller.enabled, Is.False);
            Assert.That(carrier.Boardings, Is.EqualTo(1));
            Assert.That(WorldPlayerRide.TryCapture(player, out PlayerRideSave restored), Is.True);
            Assert.That(restored.interactionId, Is.EqualTo(carrier.StableKey));
        }

        [Test]
        public void InvalidCarrierReturnsFailureAndLeavesPlayerAtFiniteSafeExit()
        {
            PlayerController player = CreatePlayer("Stage10InvalidCarrierPlayer");
            Vector3 savedExit = new Vector3(13f, .05f, -8f);
            PlayerRideSave save = new PlayerRideSave
            {
                version = 1,
                active = true,
                phase = 1,
                interactionId = "stage10.missing.carrier",
                remainingSeconds = 4f,
                exitX = savedExit.x,
                exitY = savedExit.y,
                exitZ = savedExit.z,
                originalParentWasNull = true
            };

            bool restored = WorldPlayerRide.TryRestore(
                player, save, root.transform, out string failure);

            Assert.That(restored, Is.False);
            Assert.That(failure, Is.Not.Null.And.Not.Empty);
            Assert.That(player.transform.parent, Is.Null);
            Assert.That(player.Controller.enabled, Is.True);
            Assert.That(float.IsNaN(player.transform.position.x), Is.False);
            Assert.That(float.IsNaN(player.transform.position.y), Is.False);
            Assert.That(float.IsNaN(player.transform.position.z), Is.False);
            Assert.That(float.IsInfinity(player.transform.position.x), Is.False);
            Assert.That(float.IsInfinity(player.transform.position.y), Is.False);
            Assert.That(float.IsInfinity(player.transform.position.z), Is.False);
            Assert.That(WorldPlayerRide.TryCapture(player, out _), Is.False);
        }

        [Test]
        public void LegacySaveWithoutPlayerRideRemainsReadable()
        {
            string path = Path.Combine(
                Path.GetTempPath(), "tycoon2-stage10-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                SaveStore.Write(path, new SaveData { version = 2, money = 0 });
                SaveData loaded = SaveStore.Read(path);

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.playerRide == null || !loaded.playerRide.active, Is.True);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
    public void InactivePlayerRidePayloadRemainsOptional()
        {
            string path = Path.Combine(
                Path.GetTempPath(), "tycoon2-stage10-inactive-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                SaveData data = new SaveData
                {
                    version = 3,
                    money = 0,
                    playerRide = new PlayerRideSave
                    {
                        version = 1,
                        active = false
                    }
                };

                Assert.DoesNotThrow(() => SaveStore.Write(path, data));
                Assert.That(SaveStore.Read(path).playerRide.active, Is.False);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void RestorePreservesOriginalParentSemantics()
        {
            originalParentObject = new GameObject("Stage10OriginalParent");
            originalParentObject.transform.SetParent(root.transform, false);
            WorldInteractionRegistry.Build(root.transform);
            WorldInteractionArea.Add(
                originalParentObject.transform,
                "stage10.parent",
                "Parent",
                "Parent",
                WorldInteractionAction.Visit,
                Vector3.zero,
                1f);
            PlayerController player = CreatePlayer("Stage10ParentedPlayer");
            player.transform.SetParent(originalParentObject.transform, false);
            TestCarrier carrier = CreateCarrier("Stage10ParentedCarrier", "stage10.train.parented");

            Assert.That(WorldPlayerRide.TryBoard(
                player, carrier, carrier.transform, 20f, "ride", carrier.StableKey, out string boardFailure),
                Is.True, boardFailure);
            Assert.That(WorldPlayerRide.TryCapture(player, out PlayerRideSave save), Is.True);
            Assert.That(save.originalParentWasNull, Is.False);
            Assert.That(save.originalParentId, Is.EqualTo("stage10.parent"));

            WorldPlayerRide.CancelForLoad(player);
            Assert.That(player.transform.parent, Is.EqualTo(originalParentObject.transform));
            Assert.That(WorldPlayerRide.TryRestore(
                player, save, root.transform, out string restoreFailure),
                Is.True, restoreFailure);
            Assert.That(player.transform.parent, Is.EqualTo(carrier.transform));

            WorldPlayerRide.CancelForLoad(player);
            Assert.That(player.transform.parent, Is.EqualTo(originalParentObject.transform));
        }

        PlayerController CreatePlayer(string name)
        {
            playerObject = new GameObject(name);
            PlayerController player = playerObject.AddComponent<PlayerController>();
            player.Controller = playerObject.AddComponent<CharacterController>();
            return player;
        }

        TestCarrier CreateCarrier(string name, string stableKey)
        {
            carrierObject = new GameObject(name);
            carrierObject.transform.SetParent(root.transform, false);
            TestCarrier carrier = carrierObject.AddComponent<TestCarrier>();
            carrier.StableKeyValue = stableKey;
            WorldInteractionRegistry.Build(root.transform);
            WorldInteractionArea area = WorldInteractionArea.Add(
                carrierObject.transform,
                stableKey,
                "Carrier",
                "Carrier",
                WorldInteractionAction.BoardTrain,
                Vector3.zero,
                2f);
            area.CarrierSource = carrier;
            return carrier;
        }

        sealed class TestCarrier : MonoBehaviour, ICityPassengerCarrier
        {
            public string StableKeyValue;
            public int Boardings;
            public int Alightings;

            public string StableKey => StableKeyValue;
            public Transform Hull => transform;
            public bool DoorsOpen => true;
            public Vector3 DoorInsideLocal => new Vector3(0f, 1.1f, 0f);
            public Vector3 DoorOutside => transform.TransformPoint(Vector3.forward * 2f);

            public void RecordBoarding()
            {
                Boardings++;
            }

            public void RecordAlighting()
            {
                Alightings++;
            }
        }
    }
}
