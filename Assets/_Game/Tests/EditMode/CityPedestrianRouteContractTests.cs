using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityPedestrianRouteContractTests
    {
        [Test]
        public void TryPlanReturnsConnectedRouteWithExactDestination()
        {
            Vector3 from = new(141, 0, -40.1f);
            Vector3 destination = new(-65, 0, 60);

            bool planned = CityPedestrianNetwork.TryPlan(from, destination, out List<Vector3> route,
                out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Count, Is.GreaterThan(1));
            Assert.That(route[0], Is.EqualTo(from));
            Assert.That(route[^1], Is.EqualTo(destination));
            for (int index = 1; index < route.Count; index++)
                Assert.That(Vector3.Distance(route[index - 1], route[index]), Is.GreaterThan(.01f));
        }

        [Test]
        public void TryPlanReturnsFailureForPointOutsidePedestrianWorld()
        {
            bool planned = CityPedestrianNetwork.TryPlan(Vector3.zero, new(1000, 0, 1000),
                out List<Vector3> route, out string failure);

            Assert.That(planned, Is.False);
            Assert.That(route, Is.Empty);
            Assert.That(failure, Does.Contain("outside"));
        }

        [Test]
        public void TryPlanKeepsShortTripDirectPolicy()
        {
            Vector3 from = new(-57, 0, -13);
            Vector3 destination = new(-55, 0, -14);

            bool planned = CityPedestrianNetwork.TryPlan(from, destination, out List<Vector3> route,
                out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route, Is.EqualTo(new[] { destination }));
        }

        [Test]
        public void LongFarmTripUsesSidewalkCorridorInsteadOfFarmInterior()
        {
            Vector3 from = CityDistricts.Center("farm");
            Vector3 destination = CityDistricts.Center("supermarket");

            bool planned = CityPedestrianNetwork.TryPlan(from, destination, out List<Vector3> route,
                out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route, Does.Contain(new Vector3(-46, 0, 29)));
            Assert.That(route[^1], Is.EqualTo(destination));
        }

        [Test]
        public void FarmServiceLoopReservesPlacementButPublicTripAvoidsIt()
        {
            Assert.That(WorldRouteCatalog.TryGet("farm-service-loop", out WorldRouteCorridor loop), Is.True);
            Assert.That(loop.Kind, Is.EqualTo(RouteKind.FarmPath));
            Vector3 reservedPoint = Vector3.Lerp(loop.Nodes[0], loop.Nodes[1], .5f);
            Assert.That(WorldRouteCatalog.IsReserved(reservedPoint, .2f, out string routeId), Is.True);
            Assert.That(routeId, Is.EqualTo("farm-service-loop"));

            bool planned = CityPedestrianNetwork.TryPlan(
                CityDistricts.Center("farm"), CityDistricts.Center("supermarket"),
                out List<Vector3> route, out string failure);

            Assert.That(planned, Is.True, failure);
            bool enteredServiceLoop = route.Any(point =>
                WorldRouteCatalog.IsReserved(point, .05f, out string pointRoute)
                && pointRoute == "farm-service-loop");
            Assert.That(enteredServiceLoop, Is.False);
        }

        [Test]
        public void NonFinitePointReturnsFailureAndNoPartialRoute()
        {
            bool planned = CityPedestrianNetwork.TryPlan(new(float.NaN, 0, 0), Vector3.zero,
                out List<Vector3> route, out string failure);

            Assert.That(planned, Is.False);
            Assert.That(route, Is.Empty);
            Assert.That(failure, Does.Contain("finite"));
        }
    }
}
