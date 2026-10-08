using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class TownModelParts
    {
        static readonly Dictionary<Vector3, Mesh> meshes = new();

        public static Transform Root(string name, Vector3 point, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = point;
            return root;
        }

        public static GameObject Box(string name, Vector3 point, Vector3 size, string surface,
            string tint, Transform parent, float yaw = 0, bool solid = false)
        {
            var root = Root(name, point, parent);
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = BeveledBox(size);
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = TownSurfaceMaterials.Get(surface, tint);
            if (solid) root.gameObject.AddComponent<BoxCollider>().size = size;
            return root.gameObject;
        }

        public static GameObject Cylinder(string name, Vector3 point, Vector3 size, string surface,
            string tint, Transform parent)
        {
            var part = Art.Cylinder(name, point, size, tint, parent);
            part.GetComponent<Renderer>().sharedMaterial = TownSurfaceMaterials.Get(surface, tint);
            return part;
        }

        // Vát cả cạnh đứng và nắp: highlight mảnh làm đồ vật có khối ở góc camera gần.
        static Mesh BeveledBox(Vector3 size)
        {
            if (meshes.TryGetValue(size, out var found) && found) return found;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float x = size.x * .5f;
            float y = size.y * .5f;
            float z = size.z * .5f;
            float b = Mathf.Min(.04f, Mathf.Min(x, Mathf.Min(y, z)) * .2f);
            Vector2[] outline =
            {
                new(-x + b, -z), new(x - b, -z), new(x, -z + b), new(x, z - b),
                new(x - b, z), new(-x + b, z), new(-x, z - b), new(-x, -z + b)
            };
            var lower = new Vector3[8];
            var upper = new Vector3[8];
            var bottom = new Vector3[8];
            var top = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var p = outline[i];
                lower[i] = new(p.x, -y + b, p.y);
                upper[i] = new(p.x, y - b, p.y);
                float bx = p.x - Mathf.Sign(p.x) * b;
                float bz = p.y - Mathf.Sign(p.y) * b;
                bottom[i] = new(bx, -y, bz);
                top[i] = new(bx, y, bz);
            }
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                Quad(lower[i], upper[i], upper[next], lower[next]);
                Quad(upper[i], top[i], top[next], upper[next]);
                Quad(bottom[i], lower[i], lower[next], bottom[next]);
                Triangle(new(0, y, 0), top[next], top[i]);
                Triangle(new(0, -y, 0), bottom[i], bottom[next]);
            }
            var mesh = new Mesh();
            mesh.name = "TownBevel_" + size;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshes[size] = mesh;
            return mesh;

            void Triangle(Vector3 a, Vector3 c, Vector3 d)
            {
                int index = vertices.Count;
                vertices.Add(a);
                vertices.Add(c);
                vertices.Add(d);
                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
            }
            void Quad(Vector3 a, Vector3 c, Vector3 d, Vector3 e)
            {
                Triangle(a, c, d);
                Triangle(a, d, e);
            }
        }
    }
}
