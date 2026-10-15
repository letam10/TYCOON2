using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityPedestrianPublicRouteTests
    {
        [Test]
        public void PublicRouteAvoidsFarmGraphWhenDirectSegmentSkirtsFarm()
        {
            Vector3 from = new(141, 0, -40.1f);
            Vector3 destination = new(-65, 0, 60);
            Bounds farm = CityDistricts.Footprint("farm");

            Assert.That(farm.Contains(from), Is.False);
            Assert.That(farm.Contains(destination), Is.False);
            Assert.That(CityPedestrianNetwork.SegmentIntersectsFarm(from, destination), Is.False);

            bool planned = CityPedestrianNetwork.TryPlan(
                from, destination, out List<Vector3> route, out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Count, Is.GreaterThan(1));
            Assert.That(route[0], Is.EqualTo(from));
            Assert.That(route[^1], Is.EqualTo(destination));

            for (int index = 0; index < route.Count; index++)
            {
                Vector3 point = route[index];
                Assert.That(!farm.Contains(point) || IsSidewalkPoint(point), Is.True,
                    "Public route waypoint entered the farm outside a sidewalk: " + point);
                Assert.That(IsFarmReserved(point), Is.False,
                    "Public route waypoint used a reserved farm route: " + point);
            }

            for (int index = 1; index < route.Count; index++)
            {
                bool crossesFarm = CityPedestrianNetwork.SegmentIntersectsFarm(
                    route[index - 1], route[index]);
                bool designatedSidewalk = IsSidewalkPoint(route[index - 1]) ||
                    IsSidewalkPoint(route[index]);
                Assert.That(!crossesFarm || designatedSidewalk, Is.True,
                    "Public route edge entered the farm outside a sidewalk: " +
                    route[index - 1] + " -> " + route[index]);
            }
        }

        [Test]
        public void FarmToSupermarketCompletesThroughSidewalkCrossing()
        {
            Vector3 from = CityDistricts.Center("farm");
            Vector3 destination = CityDistricts.Center("supermarket");

            bool planned = CityPedestrianNetwork.TryPlan(
                from, destination, out List<Vector3> route, out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Count, Is.GreaterThan(1));
            Assert.That(route[0], Is.EqualTo(from));
            Assert.That(route[^1], Is.EqualTo(destination));
            Assert.That(WorldRouteCatalog.TryGet(
                "sidewalk-crossings", out WorldRouteCorridor sidewalk), Is.True);

            bool usesSidewalkCrossing = false;
            for (int routeIndex = 0; routeIndex < route.Count && !usesSidewalkCrossing; routeIndex++)
            {
                for (int nodeIndex = 0; nodeIndex < sidewalk.Nodes.Count; nodeIndex++)
                {
                    if (Vector3.Distance(route[routeIndex], sidewalk.Nodes[nodeIndex]) <= .001f)
                    {
                        usesSidewalkCrossing = true;
                        break;
                    }
                }
            }

            Assert.That(usesSidewalkCrossing, Is.True);
        }

        static bool IsFarmReserved(Vector3 point)
        {
            if (IsSidewalkPoint(point)) return false;
            if (!WorldRouteCatalog.IsReserved(point, .05f, out string routeId))
                return false;
            if (!WorldRouteCatalog.TryGet(routeId, out WorldRouteCorridor corridor))
                return false;
            return corridor.Kind == RouteKind.FarmPath;
        }

        static bool IsSidewalkPoint(Vector3 point)
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                WorldRouteCorridor corridor = routes[routeIndex];
                if (corridor.Kind != RouteKind.Sidewalk) continue;
                for (int index = 0; index < corridor.Nodes.Count; index++)
                    if (Vector3.Distance(point, corridor.Nodes[index]) <= .001f)
                        return true;
            }
            return false;
        }
    }
}
