using UnityEngine;

namespace Tycoon
{
    public static class CityBuildingDetails
    {
        public static void PitchedRoof(CityStaticGeometry g, Vector3 p, float width, float depth,
            float height, string roof, string wall)
        {
            float half = depth * .5f + .4f;
            float rise = depth * .22f;
            float length = Mathf.Sqrt(half * half + rise * rise);
            float angle = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;
            for (int side = -1; side <= 1; side += 2)
            {
                var rotation = Quaternion.Euler(side * angle, 0, 0);
                Vector3 center = p + new Vector3(0, height + rise * .5f, side * half * .5f);
                // Một mặt mái kín; gờ ngói chỉ là chi tiết phủ bên trên.
                g.RotatedBox(center, new(width + .8f, .18f, length + .06f), rotation, "roof", roof);
                for (int course = 1; course < 7; course++)
                {
                    float t = course / 7f;
                    Vector3 seam = p + new Vector3(0, height + rise * t, side * half * (1 - t));
                    seam += rotation * Vector3.up * .1f;
                    g.RotatedBox(seam, new(width + .8f, .035f, .055f), rotation, "roof", roof);
                }
                g.Box(p + new Vector3(0, height - .09f, side * half), new(width + .9f, .18f, .17f),
                    "wood", "#E1DFCC");
                g.Box(p + new Vector3(0, height - .18f, side * (half + .1f)),
                    new(width + 1, .12f, .16f), "metal", "#647D7B");
                for (int end = -1; end <= 1; end += 2)
                    g.Beam(p + new Vector3(end * (width * .5f + .24f), height, side * half),
                        p + new Vector3(end * (width * .5f + .24f), height + rise, 0),
                        .19f, "wood", "#E1DFCC");
            }
            g.Box(p + Vector3.up * (height + rise + .06f), new(width + 1, .16f, .2f), "roof", roof);
            // Pignon kín dưới mái, các hàng chồng nhẹ để không tạo khe sáng.
            for (int row = 0; row < 16; row++)
            {
                float y = rise * (row + .5f) / 16;
                float span = Mathf.Min(depth, 2 * half * (1 - row / 16f));
                for (int side = -1; side <= 1; side += 2)
                    g.Box(p + new Vector3(side * (width * .5f - .05f), height + y, 0),
                        new(.1f, rise / 16 + .015f, span), "plaster", wall);
            }
        }

        public static void Facade(CityStaticGeometry g, Vector3 p, float width, float depth,
            float height, string accent, bool porch)
        {
            float front = -depth * .5f;
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(p + new Vector3(side * (width * .5f - .1f), (height + .4f) * .5f, front - .06f),
                    new(.24f, height - .4f, .14f), "stone", "#C2BDA8");
                g.Box(p + new Vector3(side * (width * .5f + .12f), height * .5f + .2f, front - .23f),
                    new(.1f, height - .4f, .1f), "metal", "#647D7B");
                g.Box(p + new Vector3(0, .56f, side * depth * .5f), new(width, .28f, .13f),
                    "brick", "#9B8873");
            }
            for (int level = 0; level < (height > 6 ? 2 : 1); level++)
            {
                for (float x = -width * .5f + 1.8f; x < width * .5f - 1; x += 3.2f)
                {
                    if (level == 0 && Mathf.Abs(x) < 2.1f) continue;
                    Window(g, p + new Vector3(x, 2.1f + level * 2.7f, front - .09f), accent);
                }
            }
            g.Box(p + new Vector3(0, 1.66f, front - .08f), new(1.85f, 2.52f, .16f),
                "stone", "#E1DFCC");
            g.Box(p + new Vector3(0, 1.61f, front - .19f), new(1.5f, 2.42f, .12f), "wood", accent);
            for (int panel = 0; panel < 2; panel++)
                g.Box(p + new Vector3(0, 1.03f + panel * 1.12f, front - .27f), new(1.15f, .88f, .055f),
                    panel == 0 ? "wood" : "glass", panel == 0 ? "#9C8463" : "#647D7B");
            g.Box(p + new Vector3(.54f, 1.52f, front - .32f), new(.065f, .24f, .09f),
                "metal", "#C0A66B");
            if (!porch) return;
            Vector3 step = p + new Vector3(0, .23f, front - .82f);
            g.Box(step, new(2.5f, .34f, 1.55f), "stone", "#AAA89A");
            g.Collider("PorchStep", step, new(2.5f, .34f, 1.55f));
            g.RotatedBox(p + new Vector3(0, 3.05f, front - .72f), new(2.7f, .13f, 1.8f),
                Quaternion.Euler(-9, 0, 0), "roof", accent);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 post = p + new Vector3(side * 1.08f, 1.7f, front - 1.35f);
                g.Box(post, new(.14f, 2.6f, .14f), "wood", "#E1DFCC");
                g.Collider("PorchPost", post, new(.14f, 2.6f, .14f));
            }
        }

        static void Window(CityStaticGeometry g, Vector3 p, string accent)
        {
            g.Box(p, new(1.8f, 1.8f, .16f), "wood", "#E1DFCC");
            g.Box(p + Vector3.back * .1f, new(1.5f, 1.5f, .1f), "glass", "#647D7B");
            g.Box(p + Vector3.back * .18f, new(.08f, 1.5f, .06f), "wood", "#E1DFCC");
            g.Box(p + Vector3.back * .18f, new(1.5f, .08f, .06f), "wood", "#E1DFCC");
            g.Box(p + new Vector3(0, -.94f, -.1f), new(2, .14f, .37f), "stone", "#C2BDA8");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(p + new Vector3(side * 1.15f, 0, -.02f), new(.4f, 1.75f, .12f), "wood", accent);
                for (int slat = 0; slat < 6; slat++)
                    g.Box(p + new Vector3(side * 1.15f, -.68f + slat * .27f, -.095f),
                        new(.32f, .035f, .045f), "wood", "#9C8463");
            }
        }
    }
}
