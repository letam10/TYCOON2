using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class TownFoliage
    {
        public static void Tree(Vector3 point, Transform parent, float scale, int seed)
        {
            var root = TownModelParts.Root("LandscapeTree", point, parent);
            root.localScale = Vector3.one * scale;
            var bark = new List<CombineInstance>();
            var crown = new List<CombineInstance>();
            Tube(Vector3.zero, new(.13f, 3.8f, .12f), .22f, .08f);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * 2.39996f + seed;
                var end = new Vector3(Mathf.Sin(angle) * 1.3f, 3.5f + i % 3 * .5f,
                    Mathf.Cos(angle) * 1.3f);
                Tube(new(0, 2.3f + i % 3 * .3f, 0), end, .08f, .025f);
                var mesh = Crown(seed + i * 7);
                crown.Add(new CombineInstance
                {
                    mesh = mesh,
                    transform = Matrix4x4.TRS(end, Quaternion.Euler(i * 13, seed, i * 7),
                        new Vector3(2.3f, 1.8f + i % 2 * .25f, 2.2f))
                });
            }
            Combine("TreeBark", bark, "wood", "#695745");
            Combine("TreeCrown", crown, "grass", seed % 2 == 0 ? "#687D4E" : "#586B44");
            foreach (var part in bark) Object.Destroy(part.mesh);
            foreach (var part in crown) Object.Destroy(part.mesh);

            void Tube(Vector3 a, Vector3 b, float bottom, float top)
            {
                var direction = b - a;
                bark.Add(new CombineInstance
                {
                    mesh = TubeMesh(bottom, top),
                    transform = Matrix4x4.TRS(a, Quaternion.FromToRotation(Vector3.up, direction),
                        new Vector3(1, direction.magnitude, 1))
                });
            }
            void Combine(string name, List<CombineInstance> parts, string surface, string tint)
            {
                var mesh = new Mesh();
                mesh.name = name;
                mesh.CombineMeshes(parts.ToArray());
                var part = TownModelParts.Root(name, Vector3.zero, root);
                part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = TownSurfaceMaterials.Get(surface, tint);
            }
        }

        static Mesh TubeMesh(float bottom, float top)
        {
            const int sides = 10;
            var vertices = new Vector3[(sides + 1) * 2];
            var triangles = new int[sides * 6];
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                vertices[i] = new(Mathf.Sin(a) * bottom, 0, Mathf.Cos(a) * bottom);
                vertices[i + sides + 1] = new(Mathf.Sin(a) * top, 1, Mathf.Cos(a) * top);
                if (i == sides) continue;
                int t = i * 6;
                triangles[t] = i;
                triangles[t + 1] = i + 1;
                triangles[t + 2] = i + sides + 1;
                triangles[t + 3] = i + 1;
                triangles[t + 4] = i + sides + 2;
                triangles[t + 5] = i + sides + 1;
            }
            var mesh = new Mesh { vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh Crown(int seed)
        {
            const int sides = 12;
            const int rings = 7;
            var vertices = new Vector3[(rings + 1) * (sides + 1)];
            var triangles = new List<int>();
            for (int y = 0; y <= rings; y++)
            {
                for (int x = 0; x <= sides; x++)
                {
                    float a = Mathf.PI * y / rings;
                    float b = Mathf.PI * 2 * x / sides;
                    var direction = new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a),
                        Mathf.Sin(a) * Mathf.Sin(b));
                    float noise = Mathf.PerlinNoise(direction.x * 4 + seed, direction.z * 4 + seed);
                    int index = y * (sides + 1) + x;
                    vertices[index] = direction * (.42f + noise * .14f);
                    if (y == rings || x == sides) continue;
                    int next = index + sides + 1;
                    triangles.Add(index);
                    triangles.Add(index + 1);
                    triangles.Add(next);
                    triangles.Add(index + 1);
                    triangles.Add(next + 1);
                    triangles.Add(next);
                }
            }
            var mesh = new Mesh { vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
