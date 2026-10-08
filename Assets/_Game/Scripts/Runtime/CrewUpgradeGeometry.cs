using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CrewUpgradeGeometry
    {
        static readonly Dictionary<Vector3Int, Mesh> meshes = new();

        public static void Build(Transform parent, int count, int carry, int speed)
        {
            if (count <= 1 && carry <= 1 && speed <= 1) return;
            var key = new Vector3Int(count, carry, speed);
            if (!meshes.TryGetValue(key, out var mesh) || !mesh)
            {
                var pieces = new List<CombineInstance>();
                for (int i = 1; i < carry; i++)
                    Add(pieces, "Cloth harness", new Vector3(0, .82f + i * .11f, .21f),
                        new Vector3(.35f, .05f, .025f), "#AA8D56", "fiber");
                for (int i = 1; i < speed; i++)
                    Add(pieces, "Cloth stripe", new Vector3(.23f, 1.24f + i * .07f, .14f),
                        new Vector3(.06f, .035f, .12f), "#CFB67C", "fiber");
                for (int i = 1; i < count; i++)
                    Add(pieces, "Rank pin", new Vector3(-.20f, 1.23f + i * .05f, .16f),
                        new Vector3(.07f, .026f, .025f), "#BFA86C", "metal");
                // Giữ đủ chi tiết cấp, dùng chung một mesh/atlas thay vì nhiều renderer mỗi nhân viên.
                mesh = new Mesh { name = "CrewUpgrade_" + key };
                mesh.CombineMeshes(pieces.ToArray(), true, true);
                mesh.RecalculateBounds();
                meshes[key] = mesh;
                HandItemSurfaceAtlas.Apply();
            }
            var root = new GameObject("CrewUpgradeDetails", typeof(MeshFilter), typeof(MeshRenderer));
            root.transform.SetParent(parent, false);
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            root.GetComponent<MeshRenderer>().sharedMaterial = HandItemSurfaceAtlas.Material;
        }

        static void Add(List<CombineInstance> pieces, string name, Vector3 point, Vector3 size,
            string color, string surface)
        {
            pieces.Add(new CombineInstance
            {
                mesh = HandItemMeshPrimitives.Get(PrimitiveType.Cube, name, color, surface),
                transform = Matrix4x4.TRS(point, Quaternion.identity, size)
            });
        }
    }
}
