using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityTrafficTests
    {
        static readonly Vector3[] Route =
        {
            new(0, 0, 0), new(0, 0, 20), new(20, 0, 20), new(20, 0, 0)
        };

        [TestCase(0, .25f, true)]
        [TestCase(0, 4.9f, true)]
        [TestCase(1.6f, 3, true)]
        [TestCase(1.8f, 3, false)]
        [TestCase(0, -.1f, false)]
        [TestCase(0, 5.1f, false)]
        public void CrossingActorBlocksOnlyForwardCorridor(float x, float z, bool expected)
        {
            Assert.That(CityTrafficSafety.ActorAhead(Vector3.zero, Vector3.forward, .1f,
                new Vector3(x, 0, z)), Is.EqualTo(expected));
        }

        [Test]
        public void ActorGeometryUsesWorldDirectionAndIgnoresGroundHeight()
        {
            var position = new Vector3(-50, 3, 27);
            Assert.That(CityTrafficSafety.ActorAhead(position, Vector3.left, .1f,
                new Vector3(-53, 0, 28)), Is.True);
            Assert.That(CityTrafficSafety.ActorAhead(position, Vector3.left, .1f,
                new Vector3(-48, 0, 27)), Is.False);
            Assert.That(CityTrafficSafety.ActorAhead(position, Vector3.zero, .1f, position), Is.False);
        }

        [Test]
        public void LargeMovementStepStillSeesActorBeyondNormalLookAhead()
        {
            Assert.That(CityTrafficSafety.ActorAhead(Vector3.zero, Vector3.forward, 8,
                new Vector3(0, 0, 9)), Is.True);
        }

        [Test]
        public void FollowingCarYieldsButLeadingCarKeepsMoving()
        {
            var follower = new Vector3(0, 0, 5);
            var leader = new Vector3(0, 0, 10);
            Assert.That(CityTrafficSafety.CarAhead(follower, Route, 1, 0, leader, 1, 0, .1f), Is.True);
            Assert.That(CityTrafficSafety.CarAhead(leader, Route, 1, 0, follower, 1, 0, .1f), Is.False);
        }

        [Test]
        public void OppositeLaneDoesNotBlockEvenWhenCarsAreNearby()
        {
            var incoming = new Vector3(3.1f, 0, 4);
            Assert.That(CityTrafficSafety.CarAhead(Vector3.zero, Route, 1, 0, incoming, 0, 1, .1f), Is.False);
            Assert.That(CityTrafficSafety.CarAhead(incoming, Route, 0, 1, Vector3.zero, 1, 0, .1f), Is.False);
        }

        [Test]
        public void FollowerKeepsGapWhileLeaderTurnsCorner()
        {
            var follower = new Vector3(0, 0, 17);
            var leader = new Vector3(2, 0, 20);
            Assert.That(CityTrafficSafety.CarAhead(follower, Route, 1, 0, leader, 2, 0, .1f), Is.True);
            Assert.That(CityTrafficSafety.CarAhead(leader, Route, 2, 0, follower, 1, 0, .1f), Is.False);
            Assert.That(CityTrafficSafety.CarAhead(follower, Route, 1, 0,
                new Vector3(4, 0, 20), 2, 0, .1f), Is.False);
        }

        [Test]
        public void FollowingDistanceWrapsAcrossFinalRouteWaypoint()
        {
            var follower = new Vector3(2, 0, 0);
            var leader = new Vector3(0, 0, 2);
            Assert.That(CityTrafficSafety.CarAhead(follower, Route, 0, 0, leader, 1, 0, .1f), Is.True);
            Assert.That(CityTrafficSafety.CarAhead(leader, Route, 1, 0, follower, 0, 0, .1f), Is.False);
        }
    }
}
