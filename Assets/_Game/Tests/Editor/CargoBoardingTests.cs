using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Tycoon.Tests
{
    public sealed class CargoBoardingTests : CityPedestrianPlayModeFixture
    {
        static readonly Vector3 RecordedDoor = new(-24.779f, 0, 33.728f);
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject world;
        CityLife life;
        NavMeshSurface surface;

        protected override void CreateWorld()
        {
            world = new GameObject("CargoBoardingFixture");
            life = world.AddComponent<CityLife>();
            life.enabled = false;
            Box("Sidewalk", At(0, 0) + Vector3.down * .1f, new(60, .2f, 20));
            Box("DisconnectedSidewalk", At(0, 16) + Vector3.down * .1f, new(8, .2f, 4));
            surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Bake();
        }

        protected override void ClearWorld()
        {
            if (world) UnityEngine.Object.DestroyImmediate(world);
        }

        [UnityTest]
        public IEnumerator ClosestReachableResidentWinsInsteadOfFirstInPool()
        {
            AddResident(At(15, 0));
            var nearest = AddResident(At(4, 0));
            Assert.That(life.FindPassenger(RecordedDoor, 22), Is.EqualTo(nearest));
            yield break;
        }

        [UnityTest]
        public IEnumerator DisconnectedResidentDoesNotHideReachablePassenger()
        {
            var disconnected = AddResident(At(0, 16));
            var reachable = AddResident(At(10, 8));
            Vector3 stop = At(0, 8);
            var path = new NavMeshPath();
            disconnected.CalculatePath(stop, path);
            Assert.That(path.status, Is.Not.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(life.FindPassenger(stop, 22), Is.EqualTo(reachable));
            yield break;
        }

        [UnityTest]
        public IEnumerator LongDetourCannotConsumeShortBoardingWindow()
        {
            Box("Wall", At(0, 0) + Vector3.up * 1.5f, new(1, 3, 16));
            Bake();
            var behindWall = AddResident(At(-3, 0));
            Vector3 stop = At(3, 0);
            var path = new NavMeshPath();
            Assert.That(behindWall.CalculatePath(stop, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(life.FindPassenger(stop, 10), Is.Null);
            var reachable = AddResident(At(4, 0));
            Assert.That(life.FindPassenger(stop, 10), Is.EqualTo(reachable));
            yield break;
        }

        [UnityTest]
        public IEnumerator RecordedCargoDoorCanReserveResidentBeyondOldRadiusWithinExistingDwell()
        {
            var vehicle = new GameObject("CargoAtRecordedStop");
            vehicle.transform.SetParent(world.transform, false);
            vehicle.transform.position = new(-21.699f, 0, 30.55f);
            vehicle.transform.rotation = Quaternion.LookRotation(Vector3.left);
            var rig = vehicle.AddComponent<CityVehicleRig>();
            rig.Kind = CityVehicleKind.CargoTruck;
            rig.DoorInsideLocal = new(1.078f, .85f, 3.08f);
            var service = vehicle.AddComponent<CityRoadVehiclePassengers>();
            service.Initialize(null, life, rig);
            service.BeginStop();
            Assert.That(Vector3.Distance(service.DoorOutside, RecordedDoor), Is.LessThan(.01f));
            Assert.That((float)typeof(CityRoadVehiclePassengers).GetField("dwell", PrivateInstance)
                .GetValue(service), Is.EqualTo(18));
            var resident = AddResident(new(-8, 0, 33.9f));
            resident.speed = 1.7f;
            Assert.That(life.FindPassenger(service.DoorOutside, 11), Is.Null);
            float radius = SearchRadius(17.4f);
            Assert.That(radius, Is.LessThanOrEqualTo(CityPedestrianNetwork.LocalTrip));
            Assert.That(life.FindPassenger(service.DoorOutside, radius), Is.EqualTo(resident));
            Assert.That(Vector3.Distance(resident.transform.position, service.DoorOutside) / resident.speed + 4,
                Is.LessThan(17.4f));
            var passenger = resident.gameObject.AddComponent<CityPublicTransportPassenger>();
            Assert.That(passenger.Reserve(service, new(-.4f, .85f, 2.88f)), Is.True);
            var route = resident.GetComponent<CityPedestrianRoute>();
            Assert.That(route.HasDestination, Is.True);
            Assert.That(route.RemainingNodes, Is.Zero, "Boarding stays within a bounded local walking trip.");
            Assert.That(Vector3.Distance(route.FinalDestination, service.DoorOutside), Is.LessThan(.45f));
            passenger.CancelApproach();
            yield break;
        }

        [UnityTest]
        public IEnumerator SearchBudgetShrinksAndNeverExtendsTheLocalWalkingBound()
        {
            Assert.That(SearchRadius(18), Is.EqualTo(22));
            Assert.That(SearchRadius(12), Is.EqualTo(13.6f).Within(.001f));
            Assert.That(SearchRadius(8), Is.EqualTo(6.8f).Within(.001f));
            Assert.That(SearchRadius(4), Is.Zero);
            Assert.That(SearchRadius(0), Is.Zero);
            yield break;
        }

        [UnityTest]
        public IEnumerator InvalidDoorLocationAndUnavailableResidentsRemainIneligible()
        {
            var resident = AddResident(At(0, 8));
            Assert.That(life.FindPassenger(At(0, 12), 11), Is.Null,
                "A nearby resident cannot make a door in a NavMesh gap reachable.");
            var passenger = resident.gameObject.AddComponent<CityPublicTransportPassenger>();
            typeof(CityPublicTransportPassenger).GetField("cooldown", PrivateInstance)
                .SetValue(passenger, Time.time + 35);
            Assert.That(life.FindPassenger(At(0, 8), 11), Is.Null);
            yield break;
        }

        NavMeshAgent AddResident(Vector3 position)
        {
            var actor = new GameObject("ExistingResidentFixture");
            actor.transform.SetParent(world.transform, false);
            actor.transform.position = position;
            var agent = Navigation.Agent(actor);
            Assert.That(agent.isOnNavMesh, Is.True);
            var type = typeof(CityLife).GetNestedType("Resident", BindingFlags.NonPublic);
            var record = Activator.CreateInstance(type, true);
            type.GetField("Agent").SetValue(record, agent);
            ((IList)typeof(CityLife).GetField("residents", PrivateInstance).GetValue(life)).Add(record);
            return agent;
        }

        static float SearchRadius(float dwell) =>
            (float)typeof(CityRoadVehiclePassengers).GetMethod("BoardingSearchRadius",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { dwell });

        static Vector3 At(float x, float z) => RecordedDoor + new Vector3(x, 0, z);

        void Box(string name, Vector3 position, Vector3 size)
        {
            var part = new GameObject(name, typeof(BoxCollider));
            part.transform.SetParent(world.transform, false);
            part.transform.position = position;
            part.GetComponent<BoxCollider>().size = size;
        }

        void Bake()
        {
            Physics.SyncTransforms();
            surface.BuildNavMesh();
        }
    }
}
