using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    // Ghép trực tiếp các mặt theo vật liệu; không tạo hàng trăm GameObject tạm.
    public sealed class CityVehicleGeometry
    {
        readonly List<Vector3> vertices = new();
        readonly List<Vector2> uv = new();
        readonly Dictionary<Material, List<int>> faces = new();

        public void Quad(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            uv.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            if (!faces.TryGetValue(material, out var triangles)) faces.Add(material, triangles = new());
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        public void Box(Material material, Vector3 center, Vector3 size, float bevel = .025f)
        {
            float b = Mathf.Min(bevel, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .2f);
            float x = size.x * .5f;
            float y = size.y * .5f;
            float z = size.z * .5f;
            Vector2[] shape =
            {
                new(-x + b, -y), new(x - b, -y), new(x, -y + b), new(x, y - b),
                new(x - b, y), new(-x + b, y), new(-x, y - b), new(-x, -y + b)
            };
            Loft(material, center, new[] { -z, -z + b, z - b, z }, shape,
                new[] { .96f, 1f, 1f, .96f });
        }

        public void Loft(Material material, Vector3 center, float[] zs, Vector2[] shape, float[] scales)
        {
            for (int ring = 0; ring < zs.Length - 1; ring++)
                for (int p = 0; p < shape.Length; p++)
                {
                    int next = (p + 1) % shape.Length;
                    Quad(material, Point(ring, p), Point(ring, next), Point(ring + 1, next), Point(ring + 1, p));
                }
            for (int p = 1; p < shape.Length - 1; p++)
            {
                Quad(material, Point(0, 0), Point(0, p + 1), Point(0, p), Point(0, 0));
                int last = zs.Length - 1;
                Quad(material, Point(last, 0), Point(last, p), Point(last, p + 1), Point(last, 0));
            }
            Vector3 Point(int ring, int p) => center +
                new Vector3(shape[p].x * scales[ring], shape[p].y * scales[ring], zs[ring]);
        }

        public void Cylinder(Material material, Vector3 center, float radius, float length,
            bool alongX = false, int segments = 24)
        {
            var shape = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                shape[i] = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
            if (!alongX)
            {
                Loft(material, center, new[] { -length * .5f, -length * .46f, length * .46f, length * .5f },
                    shape, new[] { .86f, 1f, 1f, .86f });
                return;
            }
            int first = vertices.Count;
            Loft(material, Vector3.zero, new[] { -length * .5f, -length * .4f, length * .4f, length * .5f },
                shape, new[] { .9f, 1f, 1f, .9f });
            var rotation = Quaternion.Euler(0, 90, 0);
            for (int i = first; i < vertices.Count; i++) vertices[i] = center + rotation * vertices[i];
        }

        public void Beam(Material material, Vector3 from, Vector3 to, float width)
        {
            int first = vertices.Count;
            Box(material, Vector3.zero, new(width, width, Vector3.Distance(from, to)), width * .1f);
            var rotation = Quaternion.LookRotation(to - from);
            for (int i = first; i < vertices.Count; i++)
                vertices[i] = (from + to) * .5f + rotation * vertices[i];
        }

        public void Arch(Material material, float x, float z, float y, float radius, float thickness)
        {
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI / 20;
                float b = (i + 1) * Mathf.PI / 20;
                Vector3 Point(float angle, float r) => new(x, y + Mathf.Sin(angle) * r,
                    z + Mathf.Cos(angle) * r);
                Quad(material, Point(a, radius), Point(b, radius), Point(b, radius + thickness),
                    Point(a, radius + thickness));
                Quad(material, Point(a, radius + thickness), Point(b, radius + thickness),
                    Point(b, radius), Point(a, radius));
            }
        }

        public void Build(string name, Transform parent, CityVehicleRig owner)
        {
            foreach (var group in faces)
            {
                // Kính tách riêng để transparent sorting không kéo cả vỏ vào hàng trong suốt.
                var mesh = new Mesh { name = name + "_" + group.Key.name, indexFormat = IndexFormat.UInt32 };
                var compactVertices = new List<Vector3>();
                var compactUv = new List<Vector2>();
                var indices = new List<int>();
                var remap = new Dictionary<int, int>();
                foreach (int old in group.Value)
                {
                    if (!remap.TryGetValue(old, out int index))
                    {
                        index = compactVertices.Count;
                        remap.Add(old, index);
                        compactVertices.Add(vertices[old]);
                        compactUv.Add(uv[old]);
                    }
                    indices.Add(index);
                }
                mesh.SetVertices(compactVertices);
                mesh.SetUVs(0, compactUv);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                CityVehicleNormals.Smooth(mesh);
                mesh.RecalculateBounds();
                owner.Own(mesh);
                var node = TownModelParts.Root(name + "_" + group.Key.name, Vector3.zero, parent);
                node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                node.gameObject.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
            }
        }

        public void BuildSingle(string name, Transform parent, CityVehicleRig owner)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = faces.Count;
            var materials = new List<Material>();
            int sub = 0;
            foreach (var group in faces)
            {
                mesh.SetTriangles(group.Value, sub++);
                materials.Add(group.Key);
            }
            mesh.RecalculateNormals();
            CityVehicleNormals.Smooth(mesh);
            mesh.RecalculateBounds();
            owner.Own(mesh);
            parent.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            parent.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
        }
    }
}
