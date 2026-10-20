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

        [Test]
        public void StreetJunctionsCoverSharedEndpointsAndCrossings()
        {
            IReadOnlyList<WorldRouteJunction> junctions =
                WorldRouteGeometry.FindJunctions(WorldRouteCatalog.BuildStreetNetwork());

            Assert.That(junctions, Is.Not.Null);
            Assert.That(junctions.Count, Is.GreaterThan(0));
            bool foundMainWestIntersection = false;
            for (int index = 0; index < junctions.Count; index++)
            {
                WorldRouteJunction junction = junctions[index];
                Assert.That(junction.Width, Is.GreaterThan(0));
                Assert.That(IsFinite(junction.Point), Is.True);
                if ((junction.Point - new Vector3(-105, 0, 29)).sqrMagnitude < .01f)
                    foundMainWestIntersection = true;
            }
            Assert.That(foundMainWestIntersection, Is.True);
        }

        [Test]
        public void SidewalkSegmentTrimsAtSharedStreetEndpoints()
        {
            WorldRouteCorridor route = WorldRouteCatalog.BuildStreetNetwork()[2];
            WorldRouteSegment segment = First(WorldRouteGeometry.Segments(route));
            IReadOnlyList<WorldRouteJunction> junctions =
                WorldRouteGeometry.FindJunctions(WorldRouteCatalog.BuildStreetNetwork());
            WorldRouteSegment trimmed = WorldRouteGeometry.TrimAtJunctions(
                segment,
                junctions,
                .9f);

            Assert.That(trimmed.Width, Is.EqualTo(segment.Width));
            Assert.That(trimmed.Length, Is.LessThan(segment.Length));
            Assert.That(trimmed.Start, Is.Not.EqualTo(segment.Start));
            Assert.That(trimmed.End, Is.Not.EqualTo(segment.End));
        }

        [Test]
        public void DefaultRoutesHaveNoUnexpectedSelfIntersections()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            for (int index = 0; index < routes.Count; index++)
            {
                Assert.That(
                    WorldRouteGeometry.ValidateNoUnexpectedSelfIntersections(
                        routes[index],
                        out string failure),
                    Is.True,
                    failure);
            }
        }

        [Test]
        public void CrossRouteDiagnosticsClassifyARealCrossing()
        {
            var routes = new[]
            {
                new WorldRouteCorridor(
                    "crossing-a",
                    RouteKind.Highway,
                    new[]
                    {
                        new Vector3(-10, 0, -10),
                        new Vector3(10, 0, 10)
                    },
                    2,
                    true),
                new WorldRouteCorridor(
                    "crossing-b",
                    RouteKind.Highway,
                    new[]
                    {
                        new Vector3(-10, 0, 10),
                        new Vector3(10, 0, -10)
                    },
                    2,
                    true)
            };

            IReadOnlyList<WorldRouteOverlap> overlaps =
                WorldRouteDiagnostics.FindCrossRouteOverlaps(routes);

            Assert.That(overlaps, Has.Count.EqualTo(1));
            Assert.That(overlaps[0].Kind, Is.EqualTo(WorldRouteOverlapKind.Crossing));
            Assert.That(
                WorldRouteDiagnostics.ValidateCrossRouteOverlaps(
                    routes,
                    out string failure),
                Is.True,
                failure);
        }

        [Test]
        public void CrossRouteDiagnosticsRejectParallelClearanceOverlap()
        {
            var routes = new[]
            {
                new WorldRouteCorridor(
                    "parallel-a",
                    RouteKind.Highway,
                    new[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(20, 0, 0)
                    },
                    4,
                    true),
                new WorldRouteCorridor(
                    "parallel-b",
                    RouteKind.Rail,
                    new[]
                    {
                        new Vector3(0, 0, 3),
                        new Vector3(20, 0, 3)
                    },
                    4,
                    true)
            };

            IReadOnlyList<WorldRouteOverlap> overlaps =
                WorldRouteDiagnostics.FindCrossRouteOverlaps(routes);

            Assert.That(overlaps, Has.Count.EqualTo(1));
            Assert.That(
                overlaps[0].Kind,
                Is.EqualTo(WorldRouteOverlapKind.ParallelClearance));
            Assert.That(
                WorldRouteDiagnostics.ValidateCrossRouteOverlaps(
                    routes,
                    out string failure),
                Is.False);
            Assert.That(failure, Does.Contain("parallel-a"));
        }

        [Test]
        public void DefaultRouteKindBoundariesRemainValid()
        {
            Assert.That(
                WorldRouteDiagnostics.ValidateRouteKindBoundaries(
                    WorldRouteCatalog.BuildDefault(),
                    out string failure),
                Is.True,
                failure);
        }

        [Test]
        public void OpenRouteRejectsIntersectionBetweenFirstAndLastSegments()
        {
            var route = new WorldRouteCorridor(
                "open-crossing",
                RouteKind.Highway,
                new[]
                {
                    new Vector3(-10, 0, -10),
                    new Vector3(10, 0, 10),
                    new Vector3(-10, 0, 10),
                    new Vector3(10, 0, -10)
                },
                2,
                true);

            Assert.That(
                WorldRouteGeometry.ValidateNoUnexpectedSelfIntersections(
                    route,
                    out string failure),
                Is.False,
                "The open route crosses itself between its first and last edges.");
            Assert.That(failure, Does.Contain("open-crossing"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CollinearOverlapIsRejectedEvenWithSharedEndpoint(bool reverse)
        {
            Vector3[] secondPoints =
            {
                new Vector3(0, 0, 0),
                new Vector3(10, 0, 0)
            };
            if (reverse)
                System.Array.Reverse(secondPoints);
            var routes = new[]
            {
                new WorldRouteCorridor("long-edge", RouteKind.Highway,
                    new[] { Vector3.zero, new Vector3(20, 0, 0) }, 2, true),
                new WorldRouteCorridor("short-edge", RouteKind.Rail,
                    secondPoints, 2, true)
            };

            IReadOnlyList<WorldRouteOverlap> overlaps =
                WorldRouteDiagnostics.FindCrossRouteOverlaps(routes);

            Assert.That(overlaps, Has.Count.EqualTo(1));
            Assert.That(overlaps[0].Kind,
                Is.EqualTo(WorldRouteOverlapKind.ParallelClearance));
            Assert.That(WorldRouteDiagnostics.ValidateRouteKindBoundaries(
                routes, out _), Is.False);
        }

        [Test]
        public void CrossingDiagnosticReportsTheActualIntersection()
        {
            var routes = new[]
            {
                new WorldRouteCorridor("horizontal", RouteKind.Highway,
                    new[] { new Vector3(-10, 0, -2), new Vector3(30, 0, -2) },
                    2, true),
                new WorldRouteCorridor("vertical", RouteKind.Highway,
                    new[] { new Vector3(0, 0, -10), new Vector3(0, 0, 5) },
                    2, true)
            };

            IReadOnlyList<WorldRouteOverlap> overlaps =
                WorldRouteDiagnostics.FindCrossRouteOverlaps(routes);

            Assert.That(overlaps, Has.Count.EqualTo(1));
            Assert.That(overlaps[0].Kind, Is.EqualTo(WorldRouteOverlapKind.Crossing));
            Assert.That(Vector3.Distance(overlaps[0].Point, new Vector3(0, 0, -2)),
                Is.LessThan(.0001f));
        }

        static WorldRouteSegment First(IEnumerable<WorldRouteSegment> segments)
        {
            foreach (WorldRouteSegment segment in segments)
                return segment;
            Assert.Fail("Expected a route segment.");
            return default;
        }

        static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y)
                && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z)
                && !float.IsInfinity(value.z);
        }
    }
}
