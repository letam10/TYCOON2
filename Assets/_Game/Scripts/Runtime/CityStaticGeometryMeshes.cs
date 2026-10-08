using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityStaticGeometryMeshes
    {
        public static Mesh Box()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Face(new(-.5f, -.5f, -.5f), new(-.5f, .5f, -.5f), new(.5f, .5f, -.5f), new(.5f, -.5f, -.5f));
            Face(new(.5f, -.5f, .5f), new(.5f, .5f, .5f), new(-.5f, .5f, .5f), new(-.5f, -.5f, .5f));
            Face(new(-.5f, -.5f, .5f), new(-.5f, .5f, .5f), new(-.5f, .5f, -.5f), new(-.5f, -.5f, -.5f));
            Face(new(.5f, -.5f, -.5f), new(.5f, .5f, -.5f), new(.5f, .5f, .5f), new(.5f, -.5f, .5f));
            Face(new(-.5f, .5f, -.5f), new(-.5f, .5f, .5f), new(.5f, .5f, .5f), new(.5f, .5f, -.5f));
            Face(new(-.5f, -.5f, .5f), new(-.5f, -.5f, -.5f), new(.5f, -.5f, -.5f), new(.5f, -.5f, .5f));
            return Build(vertices, triangles);

            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int index = vertices.Count;
                vertices.AddRange(new[] { a, b, c, d });
                triangles.AddRange(new[] { index, index + 1, index + 2, index, index + 2, index + 3 });
            }
        }

        public static Mesh Radial(float bottom, float top, int sides)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                float b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 lowA = new(Mathf.Cos(a) * bottom, -.5f, Mathf.Sin(a) * bottom);
                Vector3 lowB = new(Mathf.Cos(b) * bottom, -.5f, Mathf.Sin(b) * bottom);
                Vector3 highA = new(Mathf.Cos(a) * top, .5f, Mathf.Sin(a) * top);
                Vector3 highB = new(Mathf.Cos(b) * top, .5f, Mathf.Sin(b) * top);
                Triangle(lowA, highA, highB);
                Triangle(lowA, highB, lowB);
                Triangle(new(0, .5f, 0), highB, highA);
                Triangle(new(0, -.5f, 0), lowA, lowB);
            }
            return Build(vertices, triangles);

            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int index = vertices.Count;
                vertices.AddRange(new[] { a, b, c });
                triangles.AddRange(new[] { index, index + 1, index + 2 });
            }
        }

        public static Mesh Crown()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int sides = 9;
            const int rings = 5;
            for (int ring = 0; ring <= rings; ring++)
            {
                float angle = ring * Mathf.PI / rings;
                for (int i = 0; i <= sides; i++)
                {
                    float a = i * Mathf.PI * 2 / sides;
                    vertices.Add(new Vector3(Mathf.Sin(angle) * Mathf.Cos(a), Mathf.Cos(angle),
                        Mathf.Sin(angle) * Mathf.Sin(a)) * .5f);
                }
            }
            for (int ring = 0; ring < rings; ring++)
            {
                for (int i = 0; i < sides; i++)
                {
                    int a = ring * (sides + 1) + i;
                    int b = a + sides + 1;
                    triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            }
            return Build(vertices, triangles);
        }

        static Mesh Build(List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = "CitySharedPrimitive" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            var uv = new Vector2[vertices.Count];
            for (int i = 0; i < uv.Length; i++) uv[i] = new(vertices[i].x, vertices[i].z);
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
