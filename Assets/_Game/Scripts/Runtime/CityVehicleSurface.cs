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
                paint.SetFloat("_Metallic", .72f);
            if (paint.HasProperty("_Smoothness"))
                paint.SetFloat("_Smoothness", .74f);
            if (paint.HasProperty("_CoatWeight"))
                paint.SetFloat("_CoatWeight", .4f);
            if (paint.HasProperty("_CoatRoughness"))
                paint.SetFloat("_CoatRoughness", .18f);
            if (paint.HasProperty("_IrregularDetail"))
                paint.SetFloat("_IrregularDetail", 1);
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
                glass.SetColor("_BaseColor", new Color(.18f, .37f, .43f, .3f));
                if (glass.HasProperty("_BaseMap"))
                    glass.SetTexture("_BaseMap", TownSurfaceTexture.Get("glass"));
                glass.SetFloat("_Metallic", .05f);
                glass.SetFloat("_Smoothness", .93f);
                if (glass.HasProperty("_CoatWeight"))
                    glass.SetFloat("_CoatWeight", .45f);
                if (glass.HasProperty("_CoatRoughness"))
                    glass.SetFloat("_CoatRoughness", .12f);
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
