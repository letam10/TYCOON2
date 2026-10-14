using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityRoadRoute
    {
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
    }
}
