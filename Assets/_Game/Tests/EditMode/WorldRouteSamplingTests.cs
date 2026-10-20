using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldRouteSamplingTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void SampledNodesCopyInputAndRemainCachedAndReadOnly(bool publicApproach)
        {
            Vector3[] input = CurvedAnchors();
            IReadOnlyList<Vector3> anchors;
            IReadOnlyList<Vector3> samples;
            if (publicApproach)
            {
                var approach = new WorldPublicApproach("sample-approach", input, 3.25f);
                anchors = approach.Nodes;
                samples = approach.SampledNodes;
                Assert.That(approach.SampledNodes, Is.SameAs(samples));
            }
            else
            {
                var corridor = new WorldRouteCorridor(
                    "sample-corridor", RouteKind.Sidewalk, input, 3.25f, true);
                anchors = corridor.Nodes;
                samples = corridor.SampledNodes;
                Assert.That(corridor.SampledNodes, Is.SameAs(samples));
            }

            Vector3[] before = new Vector3[samples.Count];
            for (int index = 0; index < samples.Count; index++)
                before[index] = samples[index];
            for (int index = 0; index < input.Length; index++)
                input[index] = new Vector3(999, 999, 999);

            CollectionAssert.AreEqual(CurvedAnchors(), anchors);
            CollectionAssert.AreEqual(before, samples);
            AssertReadOnly(anchors);
            AssertReadOnly(samples);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TwoAnchorRouteRetainsItsExactTwoPointRepresentation(bool publicApproach)
        {
            Vector3[] input = { new Vector3(-4, 2, 8), new Vector3(12, 7, -6) };
            IReadOnlyList<Vector3> anchors;
            IReadOnlyList<Vector3> samples;
            if (publicApproach)
            {
                var approach = new WorldPublicApproach("two-point-approach", input, 2);
                anchors = approach.Nodes;
                samples = approach.SampledNodes;
            }
            else
            {
                var corridor = new WorldRouteCorridor(
                    "two-point-corridor", RouteKind.Sidewalk, input, 2, true);
                anchors = corridor.Nodes;
                samples = corridor.SampledNodes;
            }

            Assert.That(samples, Is.SameAs(anchors));
            CollectionAssert.AreEqual(input, samples);
            Assert.That(samples.Count, Is.EqualTo(2));
        }

        [Test]
        public void EveryCatalogRouteKeepsExactAnchorsAndFiniteSamplesInsideEachSpan()
        {
            foreach (WorldRouteCorridor route in WorldRouteCatalog.BuildDefault())
                AssertBoundedSamples(route.Id, route.Nodes, route.SampledNodes);
            foreach (WorldRouteCorridor route in WorldRouteCatalog.BuildStreetNetwork())
                AssertBoundedSamples(route.Id, route.Nodes, route.SampledNodes);
            foreach (WorldPublicApproach route in WorldRouteCatalog.BuildPublicApproaches())
                AssertBoundedSamples(route.Id, route.Nodes, route.SampledNodes);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CurvedExampleMatchesIndependentQuarterAndMidpointCoordinates(bool publicApproach)
        {
            IReadOnlyList<Vector3> samples = publicApproach
                ? new WorldPublicApproach("numeric-approach", CurvedAnchors(), 3).SampledNodes
                : new WorldRouteCorridor(
                    "numeric-corridor", RouteKind.Sidewalk, CurvedAnchors(), 3, true).SampledNodes;

            Assert.That(samples.Count, Is.EqualTo(9));
            AssertVector(samples[1], new Vector3(1.4375f, .90625f, 1.8125f));
            AssertVector(samples[2], new Vector3(3.5f, 2.25f, 4.5f));
            AssertVector(samples[3], new Vector3(5.8125f, 3.46875f, 6.9375f));
            AssertVector(samples[6], new Vector3(12.5f, 2.25f, 4.5f));
            Assert.That(samples[2], Is.Not.EqualTo(new Vector3(4, 2, 4)),
                "A raw anchor chord must not replace the curved midpoint.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SegmentOverloadUsesEveryCachedEdgeWithItsOriginalWidth(bool publicApproach)
        {
            const float width = 3.25f;
            IReadOnlyList<Vector3> samples;
            IEnumerable<WorldRouteSegment> segments;
            if (publicApproach)
            {
                var approach = new WorldPublicApproach("segment-approach", CurvedAnchors(), width);
                samples = approach.SampledNodes;
                segments = WorldRouteGeometry.Segments(approach);
            }
            else
            {
                var corridor = new WorldRouteCorridor(
                    "segment-corridor", RouteKind.Sidewalk, CurvedAnchors(), width, true);
                samples = corridor.SampledNodes;
                segments = WorldRouteGeometry.Segments(corridor);
            }

            int edgeIndex = 0;
            foreach (WorldRouteSegment edge in segments)
            {
                Assert.That(edgeIndex, Is.LessThan(samples.Count - 1));
                Assert.That(edge.Start, Is.EqualTo(samples[edgeIndex]));
                Assert.That(edge.End, Is.EqualTo(samples[edgeIndex + 1]));
                Assert.That(edge.Width, Is.EqualTo(width));
                Assert.That(edge.Length, Is.EqualTo(Vector3.Distance(edge.Start, edge.End)).Within(.00001f));
                Assert.That(edge.Center, Is.EqualTo((edge.Start + edge.End) * .5f));
                Assert.That(edge.IsFinite, Is.True);
                Assert.That(edge.Length, Is.GreaterThan(0));
                edgeIndex++;
            }
            Assert.That(edgeIndex, Is.EqualTo(samples.Count - 1));
        }

        [Test]
        public void CatalogNearestPointUsesCurvedMarinaSampleRatherThanRawChord()
        {
            // Trung điểm đã tính độc lập: lệch hơn 2 m khỏi dây nối hai anchor.
            Vector3 expected = new Vector3(393.75f, 0, -125.5f);
            Assert.That(WorldRouteCatalog.TryGet(
                "expansion-marina-dock", out WorldRouteCorridor route), Is.True);
            Assert.That(DistanceToPolyline(expected, route.Nodes), Is.GreaterThan(2));

            bool found = WorldRouteCatalog.TryGetNearestPoint(
                route.Id, expected + Vector3.up * 19, .001f,
                out Vector3 nearest, out float distance);

            Assert.That(found, Is.True);
            AssertVector(nearest, expected);
            Assert.That(distance, Is.LessThan(.001f));
        }

        [Test]
        public void CatalogReservationIncludesCurvedStripOutsideTheRawAnchorStrip()
        {
            Assert.That(WorldRouteCatalog.TryGet(
                "expansion-marina-dock", out WorldRouteCorridor route), Is.True);
            Vector3 point = new Vector3(393.45f, 0, -124.5f);
            Assert.That(DistanceToPolyline(point, route.Nodes), Is.GreaterThan(route.Width * .5f),
                "This regression point must lie outside the former straight-strip reservation.");

            bool reserved = WorldRouteCatalog.IsReserved(point, 0, out string routeId);

            Assert.That(reserved, Is.True);
            Assert.That(routeId, Is.EqualTo(route.Id));
        }

        [Test]
        public void PublicApproachClassificationRecognizesIntermediateSamples()
        {
            foreach (WorldPublicApproach approach in WorldRouteCatalog.BuildPublicApproaches())
            {
                bool checkedIntermediate = false;
                for (int index = 0; index < approach.SampledNodes.Count; index++)
                {
                    Vector3 sample = approach.SampledNodes[index];
                    if (ContainsExact(approach.Nodes, sample))
                        continue;
                    checkedIntermediate = true;
                    Assert.That(WorldRouteCatalog.IsPublicApproachPoint(sample), Is.True,
                        approach.Id + " sample " + index);
                    Assert.That(WorldRouteCatalog.IsReservedByPublicApproach(
                        sample, 0, out _), Is.True, approach.Id + " sample " + index);
                }
                Assert.That(checkedIntermediate, Is.True, approach.Id);
            }
            Assert.That(WorldRouteCatalog.IsPublicApproachPoint(new Vector3(520, 0, 40)), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PublicGraphTripFollowsIntermediateIndustrialSamplesWithExactDestination(bool reverse)
        {
            WorldPublicApproach approach = FindApproach("industrial-sidewalk-approach");
            Vector3 from = reverse ? approach.Nodes[^1] : approach.Nodes[0];
            Vector3 destination = reverse ? approach.Nodes[0] : approach.Nodes[^1];
            Assert.That(Vector3.Distance(from, destination), Is.GreaterThan(CityPedestrianNetwork.LocalTrip));

            bool planned = CityPedestrianNetwork.TryPlan(
                from, destination, out List<Vector3> route, out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route[0], Is.EqualTo(from));
            Assert.That(route[^1], Is.EqualTo(destination));
            Assert.That(route.Count, Is.EqualTo(approach.SampledNodes.Count));
            bool followsIntermediateSample = false;
            for (int index = 0; index < route.Count; index++)
            {
                int sampleIndex = reverse ? approach.SampledNodes.Count - 1 - index : index;
                Assert.That(route[index], Is.EqualTo(approach.SampledNodes[sampleIndex]),
                    "Graph skipped or changed canonical sample " + sampleIndex);
                if (!ContainsExact(approach.Nodes, route[index]))
                    followsIntermediateSample = true;
            }
            Assert.That(followsIntermediateSample, Is.True);
            Assert.That(CityPedestrianNetwork.ValidateRoute(route, .38f, out string validationFailure),
                Is.True, validationFailure);
        }

        static Vector3[] CurvedAnchors()
        {
            return new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(8, 4, 8),
                new Vector3(16, 0, 0)
            };
        }

        [Test]
        public void SubdividedCrossingDoesNotBecomeAFalseClearanceOverlap()
        {
            var routes = new[]
            {
                DiagnosticRoute("cross-horizontal", new(-40, 0, 0), new(0, 0, 0), new(40, 0, 0)),
                DiagnosticRoute("cross-vertical", new(0, 0, -40), new(0, 0, 0), new(0, 0, 40))
            };
            Assert.That(WorldRouteDiagnostics.ValidateCrossRouteOverlaps(routes, out string failure),
                Is.True, failure);
        }

        [Test]
        public void SubdividedCollinearContinuationRetainsItsSharedEndpoint()
        {
            var routes = new[]
            {
                DiagnosticRoute("join-west", new(-40, 0, 0), new(-20, 0, 0), new(0, 0, 0)),
                DiagnosticRoute("join-east", new(0, 0, 0), new(20, 0, 0), new(40, 0, 0))
            };
            Assert.That(WorldRouteDiagnostics.ValidateCrossRouteOverlaps(routes, out string failure),
                Is.True, failure);
        }

        [Test]
        public void NearbyDeadEndWithoutAnActualCrossingIsStillRejected()
        {
            var routes = new[]
            {
                DiagnosticRoute("near-horizontal", new(-40, 0, 0), new(40, 0, 0)),
                DiagnosticRoute("near-vertical", new(0, 0, 6), new(0, 0, 40))
            };
            Assert.That(WorldRouteDiagnostics.ValidateCrossRouteOverlaps(routes, out string failure),
                Is.False);
            Assert.That(failure, Does.Contain("near-horizontal"));
        }

        [Test]
        public void ActualCrossingDoesNotHideRemoteParallelOverlap()
        {
            var routes = new[]
            {
                DiagnosticRoute("remote-horizontal", new(-50, 0, 0), new(0, 0, 0), new(50, 0, 0)),
                DiagnosticRoute("remote-turn", new(0, 0, -40), new(0, 0, 0), new(20, 0, 4), new(45, 0, 4))
            };
            Assert.That(WorldRouteDiagnostics.ValidateCrossRouteOverlaps(routes, out string failure),
                Is.False);
            Assert.That(failure, Does.Contain("remote-turn"));
        }

        [Test]
        public void VegetationClearanceUsesTheCurvedStripRatherThanItsChord()
        {
            var route = new WorldRouteCorridor("curve-clearance", RouteKind.Sidewalk,
                CurvedAnchors(), .1f, true);
            Vector3 point = new(3.5f, 0, 4.5f);
            Assert.That(DistanceToPolyline(point, route.Nodes), Is.GreaterThan(.5f));
            Assert.That(WorldVegetationCatalog.IsInsideCorridor(point, new[] { route }, 0), Is.True);
        }

        static WorldRouteCorridor DiagnosticRoute(string id, params Vector3[] anchors)
        {
            return new WorldRouteCorridor(id, RouteKind.Highway, anchors, 8, true);
        }

        static void AssertReadOnly(IReadOnlyList<Vector3> points)
        {
            Assert.That(points, Is.AssignableTo<IList<Vector3>>());
            var mutableInterface = (IList<Vector3>)points;
            Assert.That(mutableInterface.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableInterface[0] = Vector3.one);
            Assert.Throws<NotSupportedException>(() => mutableInterface.Add(Vector3.one));
        }

        static void AssertBoundedSamples(
            string id,
            IReadOnlyList<Vector3> anchors,
            IReadOnlyList<Vector3> samples)
        {
            int subdivisions = anchors.Count == 2 ? 1 : 4;
            Assert.That(samples.Count, Is.EqualTo((anchors.Count - 1) * subdivisions + 1), id);
            for (int index = 1; index < samples.Count; index++)
                Assert.That(Vector3.Distance(samples[index - 1], samples[index]),
                    Is.GreaterThan(.0001f), id + " has duplicate consecutive samples");
            for (int anchor = 0; anchor < anchors.Count; anchor++)
                Assert.That(samples[anchor * subdivisions], Is.EqualTo(anchors[anchor]), id);
            for (int span = 0; span < anchors.Count - 1; span++)
            {
                for (int sample = 0; sample <= subdivisions; sample++)
                {
                    Vector3 point = samples[span * subdivisions + sample];
                    for (int axis = 0; axis < 3; axis++)
                    {
                        Assert.That(float.IsNaN(point[axis]) || float.IsInfinity(point[axis]),
                            Is.False, id);
                        Assert.That(point[axis], Is.InRange(
                            Mathf.Min(anchors[span][axis], anchors[span + 1][axis]),
                            Mathf.Max(anchors[span][axis], anchors[span + 1][axis])), id);
                    }
                }
            }
        }

        static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(.0001f),
                "Expected " + expected + " but found " + actual);
        }

        static bool ContainsExact(IReadOnlyList<Vector3> points, Vector3 point)
        {
            for (int index = 0; index < points.Count; index++)
                if (points[index].Equals(point))
                    return true;
            return false;
        }

        static WorldPublicApproach FindApproach(string id)
        {
            foreach (WorldPublicApproach approach in WorldRouteCatalog.BuildPublicApproaches())
                if (approach.Id == id)
                    return approach;
            Assert.Fail("Missing approach " + id);
            return null;
        }

        static float DistanceToPolyline(Vector3 point, IReadOnlyList<Vector3> nodes)
        {
            double minimumSquared = double.PositiveInfinity;
            for (int index = 1; index < nodes.Count; index++)
            {
                double dx = nodes[index].x - nodes[index - 1].x;
                double dz = nodes[index].z - nodes[index - 1].z;
                double px = point.x - nodes[index - 1].x;
                double pz = point.z - nodes[index - 1].z;
                double projection = (px * dx + pz * dz) / (dx * dx + dz * dz);
                projection = Math.Max(0, Math.Min(1, projection));
                double offsetX = px - projection * dx;
                double offsetZ = pz - projection * dz;
                minimumSquared = Math.Min(minimumSquared,
                    offsetX * offsetX + offsetZ * offsetZ);
            }
            return (float)Math.Sqrt(minimumSquared);
        }
    }
}
