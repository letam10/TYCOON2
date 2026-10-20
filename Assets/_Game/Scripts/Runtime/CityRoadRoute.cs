using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityRoadRoute
    {
        public static Vector3[] CanonicalTrafficLoop(float laneOffset)
        {
            if (!TryBuildTrafficCenterline(out Vector3[] centerline))
                return new Vector3[0];

            Vector3[] laneCorners = OffsetClosedLoop(centerline, laneOffset);
            // Bo góc nằm trong bề rộng mặt đường; bán kính lớn hơn sẽ đẩy lane
            // ra ngoài footprint sampled ở các góc của highway loop.
            return Round(laneCorners, 2f);
        }

        public static Vector3[] Round(Vector3[] corners, float radius = 4)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 corner = corners[i];
                Vector3 before = corners[(i + corners.Length - 1) % corners.Length];
                Vector3 after = corners[(i + 1) % corners.Length];
                Vector3 incoming = (corner - before).normalized;
                Vector3 outgoing = (after - corner).normalized;
                if (Vector3.Dot(incoming, outgoing) > .98f)
                {
                    points.Add(corner);
                    continue;
                }
                float distance = Mathf.Min(radius, Vector3.Distance(before, corner) * .3f,
                    Vector3.Distance(after, corner) * .3f);
                Vector3 entry = corner - incoming * distance;
                Vector3 exit = corner + outgoing * distance;
                for (int segment = 0; segment <= 6; segment++)
                {
                    float t = segment / 6f;
                    points.Add(Vector3.Lerp(Vector3.Lerp(entry, corner, t), Vector3.Lerp(corner, exit, t), t));
                }
            }
            return points.ToArray();
        }

        public static Vector3 Spawn(Vector3[] route, float fraction, out int next)
        {
            float length = 0;
            for (int i = 0; i < route.Length; i++)
                length += Vector3.Distance(route[i], route[(i + 1) % route.Length]);
            float remaining = Mathf.Repeat(fraction, 1) * length;
            for (int i = 0; i < route.Length; i++)
            {
                next = (i + 1) % route.Length;
                float segment = Vector3.Distance(route[i], route[next]);
                if (remaining <= segment) return Vector3.Lerp(route[i], route[next], remaining / segment);
                remaining -= segment;
            }
            next = 1;
            return route[0];
        }

        static bool TryBuildTrafficCenterline(out Vector3[] centerline)
        {
            WorldRouteCorridor south = FindStreet("street-main-south");
            WorldRouteCorridor north = FindStreet("street-main-north");
            WorldRouteCorridor east = FindStreet("street-east-north");
            if (south == null || north == null || east == null)
            {
                centerline = null;
                return false;
            }

            if (!TryPointAtX(south.SampledNodes, -105, out Vector3 southWest) ||
                !TryPointAtX(south.SampledNodes, -22, out Vector3 southStopOne) ||
                !TryPointAtX(south.SampledNodes, 70, out Vector3 southStopTwo) ||
                !TryPointAtX(south.SampledNodes, 102, out Vector3 southEast) ||
                !TryPointAtZ(east.SampledNodes, 29, out Vector3 northEast) ||
                !TryPointAtX(north.SampledNodes, 70, out Vector3 northStopTwo) ||
                !TryPointAtX(north.SampledNodes, -22, out Vector3 northStopOne) ||
                !TryPointAtX(north.SampledNodes, -105, out Vector3 northWest))
            {
                centerline = null;
                return false;
            }

            centerline = new[]
            {
                southWest,
                southStopOne,
                southStopTwo,
                southEast,
                northEast,
                northStopTwo,
                northStopOne,
                northWest
            };
            return true;
        }

        static WorldRouteCorridor FindStreet(string id)
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildStreetNetwork();
            for (int index = 0; index < routes.Count; index++)
                if (routes[index].Id == id)
                    return routes[index];
            return null;
        }

        static bool TryPointAtX(
            IReadOnlyList<Vector3> samples,
            float x,
            out Vector3 point)
        {
            for (int index = 1; index < samples.Count; index++)
            {
                Vector3 start = samples[index - 1];
                Vector3 end = samples[index];
                float delta = end.x - start.x;
                if (Mathf.Abs(delta) <= .000001f)
                    continue;
                float amount = (x - start.x) / delta;
                if (amount < -.0001f || amount > 1.0001f)
                    continue;
                point = Vector3.Lerp(start, end, Mathf.Clamp01(amount));
                return true;
            }
            point = default;
            return false;
        }

        static bool TryPointAtZ(
            IReadOnlyList<Vector3> samples,
            float z,
            out Vector3 point)
        {
            for (int index = 1; index < samples.Count; index++)
            {
                Vector3 start = samples[index - 1];
                Vector3 end = samples[index];
                float delta = end.z - start.z;
                if (Mathf.Abs(delta) <= .000001f)
                    continue;
                float amount = (z - start.z) / delta;
                if (amount < -.0001f || amount > 1.0001f)
                    continue;
                point = Vector3.Lerp(start, end, Mathf.Clamp01(amount));
                return true;
            }
            point = default;
            return false;
        }

        static Vector3[] OffsetClosedLoop(Vector3[] corners, float laneOffset)
        {
            var result = new Vector3[corners.Length];
            for (int index = 0; index < corners.Length; index++)
            {
                Vector3 previous = corners[(index + corners.Length - 1) % corners.Length];
                Vector3 current = corners[index];
                Vector3 next = corners[(index + 1) % corners.Length];
                Vector3 incoming = current - previous;
                Vector3 outgoing = next - current;
                incoming.y = 0;
                outgoing.y = 0;
                incoming.Normalize();
                outgoing.Normalize();
                Vector3 incomingLeft = new(-incoming.z, 0, incoming.x);
                Vector3 outgoingLeft = new(-outgoing.z, 0, outgoing.x);
                Vector3 bisector = incomingLeft + outgoingLeft;
                if (bisector.sqrMagnitude <= .000001f)
                {
                    result[index] = current + incomingLeft * laneOffset;
                    continue;
                }
                bisector.Normalize();
                float alignment = Vector3.Dot(bisector, incomingLeft);
                float distance = Mathf.Abs(alignment) <= .000001f
                    ? laneOffset
                    : laneOffset / alignment;
                result[index] = current + bisector * distance;
            }
            return result;
        }
    }
}
