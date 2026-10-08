using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    internal static class HandItemSurfaceAtlas
    {
        const int Tile = 64;
        const int Grid = 16;
        const int Edge = Tile * Grid;
        static readonly Dictionary<string, Rect> tiles = new();
        static Texture2D albedo;
        static Texture2D normal;
        static Texture2D metallic;
        static Material material;
        static bool dirty;

        public static Material Material
        {
            get
            {
                if (material) return material;
                albedo = Texture("Carry Albedo", false);
                normal = Texture("Carry Normal", true);
                metallic = Texture("Carry Surface", true);
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = "Carry Shared Textured Atlas",
                    enableInstancing = true
                };
                material.SetTexture("_BaseMap", albedo);
                material.SetTexture("_BumpMap", normal);
                material.SetTexture("_MetallicGlossMap", metallic);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_BumpScale", .55f);
                material.SetFloat("_Smoothness", 1);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                return material;
            }
        }

        static Texture2D Texture(string name, bool linear) => new(Edge, Edge, TextureFormat.RGBA32, true, linear)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 2
        };

        public static string Kind(string name, string fallback)
        {
            string value = name.ToLowerInvariant();
            if (value.Contains("label") || value.Contains("banknote") || value.Contains("paper")) return "paper";
            if (value.Contains("wood") || value.Contains("board") || value.Contains("crate") ||
                value.Contains("brace") || value.Contains("whisk")) return "wood";
            if (value.Contains("woven") || value.Contains("cloth") || value.Contains("thread") ||
                value.Contains("wool") || value.Contains("twine") || value.Contains("seam")) return "fiber";
            if (value.Contains("crust") || value.Contains("loaf") || value.Contains("cake base")) return "baked";
            if (value.Contains("beef") || value.Contains("steak")) return "meat";
            if (value.Contains("leaf") || value.Contains("husk") || value.Contains("calyx")) return "leaf";
            return fallback;
        }

        public static Rect TileFor(string color, string surface)
        {
            _ = Material;
            string key = surface + color;
            if (tiles.TryGetValue(key, out var rect)) return rect;
            int slot = tiles.Count;
            if (slot >= Grid * Grid) throw new System.InvalidOperationException("Carry atlas đầy.");
            int left = slot % Grid * Tile;
            int bottom = slot / Grid * Tile;
            // Viền 8 pixel giữ màu/normal ổn định ở mip xa và ranh giới UV.
            rect = new Rect((left + 8.5f) / Edge, (bottom + 8.5f) / Edge, 47f / Edge, 47f / Edge);
            Color tint = Art.Hex(color);
            var colors = new Color[Tile * Tile];
            var normals = new Color[Tile * Tile];
            var surfaces = new Color[Tile * Tile];
            float smoothness = surface == "glass" ? .78f : surface == "metal" ? .48f :
                surface == "food" || surface == "meat" ? .34f : .13f;
            for (int y = 0; y < Tile; y++)
                for (int x = 0; x < Tile; x++)
                {
                    float u = Mathf.Clamp(x, 8, 55) / 48f;
                    float v = Mathf.Clamp(y, 8, 55) / 48f;
                    float height = Height(u, v, surface);
                    float dx = Height(u + .012f, v, surface) - Height(u - .012f, v, surface);
                    float dy = Height(u, v + .012f, surface) - Height(u, v - .012f, surface);
                    var n = new Vector3(-dx * 2, -dy * 2, 1).normalized;
                    float shade = .87f + height * .23f;
                    int index = y * Tile + x;
                    colors[index] = new Color(tint.r * shade, tint.g * shade, tint.b * shade, 1);
                    normals[index] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                    surfaces[index] = new Color(surface == "metal" ? .75f : 0, 0, 0,
                        Mathf.Clamp01(smoothness + (height - .5f) * .1f));
                }
            albedo.SetPixels(left, bottom, Tile, Tile, colors);
            normal.SetPixels(left, bottom, Tile, Tile, normals);
            metallic.SetPixels(left, bottom, Tile, Tile, surfaces);
            tiles.Add(key, rect);
            dirty = true;
            return rect;
        }

        static float Height(float u, float v, string surface)
        {
            float fine = Mathf.PerlinNoise(u * 39 + 17, v * 39 + 5);
            if (surface == "fiber") return .5f + .22f * Mathf.Sin(u * 95) * Mathf.Sin(v * 95);
            if (surface == "wood") return .5f + .25f * Mathf.Sin(u * 65 + Mathf.Sin(v * 11) * 3);
            if (surface == "meat")
                return Mathf.Pow(.5f + .5f * Mathf.Sin(u * 24 + Mathf.Sin(v * 17) * 2), 9);
            if (surface == "leaf") return .4f + .3f * Mathf.Abs(Mathf.Sin((u + Mathf.Abs(v - .5f)) * 28));
            if (surface == "baked") return fine * .6f + Mathf.PerlinNoise(u * 9, v * 9) * .4f;
            if (surface == "metal" || surface == "glass") return .5f + (fine - .5f) * .18f;
            return fine;
        }

        public static void Apply()
        {
            if (!dirty) return;
            albedo.Apply(true, false);
            normal.Apply(true, false);
            metallic.Apply(true, false);
            dirty = false;
        }
    }
}
