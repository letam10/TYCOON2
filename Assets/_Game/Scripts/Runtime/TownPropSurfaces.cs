using UnityEngine;

namespace Tycoon
{
    public static class TownPropSurfaces
    {
        public static void Apply(GameObject model, string key)
        {
            bool machine = key is "mill" or "cheesemaker" or "saucemaker" or "feedmill" or "soyextractor"
                or "milkbottler" or "spinner" or "loom" or "breadmixer" or "cakemixer"
                or "oven_asset" or "cooking_range" or "extractor" or "cold_cabinet";
            bool furniture = key is "storage_rack" or "checkout_counter" or "display_shelf" or "display_counter"
                or "prep_table" or "kitchen_sink" or "dining_table" or "dining_chair";
            bool barn = key is "farm_shelter" or "coop";
            if (!machine && !furniture && !barn) return;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var old = materials[i];
                    if (!old || old.name.Contains("BrandDecal")) continue;
                    Color color = old.HasProperty("_BaseColor") ? old.GetColor("_BaseColor") : old.color;
                    Color.RGBToHSV(color, out float hue, out float saturation, out float value);
                    bool dark = value < .22f;
                    string surface = machine ? "metal" : "wood";
                    string tint = dark ? "#48524D" : "#A9B4AD";
                    if (machine && saturation > .38f) tint = "#768D86";
                    if (machine && hue < .16f && saturation > .38f) tint = "#ACA28A";
                    if (key is "loom" or "spinner")
                    {
                        surface = "wood";
                        tint = dark ? "#5F5746" : "#AD936C";
                    }
                    if (furniture || barn)
                    {
                        tint = dark ? "#555D50" : "#AD9571";
                        if (saturation < .15f && value > .6f)
                        {
                            surface = "ceramic";
                            tint = "#CBC8B9";
                        }
                    }
                    if (key is "extractor" or "kitchen_sink")
                    {
                        surface = "metal";
                        tint = "#BCC3BD";
                    }
                    materials[i] = TownSurfaceMaterials.Get(surface, tint);
                }
                renderer.sharedMaterials = materials;
            }
        }
    }
}
