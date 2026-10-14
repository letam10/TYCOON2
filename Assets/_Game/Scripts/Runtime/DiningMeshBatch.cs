using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class DiningMeshBatch
    {
        public static void Combine(Transform root)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var sources = root.GetComponentsInChildren<MeshFilter>();
            foreach (var source in sources)
            {
                var renderer = source.GetComponent<MeshRenderer>();
                if (!source.sharedMesh || !renderer || !renderer.sharedMaterial) continue;
                var material = renderer.sharedMaterial;
                if (!groups.TryGetValue(material, out var parts))
                    groups[material] = parts = new List<CombineInstance>();
                parts.Add(new CombineInstance
                {
                    mesh = source.sharedMesh,
                    transform = root.worldToLocalMatrix * source.transform.localToWorldMatrix
                });
                renderer.enabled = false;
                Remove(renderer);
                Remove(source);
            }
            var lifetime = root.gameObject.AddComponent<DiningMeshLifetime>();
            lifetime.Meshes = new Mesh[groups.Count];
            int index = 0;
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = "Dining_" + root.name + "_" + index };
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                lifetime.Meshes[index++] = mesh;
                var part = TownModelParts.Root("DiningBatch", Vector3.zero, root);
                part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
            }
        }

        static void Remove(Object value)
        {
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
