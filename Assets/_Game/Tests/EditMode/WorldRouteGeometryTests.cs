using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldRouteGeometryTests
    {
        [Test]
        public void StreetNetworkUsesStableCatalogSegments()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildStreetNetwork();

            Assert.That(routes, Is.Not.Null);
            Assert.That(routes.Count, Is.EqualTo(22));
            Assert.That(WorldRouteCatalog.ValidateStreetNetwork(out string failure), Is.True, failure);
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                WorldRouteCorridor route = routes[routeIndex];
                Assert.That(route.Id, Is.Not.Null.And.Not.Empty);
                Assert.That(route.Kind, Is.EqualTo(RouteKind.Highway));
                Assert.That(route.ReservesPlacement, Is.True);
                Assert.That(route.Nodes.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void SegmentRotationAndBoundsFollowDiagonalRoute()
        {
            var route = new WorldRouteCorridor(
                "test-diagonal",
                RouteKind.Highway,
                new[]
                {
                    new Vector3(2, 0, 3),
                    new Vector3(12, 0, 13)
                },
                4,
                true);
            WorldRouteSegment segment = First(WorldRouteGeometry.Segments(route));

            Assert.That(segment.IsFinite, Is.True);
            Assert.That(segment.Length, Is.EqualTo(Mathf.Sqrt(200)).Within(.0001f));
            Assert.That(Vector3.Distance(segment.Start, segment.End), Is.EqualTo(segment.Length));
            Assert.That(Vector3.Dot(segment.Rotation * Vector3.forward, segment.Forward),
                Is.EqualTo(1).Within(.0001f));
            Bounds bounds = WorldRouteGeometry.ToBounds(segment);
            Assert.That(bounds.Contains(segment.Start), Is.True);
            Assert.That(bounds.Contains(segment.End), Is.True);
            Assert.That(bounds.size.x, Is.GreaterThan(0));
            Assert.That(bounds.size.z, Is.GreaterThan(0));
        }

        [Test]
        public void StreetBoundsAreProducedFromEveryCatalogSegment()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildStreetNetwork();
            var expectedCount = 0;
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                foreach (WorldRouteSegment segment in WorldRouteGeometry.Segments(routes[routeIndex]))
                    expectedCount++;

            var actualCount = 0;
            foreach (Bounds bounds in CityStreetNetwork.Bounds())
            {
                actualCount++;
                Assert.That(bounds.size.x, Is.GreaterThan(0));
                Assert.That(bounds.size.z, Is.GreaterThan(0));
            }

            Assert.That(actualCount, Is.EqualTo(expectedCount));
        }

        [Test]
        public void StreetCatalogReservationBlocksAStreetMidpoint()
        {
            WorldRouteCorridor route = WorldRouteCatalog.BuildStreetNetwork()[0];
            Vector3 midpoint = Vector3.Lerp(route.Nodes[0], route.Nodes[1], .5f);

            Assert.That(WorldRouteCatalog.IsReserved(midpoint, .1f, out string routeId), Is.True);
            Assert.That(routeId, Is.EqualTo(route.Id));
        }

        static WorldRouteSegment First(IEnumerable<WorldRouteSegment> segments)
        {
            foreach (WorldRouteSegment segment in segments)
                return segment;
            Assert.Fail("Expected a route segment.");
            return default;
        }
    }
}
