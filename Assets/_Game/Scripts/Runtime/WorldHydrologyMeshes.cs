using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class WorldHydrologyMeshes
    {
        static readonly float[] oceanOffsets = { 0, 1, 3, 6, 12, 24, 48, 96, 192, 384, 768 };

        public static Mesh Ocean(Bounds bounds)
        {
            var data = new Surface();
            var rows = new List<float>();
            float bottom = bounds.min.z - 1600;
            float top = bounds.max.z + 1600;
            for (float z = bottom; z < top;)
            {
                rows.Add(z);
                z = Mathf.Min(top, z + (z < bounds.min.z - 16 || z > bounds.max.z + 16 ? 16 : 2));
            }
            rows.Add(top);
            float right = bounds.max.x + 1600;
            for (int row = 1; row < rows.Count; row++)
            {
                float z0 = rows[row - 1];
                float z1 = rows[row];
                float coast0 = WorldHydrologyLayout.CoastX(z0);
                float coast1 = WorldHydrologyLayout.CoastX(z1);
                for (int column = 0; column < oceanOffsets.Length; column++)
                {
                    float next0 = column + 1 == oceanOffsets.Length ? right : coast0 + oceanOffsets[column + 1];
                    float next1 = column + 1 == oceanOffsets.Length ? right : coast1 + oceanOffsets[column + 1];
                    data.WaterQuad(Point(coast0 + oceanOffsets[column], z0), Point(next0, z0),
                        Point(next1, z1), Point(coast1 + oceanOffsets[column], z1));
                }
            }
            return data.Create("ContinuousCoastalOcean");
        }

        public static Mesh CoastBank(Bounds bounds)
        {
            return WorldHydrologyTerrain.Sand(bounds);
        }

        public static Mesh RiverWater()
        {
            return WorldHydrologyTerrain.River();
        }
        static Vector3 Point(float x, float z)
        {
            return new Vector3(x, WorldHydrologyLayout.WaterLevel, z);
        }

        sealed class Surface
        {
            readonly List<Vector3> vertices = new();
            readonly List<Vector2> uvs = new();
            readonly List<int> triangles = new();

            public void WaterQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Quad(a, b, c, d, true);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool water)
            {
                int first = vertices.Count;
                foreach (Vector3 point in new[] { a, b, c, d })
                {
                    vertices.Add(point);
                    uvs.Add(water ? new Vector2(WorldHydrologyLayout.ShoreDistance(point), 0)
                        : new Vector2(point.x, point.z));
                }
                Triangle(first, first + 1, first + 2);
                Triangle(first, first + 2, first + 3);
            }

            void Triangle(int a, int b, int c)
            {
                Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                if (normal.sqrMagnitude < .00000001f)
                    return;
                triangles.Add(a);
                triangles.Add(normal.y >= 0 ? b : c);
                triangles.Add(normal.y >= 0 ? c : b);
            }

            public Mesh Create(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                Bounds bounds = mesh.bounds;
                bounds.Expand(new Vector3(0, .15f, 0));
                mesh.bounds = bounds;
                return mesh;
            }
        }
    }
}
