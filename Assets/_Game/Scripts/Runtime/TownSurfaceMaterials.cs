using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class TownSurfaceMaterials
    {
        static readonly Dictionary<string, Material> cache = new();

        public static Material Paint(string hex)
        {
            string key = "paint:" + hex;
            if (cache.TryGetValue(key, out var found) && found) return found;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "Paint_" + hex;
            material.SetColor("_BaseColor", Art.Hex(hex));
            material.SetFloat("_Smoothness", .28f);
            material.enableInstancing = true;
            cache[key] = material;
            return material;
        }

        public static Material Get(string surface, string tint = "#FFFFFF")
        {
            string key = surface + ":" + tint;
            bool water = surface == "water";
            if (cache.TryGetValue(key, out var found) && found)
            {
                if (!water && !found.GetTexture("_SurfaceMap"))
                    found.SetTexture("_SurfaceMap", TownSurfaceTexture.Get(surface));
                return found;
            }
            var shader = Resources.Load<Shader>("VisualRedesign/" + (water ? "TownWater" : "TownSurface"));
            var material = new Material(shader);
            material.name = "TownSurface_" + key;
            material.SetColor("_BaseColor", Art.Hex(tint));
            material.SetFloat("_Smoothness", Smoothness(surface));
            material.SetFloat("_Metallic", surface == "metal" ? .75f : 0);
            if (!water)
            {
                material.SetTexture("_SurfaceMap", TownSurfaceTexture.Get(surface));
                material.SetFloat("_WorldScale", WorldScale(surface));
                material.SetFloat("_BumpScale", surface is "metal" or "glass" ? 0 : .018f);
                material.SetFloat("_IrregularDetail", surface is "wood" or "brick" or "roof" ? 0 : 1);
                material.SetFloat("_SurfaceKind", surface switch
                {
                    "grass" => 1,
                    "asphalt" => 2,
                    "paving" => 3,
                    // CityStreetNetwork giữ ID stone cho mặt đường đã static batch.
                    "stone" when tint == "#666C6C" => 2,
                    _ => 0
                });
                material.SetColor("_SoilColor", Art.Hex("#877354"));
                material.SetFloat("_GravelDensity", surface == "grass" ? .34f : surface == "soil" ? .55f : 0);
            }
            material.enableInstancing = true;
            cache[key] = material;
            return material;
        }

        static float WorldScale(string surface) => surface switch
        {
            "grass" => .32f,
            "soil" => .085f,
            "asphalt" => .065f,
            "paving" or "ceramic" or "stone" => .1f,
            "wood" => .45f,
            "brick" => .3f,
            "roof" => .35f,
            _ => .16f
        };

        static float Smoothness(string surface) => surface switch
        {
            "metal" => .58f,
            "water" => .88f,
            "glass" => .8f,
            "ceramic" => .42f,
            "wood" => .26f,
            "roof" => .28f,
            "plaster" => .16f,
            _ => .08f
        };

        public static void Apply(Transform world)
        {
            foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>(true))
            {
                var original = renderer.sharedMaterial;
                if (!original || !original.name.StartsWith("Paint_")) continue;
                string surface = SurfaceFor(renderer.name);
                if (surface == null) continue;
                string tint = original.name.Substring(6);
                if (surface == "grass") tint = "#6C8151";
                if (surface == "soil") tint = "#6F5033";
                if (surface == "asphalt") tint = "#858478";
                if (surface == "water") tint = "#346D73";
                if (renderer.name == "StorageBase") tint = "#AAA99A";
                if (renderer.name == "StarterSelling") tint = "#B8AF97";
                renderer.sharedMaterial = Get(surface, tint);
            }
        }

        static string SurfaceFor(string name)
        {
            string n = name.ToLowerInvariant();
            if (n == "starterfield" || n.Contains("pen") && n.Contains("floor")) return "grass";
            if (n == "river") return "water";
            if (n == "storagebase") return "paving";
            if (n == "starterselling") return "paving";
            if (n == "grass" || n.Contains("hill")) return "grass";
            if (n.Contains("soil") || n.Contains("furrow") || n.Contains("field")) return "soil";
            if (n == "road" || n.Contains("customerstreet")) return "asphalt";
            if (n.Contains("wall")) return "plaster";
            if (n.Contains("floor") || n.Contains("mat") || n.Contains("pad")) return "paving";
            if (n.Contains("belt") || n.Contains("rail")) return "metal";
            if (n.Contains("post") || n.Contains("shelf") || n.Contains("board")) return "wood";
            if (n.Contains("roof")) return "roof";
            if (n.Contains("rock")) return "stone";
            return null;
        }
    }
}
