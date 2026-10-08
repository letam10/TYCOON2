using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class WorldTypography
    {
        static readonly Dictionary<Font, Material> materials = new();
        static bool subscribed;

        public static Material Material(Font font)
        {
            if (!font) return null;
            if (materials.TryGetValue(font, out var existing) && existing) return existing;
            var shader = Resources.Load<Shader>("VisualRedesign/WorldLabel");
            if (!shader) return font.material;
            var material = new Material(shader)
            {
                name = "OutlinedWorldFont_" + font.name,
                mainTexture = font.material.mainTexture
            };
            materials[font] = material;
            if (!subscribed)
            {
                Font.textureRebuilt += RefreshTexture;
                subscribed = true;
            }
            return material;
        }

        static void RefreshTexture(Font font)
        {
            if (materials.TryGetValue(font, out var material) && material)
                material.mainTexture = font.material.mainTexture;
        }

        public static float LinePixels(Camera camera, bool quantity)
        {
            float resolution = Mathf.Clamp(camera.pixelHeight / 1080f, .75f, 2);
            return (quantity ? 23 : 20) * resolution;
        }
    }
}
