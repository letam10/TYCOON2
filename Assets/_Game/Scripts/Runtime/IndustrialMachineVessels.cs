using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class IndustrialMachineVessels
    {
        static readonly Dictionary<Vector3, Mesh> rings = new();

        public static Transform Ring(Transform parent, string name, Vector3 p,
            float outside, float inside, float height, string color = IndustrialMachineParts.Steel)
        {
            var root = TownModelParts.Root(name, p, parent);
            Vector3 key = new(outside, inside, height);
            if (!rings.TryGetValue(key, out var mesh) || !mesh)
            {
                mesh = BuildRing(outside, inside, height);
                rings[key] = mesh;
            }
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = TownSurfaceMaterials.Get("metal", color);
            return root;
        }

        public static void Vat(Transform t, Vector3 center, float diameter, float height,
            string color = IndustrialMachineParts.Steel)
        {
            Ring(t, "JacketedVessel", center, diameter * .5f, diameter * .5f - .045f, height, color);
            IndustrialMachineParts.Cylinder(t, "VesselBottom",
                center + Vector3.down * (height * .5f - .025f), diameter - .08f, .05f, color);
            Ring(t, "RolledLip", center + Vector3.up * (height * .5f),
                diameter * .5f + .015f, diameter * .5f - .048f, .045f);
        }

        public static void Hopper(Transform t, Vector3 at, float width, float height)
        {
            // Bốn thành dày khép góc; đáy thu hẹp nối vào cổ nạp, miệng vẫn mở.
            float inset = width * .22f;
            float angle = Mathf.Atan2(inset, height) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(height * height + inset * inset);
            for (int i = 0; i < 4; i++)
            {
                var pivot = TownModelParts.Root("HopperWallPivot", at, t);
                pivot.localRotation = Quaternion.Euler(0, i * 90, 0);
                IndustrialMachineParts.Box(pivot, "HopperWall", new(0, 0, width * .5f - inset * .5f),
                    new(width, length, .045f), angle: new(angle, 0, 0));
            }
            IndustrialMachineParts.Box(t, "HopperThroat", at + Vector3.down * (height * .5f + .06f),
                new(width * .56f, .15f, width * .56f), IndustrialMachineParts.Iron);
        }

        static Mesh BuildRing(float outside, float inside, float height)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI / 16;
                float b = (i + 1) * Mathf.PI / 16;
                Vector3 ob = new(Mathf.Cos(a) * outside, -height * .5f, Mathf.Sin(a) * outside);
                Vector3 ot = new(ob.x, height * .5f, ob.z);
                Vector3 nb = new(Mathf.Cos(b) * outside, -height * .5f, Mathf.Sin(b) * outside);
                Vector3 nt = new(nb.x, height * .5f, nb.z);
                Vector3 ib = new(Mathf.Cos(a) * inside, -height * .5f, Mathf.Sin(a) * inside);
                Vector3 it = new(ib.x, height * .5f, ib.z);
                Vector3 jb = new(Mathf.Cos(b) * inside, -height * .5f, Mathf.Sin(b) * inside);
                Vector3 jt = new(jb.x, height * .5f, jb.z);
                Quad(ob, ot, nt, nb);
                Quad(jb, jt, it, ib);
                Quad(ot, it, jt, nt);
                Quad(ob, nb, jb, ib);
            }
            var mesh = new Mesh { name = "IndustrialRing" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int start = vertices.Count;
                vertices.AddRange(new[] { a, b, c, d });
                indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
        }
    }
}
