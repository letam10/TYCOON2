using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityPedestrianFarmRestrictionTests
    {
        [Test]
        public void ShortCornerCrossingIsDetectedWithoutSamplingGap()
        {
            Vector3 start = new(-93.51f, 0, -28.49f);
            Vector3 end = new(-92f, 0, -28.59f);

            Assert.That(CityPedestrianNetwork.SegmentIntersectsFarm(start, end), Is.True);
        }

        [Test]
        public void SegmentOutsideFarmRemainsAllowed()
        {
            Vector3 start = new(-100f, 0, -40f);
            Vector3 end = new(-95f, 0, -35f);

            Assert.That(CityPedestrianNetwork.SegmentIntersectsFarm(start, end), Is.False);
        }
    }
}
