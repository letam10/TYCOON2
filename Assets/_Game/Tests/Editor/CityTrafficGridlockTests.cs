using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityTrafficGridlockTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void MixedFollowerYieldsAroundCornerWithoutStoppingLeader(bool reverse)
        {
            Vector3 follower = new(-106.55f, 0, -40.55f);
            Vector3 leader = new(-102.55f, 0, -46.55f);
            Vector3 followerForward = Vector3.back;
            Vector3 leaderForward = Vector3.right;
            if (reverse)
            {
                (follower, leader) = (leader, follower);
                (followerForward, leaderForward) = (-leaderForward, -followerForward);
            }
            Assert.That(CityTrafficSafety.VehicleAhead(follower, followerForward, .1f,
                leader, leaderForward), Is.True);
            Assert.That(CityTrafficSafety.VehicleAhead(leader, leaderForward, .1f,
                follower, followerForward), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OppositeStraightLanesContinueAfterLeavingCorner(bool reverse)
        {
            Vector3 a = new(-106.55f, 0, -20);
            Vector3 b = new(-103.45f, 0, -24);
            Assert.That(CityTrafficSafety.VehicleAhead(a, reverse ? Vector3.forward : Vector3.back, .1f,
                b, reverse ? Vector3.back : Vector3.forward), Is.False);
        }

        [TestCase(0, false)]
        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        [TestCase(3, true)]
        public void OppositeLongVehiclesClearEveryCornerInEitherArrivalOrder(int corner, bool innerFirst)
        {
            Vector3 center = corner switch
            {
                0 => new(-105, 0, -45),
                1 => new(102, 0, -45),
                2 => new(102, 0, 29),
                _ => new(-105, 0, 29)
            };
            Quaternion turn = Quaternion.Euler(0, -90 * corner, 0);
            var inner = new Traveller(Path(1.55f, false, center, turn), innerFirst ? 6 : 0);
            var outer = new Traveller(Path(-1.55f, true, center, turn), innerFirst ? 0 : 6);
            bool waited = false;
            for (int tick = 0; tick < 2000 && (!inner.Finished || !outer.Finished); tick++)
            {
                waited |= Move(inner, outer);
                waited |= Move(outer, inner);
                Assert.That(Separation(inner, outer), Is.GreaterThan(.02f),
                    "The full 8.6 m hulls must stay separated throughout the turn.");
            }
            Assert.That(waited, Is.True, "The conflicting corner must require a real yield.");
            Assert.That(inner.Finished && outer.Finished, Is.True, "Both directions must clear the corner.");
        }

        [Test]
        public void CornerReservationStartsBeforeTheRecordedCoachTankerDeadlock()
        {
            Vector3 waitingCoach = new(-106.55f, 0, -32.85f);
            Vector3 tanker = new(-103.09f, 0, -41.25f);
            Vector3 tankerForward = new(-.4f, 0, .916515f);
            Assert.That(CityTrafficSafety.VehicleAhead(waitingCoach, Vector3.back, .1f,
                tanker, tankerForward), Is.True);
            Assert.That(CityTrafficSafety.VehicleAhead(tanker, tankerForward, .1f,
                waitingCoach, Vector3.back), Is.False);
            // Không cho xe tiến tới vị trí kẹt cũ: đuôi dài cần khoảng trống để quay hết góc.
            Assert.That(CityTrafficSafety.ActorAhead(waitingCoach, Vector3.back, .1f,
                waitingCoach + Vector3.back * 3), Is.True);
        }

        [Test]
        public void VehiclesAlreadyInsideCornerHaveOneStableWinner()
        {
            Vector3 outer = new(-106.55f, 0, -33);
            Vector3 inner = new(-93, 0, -43.45f);
            Assert.That(CityTrafficSafety.VehicleAhead(outer, Vector3.back, .1f,
                inner, Vector3.left), Is.True);
            Assert.That(CityTrafficSafety.VehicleAhead(inner, Vector3.left, .1f,
                outer, Vector3.back), Is.False);
            inner += Vector3.left;
            Assert.That(CityTrafficSafety.VehicleAhead(outer, Vector3.back, .1f,
                inner, Vector3.left), Is.True);
            Assert.That(CityTrafficSafety.VehicleAhead(inner, Vector3.left, .1f,
                outer, Vector3.back), Is.False);
        }

        [Test]
        public void IdenticalCornerProgressHasDeterministicOuterLanePriority()
        {
            Vector3 outer = new(-106.55f, 0, -34.55f);
            Vector3 inner = new(-91.45f, 0, -43.45f);
            Assert.That(CityTrafficSafety.VehicleAhead(outer, Vector3.back, .1f,
                inner, Vector3.left), Is.False);
            Assert.That(CityTrafficSafety.VehicleAhead(inner, Vector3.left, .1f,
                outer, Vector3.back), Is.True);
        }

        [TestCase(0, false)]
        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        [TestCase(3, true)]
        public void SameLaneLongHullsStaySeparatedAroundEveryCorner(int corner, bool reverse)
        {
            Vector3 center = corner switch
            {
                0 => new(-105, 0, -45),
                1 => new(102, 0, -45),
                2 => new(102, 0, 29),
                _ => new(-105, 0, 29)
            };
            var path = Path(reverse ? -1.55f : 1.55f, reverse, center,
                Quaternion.Euler(0, -90 * corner, 0));
            var follower = new Traveller(path, 0);
            var leader = new Traveller(path, 12);
            Vector3 start = follower.Position;
            for (int tick = 0; tick < 1000; tick++)
            {
                leader.Advance(.05f);
                Move(follower, leader);
                Assert.That(Separation(follower, leader), Is.GreaterThan(.02f));
            }
            Assert.That(leader.Finished, Is.True);
            Assert.That(Vector3.Distance(start, follower.Position), Is.GreaterThan(15));
        }

        static bool Move(Traveller own, Traveller other)
        {
            if (own.Finished) return false;
            if (CityTrafficSafety.VehicleAhead(own.Position, own.Forward, .05f,
                other.Position, other.Forward)) return true;
            own.Advance(.05f);
            return false;
        }

        static Vector3[] Path(float side, bool reverse, Vector3 center, Quaternion turn)
        {
            var points = new Vector3[9];
            points[0] = new(side + 20, 0, side);
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f;
                points[i + 1] = new(side + 4 * (1 - t) * (1 - t), 0, side + 4 * t * t);
            }
            points[8] = new(side, 0, side + 20);
            if (reverse) System.Array.Reverse(points);
            for (int i = 0; i < points.Length; i++) points[i] = center + turn * points[i];
            return points;
        }

        static float Separation(Traveller a, Traveller b)
        {
            Vector3 rightA = Vector3.Cross(Vector3.up, a.Forward);
            Vector3 rightB = Vector3.Cross(Vector3.up, b.Forward);
            Vector3 offset = b.Position - a.Position;
            float gap = float.NegativeInfinity;
            foreach (var axis in new[] { a.Forward, rightA, b.Forward, rightB })
            {
                float extentA = 4.3f * Mathf.Abs(Vector3.Dot(axis, a.Forward)) +
                    1.225f * Mathf.Abs(Vector3.Dot(axis, rightA));
                float extentB = 4.3f * Mathf.Abs(Vector3.Dot(axis, b.Forward)) +
                    1.225f * Mathf.Abs(Vector3.Dot(axis, rightB));
                gap = Mathf.Max(gap, Mathf.Abs(Vector3.Dot(offset, axis)) - extentA - extentB);
            }
            return gap;
        }

        sealed class Traveller
        {
            readonly Vector3[] route;
            int next = 1;
            public Vector3 Position { get; private set; }
            public Vector3 Forward { get; private set; }
            public bool Finished => next == route.Length;

            public Traveller(Vector3[] points, float advance)
            {
                route = points;
                Position = route[0];
                Forward = (route[1] - Position).normalized;
                Advance(advance);
            }

            public void Advance(float step)
            {
                if (Finished) return;
                Vector3 direction = (route[next] - Position).normalized;
                Position = Vector3.MoveTowards(Position, route[next], step);
                // Kiểm tra cả độ trễ xoay thân: tốc độ xe nhanh nhất và góc quay chậm nhất.
                Forward = Vector3.RotateTowards(Forward, direction, 140 * Mathf.Deg2Rad * step / 7, 0);
                if ((Position - route[next]).sqrMagnitude < .00001f) next++;
            }
        }
    }
}
