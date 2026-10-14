using UnityEngine;

namespace Tycoon
{
    internal static class HandItemTubeGeometry
    {
        public static Mesh Build(string shape)
        {
            const int rings = 25;
            const int sides = 6;
            var vertices = new Vector3[rings * (sides + 1)];
            var uv = new Vector2[vertices.Length];
            for (int ring = 0; ring < rings; ring++)
            {
                float t = (float)ring / (rings - 1);
                Vector3 center = Point(shape, t);
                Vector3 tangent = (Point(shape, t + .001f) - Point(shape, t - .001f)).normalized;
                Vector3 across = Vector3.Cross(tangent, Vector3.up).normalized;
                Vector3 normal = Vector3.Cross(across, tangent).normalized;
                float radius = shape is "ring" or "handle" ? .035f : .022f;
                if (shape != "ring" && (ring == 0 || ring == rings - 1)) radius = 0;
                for (int side = 0; side <= sides; side++)
                {
                    float u = (float)side / sides;
                    float a = u * Mathf.PI * 2;
                    int index = ring * (sides + 1) + side;
                    vertices[index] = center + radius * (across * Mathf.Cos(a) + normal * Mathf.Sin(a));
                    uv[index] = new Vector2(u, t);
                }
            }
            return HandItemSculptGeometry.Finish(shape, vertices, uv, rings, sides);
        }

        static Vector3 Point(string shape, float t)
        {
            float a = t * Mathf.PI * 2;
            if (shape == "ring") return new Vector3(Mathf.Cos(a) * .45f, 0, Mathf.Sin(a) * .45f);
            if (shape == "handle")
                return new Vector3(Mathf.Cos(t * Mathf.PI) * .45f, 0, Mathf.Sin(t * Mathf.PI) * .45f);
            if (shape == "curl")
            {
                float radius = .12f + .3f * t;
                return new Vector3(Mathf.Cos(a * 1.4f) * radius, .055f * Mathf.Sin(a),
                    Mathf.Sin(a * 1.4f) * radius);
            }
            if (shape == "score")
                return new Vector3((t - .5f) * .4f, .1f * Mathf.Sin(t * Mathf.PI), (t - .5f) * .85f);
            if (shape == "strand")
                return new Vector3(.03f * Mathf.Sin(t * 7), .03f * Mathf.Sin(t * 4), t - .5f);
            return new Vector3(.14f * Mathf.Sin(t * 9), .015f * Mathf.Sin(t * 5), t - .5f);
        }
    }
}
