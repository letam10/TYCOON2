using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon
{
    [TestFixture]
    public sealed class CityAccessPathsSmoothTests
    {
        [Test]
        public void ThreeAnchorsProduceMoreThanThreeSamplesAndPreserveEndpoints()
        {
            var anchors = new List<Vector3>
            {
                new(-4f, 0f, 0f),
                new(0f, 3f, 0f),
                new(4f, 0f, 0f)
            };

            IReadOnlyList<Vector3> path = CityAccessPaths.CreateSmoothPath(anchors, 4);

            Assert.That(path, Is.Not.Null);
            Assert.That(path.Count, Is.GreaterThan(3));
            Assert.That(path[0], Is.EqualTo(anchors[0]));
            Assert.That(path[path.Count - 1], Is.EqualTo(anchors[anchors.Count - 1]));
        }

        [Test]
        public void OffAxisAnchorBendsPathAwayFromStraightSegment()
        {
            var anchors = new List<Vector3>
            {
                new(-4f, 0f, 0f),
                new(0f, 4f, 0f),
                new(4f, 0f, 0f)
            };

            IReadOnlyList<Vector3> path = CityAccessPaths.CreateSmoothPath(anchors, 4);
            Vector3 start = anchors[0];
            Vector3 end = anchors[anchors.Count - 1];
            Vector3 segment = end - start;
            bool hasOffLineSample = false;

            for (int index = 1; index < path.Count - 1; index++)
            {
                Vector3 point = path[index];
                float projectionFactor = Vector3.Dot(point - start, segment) / segment.sqrMagnitude;
                Vector3 projection = start + projectionFactor * segment;
                if ((point - projection).sqrMagnitude > 0.000001f)
                {
                    hasOffLineSample = true;
                    break;
                }
            }

            Assert.That(hasOffLineSample, Is.True);
        }

        [Test]
        public void DegenerateAnchorsAndNonPositiveSubdivisionsReturnFiniteSamples()
        {
            var anchorCases = new List<Vector3>[]
            {
                null,
                new List<Vector3>(),
                new List<Vector3> { new(2f, 3f, 4f) }
            };
            int[] subdivisionCases = { 0, -2 };

            for (int anchorCase = 0; anchorCase < anchorCases.Length; anchorCase++)
            {
                for (int subdivisionCase = 0; subdivisionCase < subdivisionCases.Length; subdivisionCase++)
                {
                    IReadOnlyList<Vector3> path = null;
                    int subdivisions = subdivisionCases[subdivisionCase];
                    Assert.DoesNotThrow(
                        () => path = CityAccessPaths.CreateSmoothPath(
                            anchorCases[anchorCase], subdivisions));
                    Assert.That(path, Is.Not.Null);
                    AssertAllPointsFinite(path);
                }
            }
        }

        [Test]
        public void SamplesAreFiniteAndAdjacentSamplesAreDistinct()
        {
            var anchors = new List<Vector3>
            {
                new(-5f, 0f, -1f),
                new(-1f, 2f, 3f),
                new(3f, 5f, 0f),
                new(6f, 1f, 4f)
            };

            IReadOnlyList<Vector3> path = CityAccessPaths.CreateSmoothPath(anchors, 5);

            AssertAllPointsFinite(path);
            for (int index = 1; index < path.Count; index++)
            {
                float distanceSquared = (path[index] - path[index - 1]).sqrMagnitude;
                Assert.That(distanceSquared, Is.GreaterThan(0.000001f),
                    "Adjacent samples overlap at index " + index + ".");
            }
        }

        [Test]
        public void SamplesStayInsideEachAnchorSpanBounds()
        {
            var anchors = new List<Vector3>
            {
                new(0f, 0f, 0f),
                new(1f, 0f, 0f),
                new(1f, 0f, 1f)
            };

            IReadOnlyList<Vector3> path = CityAccessPaths.CreateSmoothPath(anchors, 8);

            for (int span = 0; span < anchors.Count - 1; span++)
            {
                Vector3 start = anchors[span];
                Vector3 end = anchors[span + 1];
                int first = span * 8;
                int last = first + 8;
                for (int index = first; index <= last; index++)
                {
                    Vector3 point = path[index];
                    Assert.That(point.x, Is.InRange(Mathf.Min(start.x, end.x),
                        Mathf.Max(start.x, end.x)));
                    Assert.That(point.y, Is.InRange(Mathf.Min(start.y, end.y),
                        Mathf.Max(start.y, end.y)));
                    Assert.That(point.z, Is.InRange(Mathf.Min(start.z, end.z),
                        Mathf.Max(start.z, end.z)));
                }
            }
        }

        static void AssertAllPointsFinite(IReadOnlyList<Vector3> path)
        {
            for (int index = 0; index < path.Count; index++)
            {
                Vector3 point = path[index];
                Assert.That(IsFinite(point), Is.True,
                    "Path contains a non-finite sample at index " + index + ": " + point);
            }
        }

        static bool IsFinite(Vector3 point)
        {
            return IsFinite(point.x) && IsFinite(point.y) && IsFinite(point.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
