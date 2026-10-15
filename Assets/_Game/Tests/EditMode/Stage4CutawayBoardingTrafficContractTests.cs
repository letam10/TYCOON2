using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Stage4CutawayBoardingTrafficContractTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void CutawaySetPlayerInsideIsIdempotentAndRestoresInitialShellState()
        {
            root = new GameObject("Stage4CutawayContract");
            WorldCutawayVolume volume = root.AddComponent<WorldCutawayVolume>();
            volume.enterHysteresisSeconds = 0f;
            volume.exitHysteresisSeconds = 0f;

            GameObject shell = new GameObject("UpperShell");
            shell.transform.SetParent(root.transform, false);
            MeshRenderer renderer = shell.AddComponent<MeshRenderer>();
            renderer.enabled = false;
            shell.SetActive(false);
            volume.upperShellRenderers.Add(renderer);
            volume.upperShellObjects.Add(shell);

            volume.SetPlayerInside(true);
            volume.SetPlayerInside(true);
            Assert.That(volume.IsRevealed, Is.True);
            Assert.That(renderer.enabled, Is.False);
            Assert.That(shell.activeSelf, Is.False);

            volume.SetPlayerInside(false);
            volume.SetPlayerInside(false);
            Assert.That(volume.IsRevealed, Is.False);
            Assert.That(renderer.enabled, Is.False);
            Assert.That(shell.activeSelf, Is.False);
        }

        [Test]
        public void PlayerBoardingRejectsClosedCarrierBeforeChangingPlayerState()
        {
            root = new GameObject("Stage4ClosedCarrier");
            GameObject playerObject = new GameObject("Stage4Player");
            playerObject.transform.SetParent(root.transform, false);
            PlayerController player = playerObject.AddComponent<PlayerController>();
            player.Controller = playerObject.AddComponent<CharacterController>();
            GameObject vehicleObject = new GameObject("Stage4Vehicle");
            vehicleObject.transform.SetParent(root.transform, false);
            FakeCarrier carrier = new FakeCarrier(vehicleObject.transform)
            {
                DoorsOpen = false
            };

            bool boarded = WorldPlayerRide.TryBoard(
                player,
                carrier,
                vehicleObject.transform,
                1f,
                "test",
                out string failure);

            Assert.That(boarded, Is.False);
            Assert.That(failure, Does.Contain("cửa"));
            Assert.That(player.transform.parent, Is.Not.EqualTo(vehicleObject.transform));
            Assert.That(player.Controller.enabled, Is.True);
            Assert.That(player.CanControl, Is.True);
            Assert.That(carrier.Boardings, Is.Zero);
        }

        [Test]
        public void PlayerBoardingRequiresHullAndRecordsBoardingOnce()
        {
            root = new GameObject("Stage4BoardingContract");
            GameObject playerObject = new GameObject("Stage4Player");
            playerObject.transform.SetParent(root.transform, false);
            PlayerController player = playerObject.AddComponent<PlayerController>();
            player.Controller = playerObject.AddComponent<CharacterController>();
            FakeCarrier missingHull = new FakeCarrier(null)
            {
                DoorsOpen = true
            };

            bool missingHullResult = WorldPlayerRide.TryBoard(
                player,
                missingHull,
                root.transform,
                1f,
                "test",
                out string missingHullFailure);

            Assert.That(missingHullResult, Is.False);
            Assert.That(missingHullFailure, Does.Contain("sẵn sàng"));
            Assert.That(missingHull.Boardings, Is.Zero);

            GameObject vehicleObject = new GameObject("Stage4Vehicle");
            vehicleObject.transform.SetParent(root.transform, false);
            FakeCarrier carrier = new FakeCarrier(vehicleObject.transform)
            {
                DoorsOpen = true,
                DoorInsideLocal = new Vector3(0f, 1f, 1f)
            };

            bool boarded = WorldPlayerRide.TryBoard(
                player,
                carrier,
                vehicleObject.transform,
                1f,
                "test",
                out string failure);

            Assert.That(boarded, Is.True, failure);
            Assert.That(carrier.Boardings, Is.EqualTo(1));
            Assert.That(player.transform.parent, Is.EqualTo(vehicleObject.transform));
            Assert.That(player.transform.localPosition, Is.EqualTo(carrier.DoorInsideLocal));
            Assert.That(player.Controller.enabled, Is.False);
            Assert.That(player.CanControl, Is.False);
        }

        [Test]
        public void PassengerStartsWalkingAndCarrierExposesDoorContract()
        {
            root = new GameObject("Stage4PassengerContract");
            GameObject vehicleObject = new GameObject("Stage4Vehicle");
            vehicleObject.transform.SetParent(root.transform, false);
            FakeCarrier carrier = new FakeCarrier(vehicleObject.transform)
            {
                DoorsOpen = true,
                DoorInsideLocal = new Vector3(1f, .5f, 2f)
            };
            GameObject passengerObject = new GameObject("Stage4Passenger");
            passengerObject.transform.SetParent(root.transform, false);
            CityPublicTransportPassenger passenger =
                passengerObject.AddComponent<CityPublicTransportPassenger>();

            Assert.That(passenger.Phase, Is.EqualTo(CityPassengerPhase.Walking));
            Assert.That(passenger.CanBoard, Is.True);
            Assert.That(carrier.Hull, Is.EqualTo(vehicleObject.transform));
            Assert.That(carrier.DoorInsideLocal, Is.EqualTo(new Vector3(1f, .5f, 2f)));
            Assert.That(carrier.DoorOutside, Is.Not.EqualTo(Vector3.zero));
            Assert.That(carrier.DoorsOpen, Is.True);
        }

        [Test]
        public void SignalStopMatchesVehiclePhaseAndTrafficDiagnosticsStartClean()
        {
            root = new GameObject("Stage4TrafficContract");
            CityTrafficSignalSystem signals = root.AddComponent<CityTrafficSignalSystem>();
            Vector3 farPoint = new Vector3(0f, 0f, 0f);

            Assert.That(signals.ShouldStopVehicle(farPoint), Is.False);
            Assert.That(signals.PhaseAt(farPoint, false), Is.EqualTo(CitySignalPhase.Green));

            Vector3 signalPoint = CityTrafficSignalSystem.Definitions[0].Position;
            CitySignalPhase phase = signals.PhaseAt(signalPoint, false);
            Assert.That(signals.ShouldStopVehicle(signalPoint),
                Is.EqualTo(phase != CitySignalPhase.Green));

            CityTraffic traffic = root.AddComponent<CityTraffic>();
            Assert.That(traffic.StuckVehicleCount, Is.Zero);
            Assert.That(traffic.DeadlockCount, Is.Zero);
            Assert.That(traffic.LastDiagnostic, Is.Null);
            Assert.That(traffic.HasTrafficDeadlock, Is.False);
        }

        [Test]
        public void PlannedRouteKeepsExactEndpointAndCapsuleClearance()
        {
            Vector3 source = new Vector3(141f, 0f, -40.1f);
            Vector3 destination = new Vector3(-65f, 0f, 60f);

            bool planned = CityPedestrianNetwork.TryPlan(
                source,
                destination,
                out System.Collections.Generic.List<Vector3> route,
                out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route, Is.Not.Null);
            Assert.That(route[^1], Is.EqualTo(destination));
            Assert.That(CityPedestrianNetwork.ValidateRoute(route, .3f, out failure),
                Is.True,
                failure);
            Assert.That(WorldRouteCatalog.TryGetNearestPoint(
                "sidewalk-crossings",
                destination,
                200f,
                out Vector3 nearest,
                out float distance),
                Is.True);
            Assert.That(distance, Is.GreaterThan(0f));
            Assert.That(nearest.y, Is.EqualTo(destination.y).Within(.001f));
        }

        sealed class FakeCarrier : ICityPassengerCarrier
        {
            readonly Transform hull;

            public FakeCarrier(Transform hull)
            {
                this.hull = hull;
            }

            public Transform Hull => hull;
            public bool DoorsOpen { get; set; }
            public Vector3 DoorInsideLocal { get; set; }
            public Vector3 DoorOutside => hull
                ? hull.TransformPoint(DoorInsideLocal + Vector3.right * 2f)
                : Vector3.zero;
            public int Boardings { get; private set; }
            public int Alightings { get; private set; }

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
