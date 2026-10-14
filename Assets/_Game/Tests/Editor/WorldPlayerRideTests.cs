using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldPlayerRideTests
    {
        GameObject playerObject;
        GameObject carrierObject;

        [TearDown]
        public void TearDown()
        {
            if (playerObject) Object.DestroyImmediate(playerObject);
            if (carrierObject) Object.DestroyImmediate(carrierObject);
        }

        [Test]
        public void ClosedCarrierRejectsBoardingWithoutChangingPlayerState()
        {
            PlayerController player = CreatePlayer();
            TestCarrier carrier = CreateCarrier(false, true);

            bool boarded = WorldPlayerRide.TryBoard(player, carrier, carrier.transform, 5f,
                "ride", out string failure);

            Assert.That(boarded, Is.False);
            Assert.That(failure, Does.Contain("cửa"));
            Assert.That(player.transform.parent, Is.Null);
            Assert.That(player.Controller.enabled, Is.True);
            Assert.That(carrier.Boardings, Is.Zero);
        }

        [Test]
        public void MissingHullRejectsBoardingInsteadOfActivatingAStuckRide()
        {
            PlayerController player = CreatePlayer();
            TestCarrier carrier = CreateCarrier(true, false);

            bool boarded = WorldPlayerRide.TryBoard(player, carrier, null,
                5f, "ride", out string failure);

            Assert.That(boarded, Is.False);
            Assert.That(failure, Does.Contain("sẵn sàng"));
            Assert.That(player.GetComponent<WorldPlayerRide>(), Is.Null);
            Assert.That(player.Controller.enabled, Is.True);
        }

        [Test]
        public void OpenCarrierRecordsBoardingAndAlighting()
        {
            PlayerController player = CreatePlayer();
            TestCarrier carrier = CreateCarrier(true, true);

            Assert.That(WorldPlayerRide.TryBoard(player, carrier, carrier.transform, 5f,
                "ride", out string failure), Is.True, failure);
            Assert.That(player.transform.parent, Is.EqualTo(carrier.transform));
            Assert.That(player.transform.localPosition, Is.EqualTo(carrier.DoorInsideLocal));
            Assert.That(carrier.Boardings, Is.EqualTo(1));

            WorldPlayerRide ride = player.GetComponent<WorldPlayerRide>();
            typeof(WorldPlayerRide).GetMethod("EndRide", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(ride, null);

            Assert.That(carrier.Alightings, Is.EqualTo(1));
            Assert.That(player.transform.parent, Is.Null);
            Assert.That(player.Controller.enabled, Is.True);
            Assert.That(player.CanControl, Is.True);
        }

        [Test]
        public void AlightingUsesCurrentDoorAfterCarrierMoves()
        {
            PlayerController player = CreatePlayer();
            TestCarrier carrier = CreateCarrier(true, true);

            Assert.That(WorldPlayerRide.TryBoard(player, carrier, carrier.transform, 5f,
                "ride", out string failure), Is.True, failure);
            carrierObject.transform.position = new Vector3(8, 0, 0);

            WorldPlayerRide ride = player.GetComponent<WorldPlayerRide>();
            typeof(WorldPlayerRide).GetMethod("EndRide", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(ride, null);

            Assert.That(Vector3.Distance(player.transform.position, carrier.DoorOutside),
                Is.LessThan(.001f));
        }

        PlayerController CreatePlayer()
        {
            playerObject = new GameObject("RidePlayer");
            PlayerController player = playerObject.AddComponent<PlayerController>();
            player.Controller = playerObject.AddComponent<CharacterController>();
            return player;
        }

        TestCarrier CreateCarrier(bool doorsOpen, bool hasHull)
        {
            carrierObject = new GameObject("RideCarrier");
            TestCarrier carrier = carrierObject.AddComponent<TestCarrier>();
            carrier.DoorsOpenValue = doorsOpen;
            carrier.HasHull = hasHull;
            return carrier;
        }

        sealed class TestCarrier : MonoBehaviour, ICityPassengerCarrier
        {
            public bool DoorsOpenValue;
            public bool HasHull;
            public int Boardings;
            public int Alightings;
            public Transform Hull => HasHull ? transform : null;
            public bool DoorsOpen => DoorsOpenValue;
            public Vector3 DoorInsideLocal => new(0, 1.1f, 0);
            public Vector3 DoorOutside => transform.position + Vector3.forward * 2f;
            public void RecordBoarding() => Boardings++;
            public void RecordAlighting() => Alightings++;
        }
    }
}
