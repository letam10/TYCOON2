using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityAccessPathsTests
    {
        [Test]
        public void ValidSubdivisionsCreateExpectedNumberOfPoints()
        {
            Vector3 start = new(-4f, 0f, 0f);
            Vector3 end = new(4f, 0f, 0f);
            Vector3 control = new(0f, 4f, 0f);
            int subdivisions = 4;

            IReadOnlyList<Vector3> path = CityAccessPaths.CreateCurvedPath(
                start, end, control, subdivisions);

            Assert.That(path, Has.Count.EqualTo(subdivisions + 1));
        }

        [Test]
        public void CurvedPathPreservesStartAndEndPoints()
        {
            Vector3 start = new(-4f, 1f, 2f);
            Vector3 end = new(4f, 3f, -2f);
            Vector3 control = new(0f, 8f, 0f);

            IReadOnlyList<Vector3> path = CityAccessPaths.CreateCurvedPath(
                start, end, control, 6);

            Assert.That(path[0], Is.EqualTo(start));
            Assert.That(path[path.Count - 1], Is.EqualTo(end));
        }

        [Test]
        public void OffLineControlMovesTheMiddlePointAwayFromStraightSegment()
        {
            Vector3 start = new(-4f, 0f, 0f);
            Vector3 end = new(4f, 0f, 0f);
            Vector3 control = new(0f, 4f, 0f);
            IReadOnlyList<Vector3> path = CityAccessPaths.CreateCurvedPath(
                start, end, control, 4);
            Vector3 straightMiddle = Vector3.Lerp(start, end, .5f);

            Assert.That(Vector3.Distance(path[2], straightMiddle), Is.GreaterThan(.001f));
        }

        [Test]
        public void GeneratedPointsRemainFinite()
        {
            IReadOnlyList<Vector3> path = CityAccessPaths.CreateCurvedPath(
                new Vector3(-4f, 1f, 2f),
                new Vector3(4f, 3f, -2f),
                new Vector3(0f, 8f, 0f),
                8);

            foreach (Vector3 point in path)
                Assert.That(IsFinite(point), Is.True, "Path contains a non-finite point: " + point);
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void SubdivisionsBelowOneReturnOnlyEndpoints(int subdivisions)
        {
            // Hợp đồng tối giản: subdivisions < 1 được chặn về một đoạn gồm hai đầu mút.
            Vector3 start = new(-4f, 1f, 2f);
            Vector3 end = new(4f, 3f, -2f);

            IReadOnlyList<Vector3> path = CityAccessPaths.CreateCurvedPath(
                start, end, new Vector3(0f, 8f, 0f), subdivisions);

            Assert.That(path, Has.Count.EqualTo(2));
            Assert.That(path[0], Is.EqualTo(start));
            Assert.That(path[1], Is.EqualTo(end));
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
