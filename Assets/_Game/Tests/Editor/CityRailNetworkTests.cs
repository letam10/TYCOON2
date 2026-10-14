using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityRailNetworkTests
    {
        [Test]
        public void TwoRailwaysAndBothPlatformsRemainInsideExistingWorld()
        {
            Assert.That(CityRailNetwork.Tracks.Length, Is.EqualTo(2));
            Assert.That(CityRailNetwork.Platforms.Length, Is.EqualTo(2));
            Assert.That(Mathf.Abs(CityRailNetwork.Tracks[0] - CityRailNetwork.Tracks[1]), Is.GreaterThan(5));
            foreach (float z in CityRailNetwork.Platforms)
            {
                Assert.That(z - 1.5f, Is.GreaterThan(CityDistricts.WorldBounds.min.z));
                Assert.That(z + 1.5f, Is.LessThan(CityDistricts.WorldBounds.max.z));
            }
            Assert.That(CityRailNetwork.Corridor.min.x, Is.GreaterThan(CityDistricts.WorldBounds.min.x));
            Assert.That(CityRailNetwork.Corridor.max.x, Is.LessThan(CityDistricts.WorldBounds.max.x));
        }

        [TestCase(0)]
        [TestCase(.1f)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public void BrakingSpeedNeverExceedsLimitOrStoppingDistance(float distance)
        {
            float speed = CityRailNetwork.SafeSpeed(distance, 9, 2.8f);
            Assert.That(speed, Is.InRange(0, 9));
            Assert.That(speed * speed / (2 * 2.8f), Is.LessThanOrEqualTo(distance + .001f));
        }

        [Test]
        public void CurvedRoadRoutePreservesStraightBusStopsAndStaysInItsFootprint()
        {
            var lane = CityPublicTransportVehicle.Lane;
            var rounded = CityRoadRoute.Round(lane);
            foreach (int stop in new[] { 0, 1, 4, 5 }) Assert.That(rounded.Contains(lane[stop]), Is.True);
            var bounds = new Bounds(lane[0], Vector3.zero);
            foreach (var point in lane) bounds.Encapsulate(point);
            bounds.Expand(.001f);
            Assert.That(rounded.All(bounds.Contains), Is.True);
        }

        [Test]
        public void TrafficSpawnPositionsAreSeparatedOnEachLane()
        {
            Vector3[] corners = { new(-105, 0, -45), new(102, 0, -45), new(102, 0, 29), new(-105, 0, 29) };
            var route = CityRoadRoute.Round(corners);
            var points = Enumerable.Range(0, 3).Select(x => CityRoadRoute.Spawn(route, x / 3f, out _)).ToArray();
            for (int first = 0; first < points.Length; first++)
                for (int second = first + 1; second < points.Length; second++)
                    Assert.That(Vector3.Distance(points[first], points[second]), Is.GreaterThan(30));
        }
    }
}
