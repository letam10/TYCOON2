using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    // Gộp hình theo vật liệu và ô 32 m để giảm draw call mà vẫn cull theo khu.
    public sealed class CityStaticGeometry
    {
        readonly Transform root;
        readonly Dictionary<(Material, int, int), List<CombineInstance>> batches = new();
        readonly List<Mesh> owned = new();
        readonly Mesh box;
        readonly Mesh cylinder;
        readonly Mesh cone;
        readonly Mesh crown;

        public CityStaticGeometry(Transform parent)
        {
            root = parent;
            box = CityStaticGeometryMeshes.Box();
            cylinder = CityStaticGeometryMeshes.Radial(1, 1, 12);
            cone = CityStaticGeometryMeshes.Radial(1, 0, 12);
            crown = CityStaticGeometryMeshes.Crown();
            owned.AddRange(new[] { box, cylinder, cone, crown });
        }

        public void Box(Vector3 p, Vector3 size, string surface, string tint, float yaw = 0)
        {
            Add(box, p, size, Quaternion.Euler(0, yaw, 0), surface, tint);
        }

        public void Beam(Vector3 a, Vector3 b, float width, string surface, string tint)
        {
            Vector3 delta = b - a;
            Add(box, (a + b) * .5f, new(width, width, delta.magnitude),
                Quaternion.LookRotation(delta), surface, tint);
        }

        public void Ramp(Vector3 bottom, Vector3 top, string surface, string tint)
        {
            CityRailRampCollider.Build(root, bottom, top);
            Vector3 rise = top - bottom;
            float slope = Mathf.Atan2(rise.y, rise.x) * Mathf.Rad2Deg;
            RotatedBox((bottom + top) * .5f, new(rise.magnitude, .025f, 2.6f),
                Quaternion.Euler(0, 0, slope), surface, tint);
        }

        public void RotatedBox(Vector3 p, Vector3 size, Quaternion rotation, string surface, string tint)
        {
            Add(box, p, size, rotation, surface, tint);
        }

        public void Cylinder(Vector3 p, float radius, float height, string surface, string tint)
        {
            Add(cylinder, p, new(radius, height, radius), Quaternion.identity, surface, tint);
        }

        public void Cone(Vector3 p, float radius, float height, string surface, string tint)
        {
            Add(cone, p, new(radius, height, radius), Quaternion.identity, surface, tint);
        }

        public void Crown(Vector3 p, Vector3 size, string tint)
        {
            Add(crown, p, size, Quaternion.identity, "grass", tint);
        }

        public void Sculpt(Mesh mesh, Vector3 p, string surface, string tint)
        {
            Sculpt(mesh, p, Vector3.one, Quaternion.identity, surface, tint);
        }

        public void Sculpt(Mesh mesh, Vector3 p, Vector3 size, Quaternion rotation, string surface, string tint)
        {
            owned.Add(mesh);
            Add(mesh, p, size, rotation, surface, tint);
        }

        void Add(Mesh mesh, Vector3 p, Vector3 size, Quaternion rotation, string surface, string tint)
        {
            var material = TownSurfaceMaterials.Get(surface, tint);
            var key = (material, Mathf.FloorToInt(p.x / 32), Mathf.FloorToInt(p.z / 32));
            if (!batches.TryGetValue(key, out var list)) batches[key] = list = new();
            list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(p, rotation, size) });
        }

        public void Collider(string name, Vector3 p, Vector3 size, bool trigger = false)
        {
            var go = new GameObject(name + "Collision");
            go.transform.SetParent(root, false);
            go.transform.localPosition = p;
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = trigger;
        }

        public void Finish()
        {
            foreach (var pair in batches)
            {
                var mesh = new Mesh { name = "CityChunk", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true, false);
                mesh.RecalculateBounds();
                bool inspect = GameSession.Instance && GameSession.Instance.IsQa
                    && !GameSession.Instance.IsOrdinaryPlayCheck;
                mesh.UploadMeshData(!inspect);
                var go = new GameObject($"Scenery_{pair.Key.Item2}_{pair.Key.Item3}_{pair.Key.Item1.name}");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key.Item1;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                go.isStatic = true;
                owned.Add(mesh);
            }
            root.gameObject.AddComponent<CityStaticGeometryLifetime>().Meshes = owned.ToArray();
            batches.Clear();
        }
    }
}
