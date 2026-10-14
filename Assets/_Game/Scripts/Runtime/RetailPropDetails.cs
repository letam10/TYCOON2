using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class RetailPropDetails
    {
        public const string Steel = "#ADB7B2";
        public const string Iron = "#35413F";
        public const string Sage = "#647C6D";
        public const string Wood = "#A18A64";
        public const string Cream = "#D9D2B9";

        readonly struct Batch
        {
            public readonly Mesh Mesh;
            public readonly Material Material;

            public Batch(Mesh mesh, Material material)
            {
                Mesh = mesh;
                Material = material;
            }
        }

        static readonly Dictionary<string, List<Batch>> batches = new();

        public static Transform Box(Transform parent, string name, Vector3 position, Vector3 size,
            string color = Steel, string surface = "metal") =>
            TownModelParts.Box(name, position, size, surface, color, parent).transform;

        public static Transform Cylinder(Transform parent, string name, Vector3 position,
            float diameter, float depth, string color = Steel, bool forward = false)
        {
            var part = TownModelParts.Cylinder(name, position, new(diameter, depth * .5f, diameter),
                "metal", color, parent).transform;
            if (forward) part.localRotation = Quaternion.Euler(90, 0, 0);
            return part;
        }

        public static void Bolt(Transform parent, Vector3 position) =>
            Cylinder(parent, "CaptiveBolt", position, .045f, .014f, forward: true);

        public static void Handle(Transform parent, Vector3 position)
        {
            for (int side = -1; side <= 1; side += 2)
                Box(parent, "HandleMount", position + new Vector3(side * .1f, 0, -.02f),
                    new(.035f, .035f, .055f));
            Box(parent, "DrawerHandle", position + Vector3.forward * .012f, new(.235f, .035f, .035f));
        }

        public static void Crate(Transform root)
        {
            for (int side = -1; side <= 1; side += 2)
                Box(root, "PalletRunner", new(side * .39f, .045f, 0),
                    new(.17f, .09f, 1.15f), Wood, "wood");
            for (int plank = 0; plank < 4; plank++)
                Box(root, "PalletDeck", new(0, .115f, -.42f + plank * .28f),
                    new(1.15f, .05f, .27f), Wood, "wood");
            Box(root, "ClosedCrateCore", new(0, .3925f, 0), new(1.04f, .505f, 1.04f), Iron);
            for (int plank = 0; plank < 3; plank++)
            {
                float y = .225f + plank * .168f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(root, "CrateFrontBoard", new(0, y, side * .541f),
                        new(1.1f, .162f, .042f), Wood, "wood");
                    Box(root, "CrateSideBoard", new(side * .541f, y, 0),
                        new(.042f, .162f, 1.036f), Wood, "wood");
                }
            }
            for (int plank = 0; plank < 4; plank++)
                Box(root, "ClosedLidBoard", new(-.4125f + plank * .275f, .665f, 0),
                    new(.268f, .04f, 1.1f), Wood, "wood");
            for (int strap = -1; strap <= 1; strap += 2)
            {
                Box(root, "LidBand", new(strap * .36f, .6925f, 0), new(.044f, .015f, 1.12f), Steel);
                for (int face = -1; face <= 1; face += 2)
                    Box(root, "SideBand", new(strap * .36f, .42f, face * .563f),
                        new(.044f, .53f, .015f), Steel);
            }
        }

        public static bool Restore(string key, Transform root)
        {
            if (!batches.TryGetValue(key, out var cached)) return false;
            foreach (var batch in cached)
                if (!batch.Mesh || !batch.Material) return false;
            foreach (var batch in cached) AddBatch(root, batch);
            return true;
        }

        // Ghép một lần theo vật liệu; mọi bản sao dùng chung mesh, không có Update hay mesh riêng.
        public static void Bake(string key, Transform root)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var source in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = source.GetComponent<MeshRenderer>();
                var material = renderer.sharedMaterial;
                if (!groups.TryGetValue(material, out var group))
                    groups[material] = group = new List<CombineInstance>();
                group.Add(new CombineInstance
                {
                    mesh = source.sharedMesh,
                    transform = root.worldToLocalMatrix * source.transform.localToWorldMatrix
                });
            }
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }
            var result = new List<Batch>();
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = "Retail_" + key + "_" + result.Count };
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                var batch = new Batch(mesh, group.Key);
                result.Add(batch);
                AddBatch(root, batch);
            }
            batches[key] = result;
        }

        static void AddBatch(Transform root, Batch batch)
        {
            var part = TownModelParts.Root("RetailBatch", Vector3.zero, root);
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = batch.Mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = batch.Material;
        }
    }
}
