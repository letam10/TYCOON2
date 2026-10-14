using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityVehicleNormals
    {
        public static void Smooth(Mesh mesh)
        {
            Clean(mesh);
            var vertices = mesh.vertices;
            var source = mesh.normals;
            var result = new Vector3[source.Length];
            var groups = new Dictionary<Vector3, List<int>>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var point = vertices[i];
                var key = new Vector3(Mathf.Round(point.x * 100000), Mathf.Round(point.y * 100000),
                    Mathf.Round(point.z * 100000));
                if (!groups.TryGetValue(key, out var group)) groups.Add(key, group = new());
                group.Add(i);
            }
            // Chỉ làm mềm mặt cong/vát nhỏ; góc vuông của khung cửa vẫn giữ cạnh rõ.
            float limit = Mathf.Cos(35 * Mathf.Deg2Rad);
            foreach (var group in groups.Values)
            {
                foreach (int index in group)
                {
                    Vector3 sum = Vector3.zero;
                    foreach (int other in group)
                        if (Vector3.Dot(source[index], source[other]) >= limit) sum += source[other];
                    result[index] = sum.sqrMagnitude > .000001f ? sum.normalized : source[index];
                }
            }
            mesh.normals = result;
            mesh.RecalculateTangents();
        }

        static void Clean(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uv = mesh.uv;
            var triangles = mesh.triangles;
            var faces = new Vector3[vertices.Length];
            var valid = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                if (face.sqrMagnitude <= 1e-16f) continue;
                valid.Add(a);
                valid.Add(b);
                valid.Add(c);
                faces[a] += face;
                faces[b] += face;
                faces[c] += face;
            }
            var map = new Dictionary<int, int>();
            var points = new List<Vector3>();
            var directions = new List<Vector3>();
            var coordinates = new List<Vector2>();
            for (int i = 0; i < valid.Count; i++)
            {
                int old = valid[i];
                if (!map.TryGetValue(old, out int index))
                {
                    index = points.Count;
                    map.Add(old, index);
                    points.Add(vertices[old]);
                    Vector3 normal = normals.Length == vertices.Length ? normals[old] : Vector3.zero;
                    if (normal.sqrMagnitude < 1e-10f) normal = faces[old];
                    directions.Add(normal.normalized);
                    coordinates.Add(uv.Length == vertices.Length ? uv[old] : Vector2.zero);
                }
                valid[i] = index;
            }
            // Bỏ đỉnh không thuộc mặt thực; normal inverse-transpose phải chuẩn hóa trước làm mềm cạnh.
            mesh.Clear();
            mesh.SetVertices(points);
            mesh.SetNormals(directions);
            mesh.SetUVs(0, coordinates);
            mesh.SetTriangles(valid, 0);
            mesh.RecalculateBounds();
        }
    }
}
