using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    internal static class HandItemMeshPrimitives
    {
        static readonly Dictionary<string, Mesh> sources = new();
        static readonly Dictionary<string, Mesh> mapped = new();

        public static Mesh Get(PrimitiveType kind, string name, string color, string surface)
        {
            surface = HandItemSurfaceAtlas.Kind(name, surface);
            string shape = name switch
            {
                "Tapered carrot" => "carrot",
                "Carrot leaf" => "carrotLeaf",
                "Egg shell" => "egg",
                _ => kind.ToString()
            };
            return GetShape(shape, color, surface);
        }

        public static Mesh GetShape(string shape, string color, string surface)
        {
            string key = shape + color + surface;
            if (mapped.TryGetValue(key, out var result) && result) return result;
            if (!sources.TryGetValue(shape, out var source) || !source)
            {
                source = shape switch
                {
                    "carrot" => HandItemCarrotGeometry.Root(),
                    "carrotLeaf" => HandItemCarrotGeometry.Leaf(),
                    "Cube" => Cube(),
                    "Sphere" or "Cylinder" or "egg" => Lathe(shape),
                    _ => HandItemSculptGeometry.Build(shape)
                };
                CompleteSourceGeometry(source);
                sources[shape] = source;
            }
            result = Object.Instantiate(source);
            result.name = "CarryPart_" + key;
            var tile = HandItemSurfaceAtlas.TileFor(color, surface);
            var uv = result.uv;
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(tile.x + uv[i].x * tile.width, tile.y + uv[i].y * tile.height);
            result.uv = uv;
            mapped[key] = result;
            return result;
        }

        static Mesh Cube()
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            primitive.SetActive(false);
            if (Application.isPlaying) Object.Destroy(primitive);
            else Object.DestroyImmediate(primitive);
            return mesh;
        }

        static void CompleteSourceGeometry(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            var normals = mesh.normals;
            var triangles = mesh.triangles;
            var kept = new List<int>(triangles.Length);
            var sums = new Vector3[vertices.Length];
            var strongest = new Vector3[vertices.Length];
            var used = new bool[vertices.Length];
            // Cực của mặt xoay có tam giác diện tích 0 và đỉnh không thuộc mặt nào.
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                if (face.sqrMagnitude <= 1e-16f) continue;
                for (int corner = 0; corner < 3; corner++)
                {
                    int index = triangles[i + corner];
                    used[index] = true;
                    sums[index] += face;
                    if (face.sqrMagnitude > strongest[index].sqrMagnitude) strongest[index] = face;
                    kept.Add(index);
                }
            }
            var positions = new List<Vector3>(vertices.Length);
            var mappedUv = new List<Vector2>(vertices.Length);
            var directions = new List<Vector3>(vertices.Length);
            var remap = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!used[i]) continue;
                remap[i] = positions.Count;
                positions.Add(vertices[i]);
                mappedUv.Add(uv[i]);
                Vector3 direction = normals[i];
                float squared = direction.sqrMagnitude;
                if (float.IsNaN(squared) || float.IsInfinity(squared) || squared <= 1e-12f)
                    direction = sums[i].sqrMagnitude > 1e-12f ? sums[i] : strongest[i];
                directions.Add(direction / Mathf.Sqrt(direction.sqrMagnitude));
            }
            for (int i = 0; i < kept.Count; i++) kept[i] = remap[kept[i]];
            mesh.Clear();
            mesh.SetVertices(positions);
            mesh.SetUVs(0, mappedUv);
            mesh.SetNormals(directions);
            mesh.SetTriangles(kept, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        static Mesh Lathe(string shape)
        {
            bool cylinder = shape == "Cylinder";
            int sides = shape == "egg" ? 24 : 16;
            int rings = cylinder ? 6 : shape == "egg" ? 15 : 11;
            var vertices = new Vector3[rings * (sides + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(rings - 1) * sides * 6];
            for (int ring = 0; ring < rings; ring++)
            {
                float v = (float)ring / (rings - 1);
                float radius = Mathf.Sin(v * Mathf.PI) * .5f;
                float y = -.5f * Mathf.Cos(v * Mathf.PI);
                if (cylinder)
                {
                    radius = ring == 0 || ring == rings - 1 ? 0 : .5f;
                    y = ring <= 2 ? -1 : 1;
                }
                else if (shape == "egg") radius *= 1.05f - v * .24f;
                for (int side = 0; side <= sides; side++)
                {
                    float u = (float)side / sides;
                    int index = ring * (sides + 1) + side;
                    vertices[index] = new Vector3(Mathf.Cos(u * Mathf.PI * 2) * radius, y,
                        Mathf.Sin(u * Mathf.PI * 2) * radius);
                    uv[index] = new Vector2(u, v);
                }
            }
            int cursor = 0;
            for (int ring = 0; ring < rings - 1; ring++)
                for (int side = 0; side < sides; side++)
                {
                    int a = ring * (sides + 1) + side;
                    int b = a + sides + 1;
                    triangles[cursor++] = a;
                    triangles[cursor++] = b;
                    triangles[cursor++] = a + 1;
                    triangles[cursor++] = a + 1;
                    triangles[cursor++] = b;
                    triangles[cursor++] = b + 1;
                }
            var mesh = new Mesh { name = "CarrySource_" + shape, vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            // Ghép pháp tuyến tại đường UV để mặt tròn không bị sọc tối.
            var normals = mesh.normals;
            for (int ring = 0; ring < rings; ring++)
            {
                int first = ring * (sides + 1);
                Vector3 joined = (normals[first] + normals[first + sides]).normalized;
                normals[first] = joined;
                normals[first + sides] = joined;
            }
            mesh.normals = normals;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
