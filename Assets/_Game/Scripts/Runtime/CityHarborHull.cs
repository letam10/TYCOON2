using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityHarborHull
    {
        public static Mesh Build()
        {
            float[] z = { -4.6f, -2.8f, 3.2f, 5.1f, 6.6f };
            float[] width = { 1.8f, 2.3f, 2.3f, 1.4f, .035f };
            var sections = new Vector3[z.Length][];
            for (int i = 0; i < z.Length; i++)
                sections[i] = new[]
                {
                    new Vector3(-width[i], 1.15f, z[i]),
                    new Vector3(width[i], 1.15f, z[i]),
                    new Vector3(width[i] * .8f, .35f, z[i]),
                    new Vector3(0, .06f, z[i]),
                    new Vector3(-width[i] * .8f, .35f, z[i])
                };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int s = 0; s < sections.Length - 1; s++)
                for (int edge = 0; edge < 5; edge++)
                {
                    int next = (edge + 1) % 5;
                    Face(sections[s][edge], sections[s + 1][edge], sections[s + 1][next]);
                    Face(sections[s][edge], sections[s + 1][next], sections[s][next]);
                }
            for (int end = 0; end < 2; end++)
            {
                var ring = sections[end == 0 ? 0 : sections.Length - 1];
                Vector3 center = new(0, .7f, ring[0].z);
                for (int i = 0; i < 5; i++)
                {
                    int j = (i + 1) % 5;
                    if (end == 0) Face(center, ring[i], ring[j]);
                    else Face(center, ring[j], ring[i]);
                }
            }
            var mesh = new Mesh { name = "ClosedHarborHull" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }
        }
    }
}
