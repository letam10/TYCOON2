using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon.Tests
{
    public sealed class CityPedestrianRouteTests : CityPedestrianPlayModeFixture
    {
        GameObject world;
        GameObject actor;

        protected override void ClearWorld()
        {
            if (actor) Object.DestroyImmediate(actor);
            if (world) Object.DestroyImmediate(world);
        }

        [UnityTest]
        public IEnumerator CrossDistrictRouteUsesConnectedCorridorsAndKeepsExactFinalPoint()
        {
            Vector3 goal = new(-65, 0, 60);
            var route = CityPedestrianNetwork.Plan(new(141, 0, -40), goal);
            Assert.That(route.Count, Is.GreaterThan(5));
            Assert.That(route[^1], Is.EqualTo(goal));
            Assert.That(route, Does.Contain(new Vector3(-78.6f, 0, 24.1f)));
            Assert.That(route, Does.Contain(new Vector3(-78.6f, 0, 61.6f)));
            yield break;
        }

        [UnityTest]
        public IEnumerator EveryLongNodePairConnectsWithoutSkippingTheStartingCorridor()
        {
            foreach (var from in CityPedestrianNetwork.Nodes)
                foreach (var to in CityPedestrianNetwork.Nodes)
                {
                    if (Vector3.Distance(from, to) <= CityPedestrianNetwork.LocalTrip) continue;
                    var route = CityPedestrianNetwork.Plan(from, to);
                    Assert.That(route[0], Is.EqualTo(from));
                    Assert.That(route[^1], Is.EqualTo(to));
                    Assert.That(route.Count, Is.GreaterThanOrEqualTo(3));
                }
            yield break;
        }

        [UnityTest]
        public IEnumerator NearbyQueueMovementUsesOnlyItsExactConnector()
        {
            Vector3 destination = new(-55, 0, -14);
            var route = CityPedestrianNetwork.Plan(new(-57, 0, -13), destination);
            Assert.That(route, Is.EqualTo(new[] { destination }));
            yield break;
        }

        [UnityTest]
        public IEnumerator IntermediateNodeIsNotFinalArrivalAndRecoveryKeepsFinalDestination()
        {
            var agent = CreateAgent(new(141, 0, -40.1f));
            Assert.That(Navigation.Go(agent, new(-66.7f, 0, 61.6f)), Is.True);
            var route = actor.GetComponent<CityPedestrianRoute>();
            Vector3 final = route.FinalDestination;
            Assert.That(route.RemainingNodes, Is.GreaterThan(1));
            Assert.That(Navigation.Arrived(agent), Is.False);
            Assert.That(Navigation.Go(agent, agent.destination, true), Is.True);
            Assert.That(route.FinalDestination, Is.EqualTo(final));
            Assert.That(Navigation.Arrived(agent), Is.False);
            yield break;
        }

        [UnityTest]
        public IEnumerator PoolSuspensionClearsOldRouteAndCannotReportArrival()
        {
            var agent = CreateAgent(new(141, 0, -40.1f));
            Assert.That(Navigation.Go(agent, new(-66.7f, 0, 61.6f)), Is.True);
            Navigation.Suspend(agent);
            Assert.That(actor.GetComponent<CityPedestrianRoute>().HasDestination, Is.False);
            Assert.That(Navigation.Arrived(agent), Is.False);
            Assert.That(actor.GetComponent<CharacterController>().enabled, Is.False);
            yield break;
        }

        [UnityTest]
        public IEnumerator PlayerAvoidanceIsCylindricalWithoutCarving()
        {
            actor = new GameObject("PlayerObstacleTest");
            CityPedestrianPlayerObstacle.Attach(actor);
            CityPedestrianPlayerObstacle.Attach(actor);
            var obstacle = actor.GetComponent<NavMeshObstacle>();
            Assert.That(actor.GetComponents<NavMeshObstacle>().Length, Is.EqualTo(1));
            Assert.That(obstacle.shape, Is.EqualTo(NavMeshObstacleShape.Capsule));
            Assert.That(obstacle.carving, Is.False);
            Assert.That(obstacle.radius, Is.GreaterThan(.3f));
            yield break;
        }

        NavMeshAgent CreateAgent(Vector3 point)
        {
            world = new GameObject("RouteTestWorld");
            var floor = new GameObject("Floor", typeof(BoxCollider));
            floor.transform.SetParent(world.transform);
            floor.transform.position = new(20, -.1f, 20);
            floor.GetComponent<BoxCollider>().size = new(290, .2f, 170);
            Physics.SyncTransforms();
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            actor = new GameObject("RouteTestActor");
            Assert.That(NavMesh.SamplePosition(point, out var hit, 1, NavMesh.AllAreas), Is.True);
            actor.transform.position = hit.position;
            var agent = Navigation.Agent(actor);
            Assert.That(agent.isOnNavMesh, Is.True, "Native agent registered on baked test surface");
            return agent;
        }
    }
}
