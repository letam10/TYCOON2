using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Phase2PedestrianNetworkTests
    {
        [Test]
        public void ShortTripRejectsDirectSegmentThroughHighwayCorridor()
        {
            Vector3 from = new(-188, 0, -100);
            Vector3 destination = new(-202, 0, -100);

            Assert.That(Vector3.Distance(from, destination), Is.LessThanOrEqualTo(
                CityPedestrianNetwork.LocalTrip));
            bool planned = CityPedestrianNetwork.TryPlan(
                from, destination, out List<Vector3> route, out string failure);

            Assert.That(planned, Is.False);
            Assert.That(route, Is.Empty);
            Assert.That(failure, Does.Contain("highway-ring"));
        }

        [Test]
        public void ShortTripKeepsDirectDestinationWhenSegmentIsClear()
        {
            Vector3 from = new(-57, 0, -13);
            Vector3 destination = new(-55, 0, -14);

            bool planned = CityPedestrianNetwork.TryPlan(
                from, destination, out List<Vector3> route, out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(route, Is.EqualTo(new[] { destination }));
        }
    }
}
