using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Phase2TrafficRouteTests
    {
        static readonly string[] RequiredStreetRoutes =
        {
            "street-main-south",
            "street-main-north",
            "street-east-north"
        };

        static readonly string[] RequiredSignalIds =
        {
            "west_south",
            "west_mid",
            "west_north",
            "east_south",
            "east_mid",
            "east_north"
        };

        [TestCase(-1.55f)]
        [TestCase(1.55f)]
        public void CanonicalTrafficLoopUsesStreetSamplesAndKeepsPassengerStops(float laneOffset)
        {
            Vector3[] route = CityRoadRoute.CanonicalTrafficLoop(laneOffset);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Length, Is.GreaterThan(12));

            bool foundSouthFirstStop = false;
            bool foundSouthSecondStop = false;
            bool foundNorthFirstStop = false;
            bool foundNorthSecondStop = false;
            for (int index = 0; index < route.Length; index++)
            {
                Vector3 point = route[index];
                Assert.That(IsFinite(point), Is.True, "traffic route contains a non-finite point");
                Vector3 next = route[(index + 1) % route.Length];
                Assert.That((next - point).sqrMagnitude, Is.GreaterThan(.0001f));
                Assert.That(DistanceToStreetNetwork(point), Is.LessThanOrEqualTo(2.2f));
                foundSouthFirstStop |= Mathf.Abs(point.x + 22) < .001f && point.z < -40;
                foundSouthSecondStop |= Mathf.Abs(point.x - 70) < .001f && point.z < -40;
                foundNorthFirstStop |= Mathf.Abs(point.x + 22) < .001f && point.z > 24;
                foundNorthSecondStop |= Mathf.Abs(point.x - 70) < .001f && point.z > 24;
            }

            Assert.That(foundSouthFirstStop, Is.True);
            Assert.That(foundSouthSecondStop, Is.True);
            Assert.That(foundNorthFirstStop, Is.True);
            Assert.That(foundNorthSecondStop, Is.True);
        }

        [Test]
        public void TrafficSourceRoutesExposeStableSampledGeometry()
        {
            var routes = WorldRouteCatalog.BuildStreetNetwork();
            for (int requiredIndex = 0; requiredIndex < RequiredStreetRoutes.Length; requiredIndex++)
            {
                WorldRouteCorridor route = Find(routes, RequiredStreetRoutes[requiredIndex]);
                Assert.That(route, Is.Not.Null, RequiredStreetRoutes[requiredIndex]);
                Assert.That(route.SampledNodes.Count, Is.GreaterThanOrEqualTo(2));
                for (int nodeIndex = 1; nodeIndex < route.SampledNodes.Count; nodeIndex++)
                    Assert.That((route.SampledNodes[nodeIndex] - route.SampledNodes[nodeIndex - 1]).sqrMagnitude,
                        Is.GreaterThan(.0001f));
            }
        }

        [Test]
        public void TrafficSignalsKeepStableIdsAndHighwaySupport()
        {
            var highway = Find(WorldRouteCatalog.BuildDefault(), "highway-ring");
            Assert.That(highway, Is.Not.Null);
            var definitions = CityTrafficSignalSystem.Definitions;
            Assert.That(definitions.Count, Is.EqualTo(RequiredSignalIds.Length));
            for (int index = 0; index < definitions.Count; index++)
            {
                Assert.That(definitions[index].Id, Is.EqualTo(RequiredSignalIds[index]));
                Assert.That(DistanceToRoute(definitions[index].Position, highway.SampledNodes),
                    Is.LessThanOrEqualTo(6.25f));
            }
        }

        static WorldRouteCorridor Find(
            System.Collections.Generic.IReadOnlyList<WorldRouteCorridor> routes,
            string id)
        {
            for (int index = 0; index < routes.Count; index++)
                if (routes[index].Id == id)
                    return routes[index];
            return null;
        }

        static float DistanceToStreetNetwork(Vector3 point)
        {
            float nearest = float.PositiveInfinity;
            var routes = WorldRouteCatalog.BuildStreetNetwork();
            for (int index = 0; index < routes.Count; index++)
                nearest = Mathf.Min(nearest, DistanceToRoute(point, routes[index].SampledNodes));
            return nearest;
        }

        static float DistanceToRoute(
            Vector3 point,
            System.Collections.Generic.IReadOnlyList<Vector3> samples)
        {
            float nearest = float.PositiveInfinity;
            for (int index = 1; index < samples.Count; index++)
            {
                Vector3 start = samples[index - 1];
                Vector3 end = samples[index];
                Vector3 delta = end - start;
                delta.y = 0;
                Vector3 offset = point - start;
                offset.y = 0;
                float length = delta.sqrMagnitude;
                float amount = length <= .000001f
                    ? 0
                    : Mathf.Clamp01(Vector3.Dot(offset, delta) / length);
                Vector3 closest = start + delta * amount;
                closest.y = point.y;
                nearest = Mathf.Min(nearest, Vector3.Distance(point, closest));
            }
            return nearest;
        }

        static bool IsFinite(Vector3 point)
        {
            return !float.IsNaN(point.x) && !float.IsInfinity(point.x)
                && !float.IsNaN(point.y) && !float.IsInfinity(point.y)
                && !float.IsNaN(point.z) && !float.IsInfinity(point.z);
        }
    }
}
