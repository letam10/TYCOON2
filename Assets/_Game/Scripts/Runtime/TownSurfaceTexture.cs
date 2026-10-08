using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    // Kênh alpha giữ độ cao: shader lấy đạo hàm để tạo normal theo bề mặt thật.
    public static class TownSurfaceTexture
    {
        const int Size = 256;
        static readonly Dictionary<string, Texture2D> cache = new();

        public static Texture2D Get(string surface)
        {
            if (cache.TryGetValue(surface, out var found) && found) return found;
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size;
                    float v = y / (float)Size;
                    // Trộn bốn mẫu tuần hoàn để nối liền vân tại mép texture.
                    float n00 = Mathf.PerlinNoise(u * 8, v * 8);
                    float n10 = Mathf.PerlinNoise((u - 1) * 8, v * 8);
                    float n01 = Mathf.PerlinNoise(u * 8, (v - 1) * 8);
                    float n11 = Mathf.PerlinNoise((u - 1) * 8, (v - 1) * 8);
                    float broad = Mathf.Lerp(Mathf.Lerp(n00, n10, u), Mathf.Lerp(n01, n11, u), v);
                    float height = .5f;
                    float light = .9f;
                    switch (surface)
                    {
                        case "brick":
                            float row = Mathf.Floor(v * 8);
                            float bx = Mathf.Repeat(u * 4 + row % 2 * .5f, 1);
                            float by = Mathf.Repeat(v * 8, 1);
                            bool joint = bx < .045f || by < .09f;
                            height = joint ? .47f : .53f;
                            light = joint ? .88f : .91f + broad * .06f;
                            break;
                        case "wood":
                            float wave = Mathf.Sin(u * 96 + Mathf.Sin(v * 7) * 2 + broad);
                            float board = Mathf.Floor(u * 3);
                            bool gap = Mathf.Repeat(u * 3, 1) < .025f
                                || Mathf.Repeat(v + board % 2 * .33f, 1) < .016f;
                            height = gap ? .47f : .52f + wave * .008f;
                            light = gap ? .86f : .92f + wave * .02f + broad * .025f;
                            break;
                        case "paving":
                        case "ceramic":
                            // Mặt lát liền, tránh lưới ron dày gây rung khi camera di chuyển.
                            height = .49f + broad * .025f;
                            light = .93f + broad * .04f;
                            break;
                        case "roof":
                            height = .5f + Mathf.Sin(u * Mathf.PI * 12) * .04f;
                            light = .92f + height * .06f;
                            break;
                        case "glass":
                        case "metal":
                            height = .5f;
                            light = .97f + broad * .02f;
                            break;
                        case "grass":
                            height = .48f + broad * .04f;
                            light = .89f + broad * .09f;
                            break;
                        case "soil":
                        case "asphalt":
                            height = .48f + broad * .035f;
                            light = .92f + broad * .055f;
                            break;
                        default:
                            height = .49f + broad * .02f;
                            light = .93f + broad * .04f;
                            break;
                    }
                    pixels[y * Size + x] = new Color(light, light, light, height);
                }
            }
            if (surface == "grass") GrassBlades(pixels);
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false);
            texture.name = "TownDetail_" + surface;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            cache[surface] = texture;
            return texture;
        }

        static void GrassBlades(Color32[] pixels)
        {
            var random = new System.Random(80461);
            // Nét cỏ ngắn chỉ nằm trong texture, không thêm mesh hoặc collider lên nền.
            for (int blade = 0; blade < 3600; blade++)
            {
                int x = random.Next(Size);
                int y = random.Next(Size);
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float length = 2 + (float)random.NextDouble() * 6;
                bool dark = random.Next(3) == 0;
                var color = dark ? new Color(.73f, .80f, .65f, .46f)
                    : new Color(.96f, 1, .85f, .54f);
                for (int step = 0; step <= Mathf.CeilToInt(length); step++)
                {
                    float t = step / length;
                    int px = (Mathf.RoundToInt(x + Mathf.Cos(angle) * step) + Size) % Size;
                    int py = (Mathf.RoundToInt(y + Mathf.Sin(angle) * step) + Size) % Size;
                    int index = py * Size + px;
                    pixels[index] = Color.Lerp(pixels[index], color, (1 - t * .5f) * .65f);
                }
            }
        }

    }
}
