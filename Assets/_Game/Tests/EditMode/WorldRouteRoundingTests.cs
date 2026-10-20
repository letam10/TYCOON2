using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldRouteRoundingTests
    {
        const float PositionTolerance = .0001f;

        [TestCase(1f)]
        [TestCase(-1f)]
        public void RightAngleHasIndependentCenterTangentsRadiusAndSignedMidpoint(float direction)
        {
            Vector3[] anchors = { new(-10, 7, 0), new(0, 7, 0), new(0, 7, direction * 10) };
            IReadOnlyList<WorldRouteCorner> corners = WorldRouteRounding.Corners(anchors, 2);

            Assert.That(corners, Has.Count.EqualTo(1));
            WorldRouteCorner corner = corners[0];
            Assert.That(corner.AnchorIndex, Is.EqualTo(1));
            Assert.That(corner.Anchor, Is.EqualTo(anchors[1]));
            AssertVector(corner.Entry, new(-2, 7, 0));
            AssertVector(corner.Exit, new(0, 7, direction * 2));
            AssertVector(corner.Center, new(-2, 7, direction * 2));
            Assert.That(corner.Radius, Is.EqualTo(2).Within(.00001));
            Assert.That(corner.SweepRadians, Is.EqualTo(direction * Math.PI / 2).Within(.00001));
            float diagonal = (float)Math.Sqrt(2);
            AssertVector(corner.Evaluate(.5f), new(-2 + diagonal, 7, direction * (2 - diagonal)));
            Assert.That(corner.Evaluate(0), Is.EqualTo(corner.Entry));
            Assert.That(corner.Evaluate(1), Is.EqualTo(corner.Exit));
            Assert.That(Math.Abs(Vector3.Dot(corner.Entry - corner.Center, Vector3.right)),
                Is.LessThan(.00001));
            Assert.That(Math.Abs(Vector3.Dot(corner.Exit - corner.Center,
                new Vector3(0, 0, direction))), Is.LessThan(.00001));
        }

        [TestCase(30f)]
        [TestCase(90f)]
        [TestCase(150f)]
        public void ArbitraryTurnUsesTheAnalyticHalfAngleAndHalfEdgeLimit(float degrees)
        {
            double theta = degrees * Math.PI / 180;
            Vector3 outgoing = new((float)Math.Cos(theta), 0, (float)Math.Sin(theta));
            Vector3[] anchors = { new(-20, 0, 0), Vector3.zero, outgoing * 20 };
            WorldRouteCorner corner = WorldRouteRounding.Corners(anchors, 3)[0];
            double trim = Math.Min(3 * Math.Tan(theta / 2), 10);
            double radius = trim / Math.Tan(theta / 2);

            AssertVector(corner.Entry, new(-(float)trim, 0, 0));
            AssertVector(corner.Exit, outgoing * (float)trim);
            AssertVector(corner.Center, new(-(float)trim, 0, (float)radius));
            Assert.That(corner.Radius, Is.EqualTo(radius).Within(.0001));
            Assert.That(corner.SweepRadians, Is.EqualTo(theta).Within(.0001));
            Assert.That(Vector3.Distance(corner.Entry, anchors[1]), Is.LessThanOrEqualTo(10.0001f));
            Assert.That(Vector3.Distance(corner.Exit, anchors[1]), Is.LessThanOrEqualTo(10.0001f));
            foreach (float amount in new[] { 0f, .13f, .5f, .87f, 1f })
            {
                Vector3 point = corner.Evaluate(amount);
                Assert.That(Vector3.Distance(point, corner.Center), Is.EqualTo(radius).Within(.0001));
                AssertInsideTriangle(point, anchors[0], anchors[1], anchors[2]);
            }
        }

        [Test]
        public void ShortSpanShrinksRadiusAndAdjacentTrimsMeetWithoutDuplicateSamples()
        {
            Vector3[] anchors = { new(-2, 0, 0), Vector3.zero, new(0, 0, 2), new(2, 0, 2) };
            IReadOnlyList<WorldRouteCorner> corners = WorldRouteRounding.Corners(anchors, 100);
            Assert.That(corners, Has.Count.EqualTo(2));
            Assert.That(corners[0].Radius, Is.EqualTo(1).Within(.00001));
            Assert.That(corners[1].Radius, Is.EqualTo(1).Within(.00001));
            AssertVector(corners[0].Entry, new(-1, 0, 0));
            AssertVector(corners[0].Exit, new(0, 0, 1));
            Assert.That(corners[1].Entry, Is.EqualTo(corners[0].Exit));
            AssertVector(corners[1].Exit, new(1, 0, 2));

            IReadOnlyList<Vector3> samples = WorldRouteRounding.Sample(anchors, 100);
            Assert.That(samples[0], Is.EqualTo(anchors[0]));
            Assert.That(samples[^1], Is.EqualTo(anchors[^1]));
            AssertFiniteNonzeroEdges(samples);
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void ArcSamplesAreCircularContainTheirMidpointAndMeetTwentyMillimetreChordError(float direction)
        {
            Vector3[] anchors = { new(-40, 0, 0), Vector3.zero, new(0, 0, direction * 40) };
            IReadOnlyList<Vector3> samples = WorldRouteRounding.Sample(anchors, 8);
            int first = ExactIndex(samples, new Vector3(-8, 0, 0));
            int last = ExactIndex(samples, new Vector3(0, 0, direction * 8));
            Assert.That(first, Is.GreaterThanOrEqualTo(1));
            Assert.That(last, Is.GreaterThan(first));
            int segmentCount = last - first;
            Assert.That(segmentCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(segmentCount % 2, Is.EqualTo(0));
            Vector3 center = new(-8, 0, direction * 8);
            float diagonal = (float)(8 / Math.Sqrt(2));
            AssertVector(samples[first + segmentCount / 2],
                new(-8 + diagonal, 0, direction * (8 - diagonal)));
            Assert.That(WorldRouteRounding.MaximumChordError, Is.EqualTo(.02f));

            for (int index = first; index <= last; index++)
            {
                Assert.That(Vector3.Distance(samples[index], center), Is.EqualTo(8).Within(.0001));
                AssertInsideTriangle(samples[index], anchors[0], anchors[1], anchors[2]);
                if (index == first)
                    continue;
                double chord = Vector3.Distance(samples[index - 1], samples[index]);
                double sagitta = 8 - Math.Sqrt(64 - chord * chord / 4);
                Assert.That(sagitta, Is.LessThanOrEqualTo(.02001), "Chord " + index);
            }
            AssertFiniteNonzeroEdges(samples);
        }

        [Test]
        public void ClosedLoopRoundsAllFourCornersIncludingItsClosingAnchor()
        {
            Vector3[] anchors = Square();
            IReadOnlyList<WorldRouteCorner> corners = WorldRouteRounding.Corners(anchors, 3);
            IReadOnlyList<Vector3> samples = WorldRouteRounding.Sample(anchors, 3);
            Assert.That(corners, Has.Count.EqualTo(4));
            Assert.That(samples[0], Is.EqualTo(new Vector3(0, 0, 3)));
            Assert.That(samples[^1], Is.EqualTo(samples[0]));
            Assert.That(samples[0], Is.Not.EqualTo(anchors[0]));
            for (int index = 0; index < 4; index++)
            {
                Assert.That(corners[index].AnchorIndex, Is.EqualTo(index));
                Assert.That(ExactIndex(samples, corners[index].Entry), Is.GreaterThanOrEqualTo(0));
                Assert.That(ExactIndex(samples, corners[index].Exit), Is.GreaterThanOrEqualTo(0));
                Assert.That(ExactIndex(samples, corners[index].Evaluate(.5f)), Is.GreaterThanOrEqualTo(0));
                Assert.That(ExactIndex(samples, anchors[index]), Is.EqualTo(-1));
            }
            AssertFiniteNonzeroEdges(samples);
        }

        [Test]
        public void AdjacentSectionsAreExactCyclicSubsequencesAndRecombineWithoutNewEdges()
        {
            Vector3[] anchors = Square();
            IReadOnlyList<Vector3> full = WorldRouteRounding.Sample(anchors, 3);
            var combined = new List<Vector3>();
            for (int corner = 0; corner < 4; corner++)
            {
                IReadOnlyList<Vector3> section = WorldRouteRounding.Section(anchors, 3, corner, (corner + 1) % 4);
                Assert.That(section.Count, Is.GreaterThan(2));
                int offset = ExactIndex(full, section[0]);
                Assert.That(offset, Is.GreaterThanOrEqualTo(0));
                for (int index = 0; index < section.Count; index++)
                    Assert.That(section[index], Is.EqualTo(full[(offset + index) % (full.Count - 1)]));
                if (corner == 0)
                {
                    float diagonal = (float)(3 / Math.Sqrt(2));
                    AssertVector(section[0], new(3 - diagonal, 0, 3 - diagonal));
                    combined.Add(section[0]);
                }
                else
                {
                    Assert.That(combined[^1], Is.EqualTo(section[0]));
                }
                for (int index = 1; index < section.Count; index++)
                    combined.Add(section[index]);
                AssertReadOnly(section);
            }
            Assert.That(combined.Count, Is.EqualTo(full.Count));
            Assert.That(combined[^1], Is.EqualTo(combined[0]));
            int start = ExactIndex(full, combined[0]);
            for (int index = 0; index < combined.Count - 1; index++)
                Assert.That(combined[index], Is.EqualTo(full[(start + index) % (full.Count - 1)]));
            AssertFiniteNonzeroEdges(combined);
        }

        [Test]
        public void SectionRejectsOpenNonadjacentAndOutOfRangeCorners()
        {
            Vector3[] open = { new(-10, 0, 0), Vector3.zero, new(0, 0, 10) };
            Assert.That(() => WorldRouteRounding.Section(open, 2, 0, 1),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(() => WorldRouteRounding.Section(Square(), 2, 0, 2),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(() => WorldRouteRounding.Section(Square(), 2, 0, 0),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(() => WorldRouteRounding.Section(Square(), 2, -1, 0),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(() => WorldRouteRounding.Section(Square(), 2, 3, 4),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void CollinearAnchorsAndReversalRemainUnroundedAndFinite()
        {
            Vector3[] straight = { new(-10, 0, 0), Vector3.zero, new(10, 0, 0) };
            Assert.That(WorldRouteRounding.Corners(straight, 4), Is.Empty);
            CollectionAssert.AreEqual(straight, WorldRouteRounding.Sample(straight, 4));
            Vector3[] reversal = { new(-10, 0, 0), Vector3.zero, new(-5, 0, 0), new(-5, 0, 6) };
            IReadOnlyList<WorldRouteCorner> corners = WorldRouteRounding.Corners(reversal, 2);
            Assert.That(corners, Has.Count.EqualTo(1));
            Assert.That(corners[0].AnchorIndex, Is.EqualTo(2));
            IReadOnlyList<Vector3> samples = WorldRouteRounding.Sample(reversal, 2);
            Assert.That(ExactIndex(samples, Vector3.zero), Is.GreaterThanOrEqualTo(0));
            Assert.That(samples[0], Is.EqualTo(reversal[0]));
            Assert.That(samples[^1], Is.EqualTo(reversal[^1]));
            AssertFiniteNonzeroEdges(samples);
        }

        [Test]
        public void GradedInputRetainsItsIndependentLegacyHermiteCoordinates()
        {
            Vector3[] anchors = { Vector3.zero, new(8, 4, 8), new(16, 0, 0) };
            IReadOnlyList<Vector3> samples = WorldRouteRounding.Sample(anchors, 3);
            Assert.That(WorldRouteRounding.Corners(anchors, 3), Is.Empty);
            Assert.That(samples, Has.Count.EqualTo(9));
            AssertVector(samples[1], new(1.4375f, .90625f, 1.8125f));
            AssertVector(samples[2], new(3.5f, 2.25f, 4.5f));
            AssertVector(samples[3], new(5.8125f, 3.46875f, 6.9375f));
            AssertVector(samples[4], anchors[1]);
            AssertVector(samples[6], new(12.5f, 2.25f, 4.5f));
            AssertFiniteNonzeroEdges(samples);
        }

        [Test]
        public void DuplicateAuthoredAnchorsRemainVisibleWithoutCreatingZeroLengthSampleEdges()
        {
            Vector3[] anchors = { new(-10, 0, 0), Vector3.zero, Vector3.zero, new(0, 0, 10) };
            var route = new WorldRouteCorridor("duplicate-authored-corner", RouteKind.Highway, anchors, 4, true);

            CollectionAssert.AreEqual(anchors, route.Nodes);
            Assert.That(route.Nodes[1], Is.EqualTo(route.Nodes[2]), "Raw validation must still see the duplicate.");
            Assert.That(route.SampledNodes[0], Is.EqualTo(anchors[0]));
            Assert.That(route.SampledNodes[^1], Is.EqualTo(anchors[^1]));
            AssertFiniteNonzeroEdges(route.SampledNodes);
        }

        [Test]
        public void SamplesAndCornerRecordsCopyInputAndExposeOnlyReadOnlyCollections()
        {
            Vector3[] anchors = { new(-10, 0, 0), Vector3.zero, new(0, 0, 10) };
            IReadOnlyList<Vector3> samples = WorldRouteRounding.Sample(anchors, 2);
            IReadOnlyList<WorldRouteCorner> corners = WorldRouteRounding.Corners(anchors, 2);
            var expected = new List<Vector3>(samples);
            for (int index = 0; index < anchors.Length; index++)
                anchors[index] = Vector3.one * 999;

            CollectionAssert.AreEqual(expected, samples);
            Assert.That(corners[0].Anchor, Is.EqualTo(Vector3.zero));
            AssertVector(corners[0].Entry, new(-2, 0, 0));
            AssertReadOnly(samples);
            if (corners is IList<WorldRouteCorner> mutable)
            {
                Assert.That(mutable.IsReadOnly, Is.True);
                Assert.Throws<NotSupportedException>(() => mutable[0] = default);
            }
        }

        [Test]
        public void RingKeepsItsAuthoredIdsWidthsAnchorsAndFourExactSharedSections()
        {
            Assert.That(WorldRouteCatalog.TryGet("highway-ring", out WorldRouteCorridor ring), Is.True);
            Vector3[] rawRing =
            {
                new(-195, 0, -112), new(-195, 0, 155), new(190, 0, 155),
                new(190, 0, -112), new(-195, 0, -112)
            };
            CollectionAssert.AreEqual(rawRing, ring.Nodes);
            Assert.That(ring.Width, Is.EqualTo(8));
            Assert.That(ring.Kind, Is.EqualTo(RouteKind.Highway));
            Assert.That(ring.ReservesPlacement, Is.True);
            string[] ids = { "street-ring-west", "street-ring-north", "street-ring-east", "street-ring-south" };
            var expectedRaw = new[]
            {
                new[] { rawRing[0], rawRing[1] }, new[] { rawRing[1], rawRing[2] },
                new[] { rawRing[3], rawRing[2] }, new[] { rawRing[0], rawRing[3] }
            };
            var combined = new List<Vector3>();
            for (int corner = 0; corner < ids.Length; corner++)
            {
                WorldRouteCorridor street = FindStreet(ids[corner]);
                CollectionAssert.AreEqual(expectedRaw[corner], street.Nodes);
                Assert.That(street.Width, Is.EqualTo(8));
                Assert.That(street.Kind, Is.EqualTo(RouteKind.Highway));
                Assert.That(street.ReservesPlacement, Is.True);
                Assert.That(street.SampledNodes.Count, Is.GreaterThan(2));
                bool reverse = corner >= 2;
                if (corner == 0)
                    combined.Add(street.SampledNodes[0]);
                Vector3 first = street.SampledNodes[reverse ? street.SampledNodes.Count - 1 : 0];
                Assert.That(first, Is.EqualTo(combined[^1]));
                for (int index = 1; index < street.SampledNodes.Count; index++)
                    combined.Add(street.SampledNodes[reverse ? street.SampledNodes.Count - 1 - index : index]);
            }
            Assert.That(combined.Count, Is.EqualTo(ring.SampledNodes.Count));
            Assert.That(combined[^1], Is.EqualTo(combined[0]));
            int offset = ExactIndex(ring.SampledNodes, combined[0]);
            Assert.That(offset, Is.GreaterThanOrEqualTo(0));
            for (int index = 0; index < combined.Count - 1; index++)
                Assert.That(combined[index], Is.EqualTo(ring.SampledNodes[(offset + index) % (ring.SampledNodes.Count - 1)]));
            AssertFiniteNonzeroEdges(combined);
        }

        static Vector3[] Square()
        {
            return new[] { Vector3.zero, new Vector3(20, 0, 0), new Vector3(20, 0, 20),
                new Vector3(0, 0, 20), Vector3.zero };
        }

        static WorldRouteCorridor FindStreet(string id)
        {
            foreach (WorldRouteCorridor route in WorldRouteCatalog.BuildStreetNetwork())
                if (route.Id == id)
                    return route;
            Assert.Fail("Missing street " + id);
            return null;
        }

        static int ExactIndex(IReadOnlyList<Vector3> points, Vector3 expected)
        {
            for (int index = 0; index < points.Count; index++)
                if (points[index].Equals(expected))
                    return index;
            return -1;
        }

        static void AssertReadOnly(IReadOnlyList<Vector3> points)
        {
            if (!(points is IList<Vector3> mutable))
                return;
            Assert.That(mutable.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutable[0] = Vector3.one);
            Assert.Throws<NotSupportedException>(() => mutable.Add(Vector3.one));
        }

        static void AssertFiniteNonzeroEdges(IReadOnlyList<Vector3> points)
        {
            Assert.That(points.Count, Is.GreaterThanOrEqualTo(2));
            for (int index = 0; index < points.Count; index++)
            {
                for (int axis = 0; axis < 3; axis++)
                    Assert.That(float.IsNaN(points[index][axis]) || float.IsInfinity(points[index][axis]), Is.False);
                if (index > 0)
                    Assert.That((points[index] - points[index - 1]).sqrMagnitude, Is.GreaterThan(.00000001f));
            }
        }

        static void AssertInsideTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            double first = Cross(b - a, point - a);
            double second = Cross(c - b, point - b);
            double third = Cross(a - c, point - c);
            bool positive = first >= -.0001 && second >= -.0001 && third >= -.0001;
            bool negative = first <= .0001 && second <= .0001 && third <= .0001;
            Assert.That(positive || negative, Is.True, "Sample outside local anchor triangle: " + point);
        }

        static double Cross(Vector3 a, Vector3 b)
        {
            return (double)a.x * b.z - (double)a.z * b.x;
        }

        static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(PositionTolerance),
                "Expected " + expected + " but found " + actual);
        }
    }
}
