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
            string shape = name == "Tapered carrot" ? "carrot" : name == "Egg shell" ? "egg" : kind.ToString();
            string key = shape + color + surface;
            if (mapped.TryGetValue(key, out var result) && result) return result;
            if (!sources.TryGetValue(shape, out var source) || !source)
            {
                source = kind == PrimitiveType.Cube ? Cube() : Lathe(shape);
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

        static Mesh Lathe(string shape)
        {
            bool cylinder = shape == "Cylinder";
            const int sides = 16;
            int rings = cylinder ? 6 : 11;
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
                else if (shape == "carrot")
                {
                    y = v - .5f;
                    radius = v < .85f ? .04f + v * .51f : (.5f - .02f) * (1 - v) / .15f;
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
