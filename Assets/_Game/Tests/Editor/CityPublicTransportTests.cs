using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon.Tests
{
    public sealed class CityPublicTransportTests : CityPedestrianPlayModeFixture
    {
        GameObject world;
        GameObject actor;
        GameObject bus;

        protected override void CreateWorld()
        {
            world = new GameObject("TransitTestFloor", typeof(BoxCollider));
            world.transform.position = new(0, -.1f, 0);
            world.GetComponent<BoxCollider>().size = new(240, .2f, 140);
            Physics.SyncTransforms();
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            bus = new GameObject("TransitTestBus");
            actor = new GameObject("TransitTestResident");
        }

        protected override void ClearWorld()
        {
            if (actor) Object.DestroyImmediate(actor);
            if (bus) Object.DestroyImmediate(bus);
            if (world) Object.DestroyImmediate(world);
        }

        [UnityTest]
        public IEnumerator CancellingDoorApproachKeepsPedestrianCollisionAndClearsReservation()
        {
            var vehicle = bus.AddComponent<CityPublicTransportVehicle>();
            vehicle.Initialize(null, null, 0, 0);
            Physics.SyncTransforms();
            actor.transform.position = vehicle.DoorOutside + Vector3.forward * 2;
            var agent = Navigation.Agent(actor);
            Assert.That(agent.isOnNavMesh, Is.True, "Native agent registered on baked test surface");
            var passenger = actor.AddComponent<CityPublicTransportPassenger>();
            Assert.That(passenger.Reserve(vehicle, new(-.5f, .38f, -1)), Is.True);
            Assert.That(passenger.Phase, Is.EqualTo(CityPassengerPhase.Approaching));
            passenger.CancelApproach();
            Assert.That(passenger.Phase, Is.EqualTo(CityPassengerPhase.Walking));
            Assert.That(agent.enabled, Is.True);
            Assert.That(actor.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(actor.GetComponent<CityPedestrianRoute>().HasDestination, Is.False);
            yield break;
        }

        [UnityTest]
        public IEnumerator PooledPassengerMustWalkOutsideBeforeCapsuleCanBeRestored()
        {
            var passenger = SeatedPassenger(out var vehicle);
            actor.SetActive(false);
            Assert.That(passenger.Phase, Is.EqualTo(CityPassengerPhase.Disembarking));
            Assert.That(actor.GetComponent<CharacterController>().enabled, Is.False);
            Assert.That(actor.transform.parent, Is.EqualTo(bus.transform));
            Vector3 pooledPosition = actor.transform.position;
            passenger.Tick(.05f);
            Assert.That(actor.transform.position, Is.EqualTo(pooledPosition));
            actor.SetActive(true);
            Assert.That(actor.GetComponent<CharacterController>().enabled, Is.False);
            passenger.Tick(.05f);
            Assert.That(actor.transform.parent, Is.Null);
            Assert.That(actor.GetComponent<CharacterController>().enabled, Is.False);
            for (int i = 0; i < 200; i++) passenger.Tick(.05f);
            Assert.That(passenger.Phase, Is.EqualTo(CityPassengerPhase.Walking));
            Assert.That(actor.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(actor.GetComponent<NavMeshAgent>().enabled, Is.True);
            Assert.That(Vector3.Distance(actor.transform.position, vehicle.DoorOutside), Is.LessThan(.1f));
            yield break;
        }

        [UnityTest]
        public IEnumerator BlockedExitKeepsCapsuleDisabledUntilSpaceIsActuallyClear()
        {
            var passenger = SeatedPassenger(out var vehicle);
            var blocker = new GameObject("BlockedDoor", typeof(BoxCollider));
            try
            {
                blocker.transform.position = vehicle.DoorOutside + Vector3.up;
                blocker.GetComponent<BoxCollider>().size = new(1.2f, 2, 1.2f);
                Physics.SyncTransforms();
                passenger.ReleaseCarrier();
                for (int i = 0; i < 100; i++) passenger.Tick(.05f);
                Assert.That(passenger.Phase, Is.EqualTo(CityPassengerPhase.Disembarking));
                Assert.That(actor.GetComponent<CharacterController>().enabled, Is.False);
                blocker.SetActive(false);
                Physics.SyncTransforms();
                for (int i = 0; i < 100; i++) passenger.Tick(.05f);
                Assert.That(passenger.Phase, Is.EqualTo(CityPassengerPhase.Walking));
                Assert.That(actor.GetComponent<CharacterController>().enabled, Is.True);
                Assert.That(passenger.CompletedRides, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(blocker);
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator ZeroDeltaDoesNotMoveASeatedOrDisembarkingPassenger()
        {
            var passenger = SeatedPassenger(out _);
            passenger.ReleaseCarrier();
            Vector3 before = actor.transform.position;
            passenger.Tick(0);
            Assert.That(actor.transform.position, Is.EqualTo(before));
            Assert.That(actor.GetComponent<CharacterController>().enabled, Is.False);
            yield break;
        }

        CityPublicTransportPassenger SeatedPassenger(out CityPublicTransportVehicle vehicle)
        {
            vehicle = bus.AddComponent<CityPublicTransportVehicle>();
            vehicle.Initialize(null, null, 0, 0);
            Physics.SyncTransforms();
            actor.transform.position = vehicle.DoorOutside;
            var agent = Navigation.Agent(actor);
            Assert.That(agent.isOnNavMesh, Is.True, "Native agent registered on baked test surface");
            var passenger = actor.AddComponent<CityPublicTransportPassenger>();
            Assert.That(passenger.Reserve(vehicle, new(-.5f, .38f, -1)), Is.True);
            Navigation.Suspend(agent);
            actor.transform.SetParent(bus.transform, false);
            actor.transform.localPosition = new(-.5f, .38f, -1);
            // Fixture bắt đầu ở ghế để tập trung kiểm tra phục hồi sau pool/load.
            typeof(CityPublicTransportPassenger).GetProperty(nameof(passenger.Phase))!
                .GetSetMethod(true)!.Invoke(passenger, new object[] { CityPassengerPhase.Seated });
            return passenger;
        }
    }
}
