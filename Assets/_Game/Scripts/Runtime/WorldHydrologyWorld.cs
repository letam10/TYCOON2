using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public sealed class WorldHydrologyWorld : MonoBehaviour
    {
        readonly List<Mesh> meshes = new();
        Material water;

        public static void BuildOcean(Transform parent, Bounds bounds)
        {
            var owner = parent.gameObject.AddComponent<WorldHydrologyWorld>();
            owner.Add("OceanVisualHorizon", WorldHydrologyMeshes.Ocean(bounds), owner.Water());
            owner.Add("CoastalBank", WorldHydrologyMeshes.CoastBank(bounds),
                TownSurfaceMaterials.Get("sand", "#D4C49C"));
            WorldHydrologyNavigation.BuildOcean(parent, bounds);
        }

        public static void BuildRiver(Transform world)
        {
            var root = new GameObject("RiverEstuary").transform;
            root.SetParent(world, false);
            var owner = root.gameObject.AddComponent<WorldHydrologyWorld>();
            owner.Add("RiverWater", WorldHydrologyMeshes.RiverWater(), owner.Water());
            WorldHydrologyNavigation.BuildRiver(root);
            WorldHydrologyBridges.Build(root);
        }

        Material Water()
        {
            water = new Material(TownSurfaceMaterials.Get("water", CityOceanHorizon.Tint));
            water.name = "HydrologyWater";
            water.SetFloat("_UseShoreUv", 1);
            water.SetFloat("_DepthScale", 1);
            water.SetFloat("_WaveAmplitude", .8f);
            water.SetFloat("_FoamWidth", 2.5f);
            return water;
        }

        void Add(string name, Mesh mesh, Material material)
        {
            meshes.Add(mesh);
            var node = new GameObject(name);
            node.transform.SetParent(transform, false);
            node.isStatic = true;
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        void OnDestroy()
        {
            foreach (Mesh mesh in meshes)
                Release(mesh);
            Release(water);
        }

        static void Release(Object resource)
        {
            if (!resource)
                return;
            if (Application.isPlaying)
                Destroy(resource);
            else
                DestroyImmediate(resource);
        }
    }
}
