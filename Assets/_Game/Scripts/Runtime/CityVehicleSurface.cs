using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class CityVehicleSurface
    {
        static Material glass;
        static readonly Dictionary<string, Material> paintCache = new();

        public static Material Paint(string tint)
        {
            if (paintCache.TryGetValue(tint, out Material cached) && cached)
                return cached;

            Material source = TownSurfaceMaterials.Get("metal", tint);
            Material paint = new Material(source);
            paint.name = "VehiclePaint_" + tint;
            paint.enableInstancing = true;
            if (paint.HasProperty("_Metallic"))
                paint.SetFloat("_Metallic", .64f);
            if (paint.HasProperty("_Smoothness"))
                paint.SetFloat("_Smoothness", .68f);
            if (paint.HasProperty("_CoatWeight"))
                paint.SetFloat("_CoatWeight", .28f);
            if (paint.HasProperty("_CoatRoughness"))
                paint.SetFloat("_CoatRoughness", .22f);
            paintCache[tint] = paint;
            return paint;
        }
        public static Material Rubber => TownSurfaceMaterials.Get("fabric", "#252A2E");
        public static Material Chrome => TownSurfaceMaterials.Get("metal", "#B5C1C5");
        public static Material Dark => TownSurfaceMaterials.Get("metal", "#27363C");
        public static Material Seat => TownSurfaceMaterials.Get("fabric", "#527888");
        public static Material WarmSeat => TownSurfaceMaterials.Get("fabric", "#AB8160");
        public static Material Light => TownSurfaceMaterials.Get("glass", "#FFF2C7");
        public static Material Red => TownSurfaceMaterials.Get("glass", "#B83930");
        public static Material Stripe => TownSurfaceMaterials.Get("metal", "#D8AC4F");

        public static Material Glass
        {
            get
            {
                if (glass) return glass;
                glass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                glass.name = "VehicleV8SharedClearGlass";
                glass.SetColor("_BaseColor", new Color(.31f, .52f, .59f, .23f));
                glass.SetFloat("_Smoothness", .84f);
                glass.SetFloat("_Surface", 1);
                glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                glass.SetFloat("_ZWrite", 0);
                glass.SetFloat("_Cull", (float)CullMode.Off);
                glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                glass.renderQueue = 3000;
                glass.enableInstancing = true;
                return glass;
            }
        }
    }
}
