using UnityEngine;

namespace Tycoon
{
    public static class CityLeisureModels
    {
        const string Metal = "#526C6C";
        const string Wood = "#AB8158";

        public static void Playground(CityStaticGeometry g)
        {
            g.Box(new(0, .02f, 0), new(37, .04f, 29), "paving", "#C0B6A0");
            g.Box(new(-7, .055f, 0), new(17, .05f, 20), "fabric", "#839784");
            g.Box(new(10, .055f, 0), new(14, .05f, 20), "fabric", "#B59A76");
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 a = new(-8 + side * 4, .1f, -3);
                Vector3 b = new(-8 + side * 4, 4.4f, 0);
                Vector3 c = new(-8 + side * 4, .1f, 3);
                g.Beam(a, b, .2f, "metal", Metal);
                g.Beam(c, b, .2f, "metal", Metal);
                g.Beam(Vector3.Lerp(a, b, .4f), Vector3.Lerp(c, b, .4f), .14f, "metal", Metal);
                foreach (Vector3 foot in new[] { a, c })
                    g.Box(new(foot.x, .055f, foot.z), new(.5f, .11f, .5f), "metal", Metal);
                g.Collider("SwingFrameFoot", a + Vector3.up * .8f, new(.4f, 1.6f, .4f));
                g.Collider("SwingFrameFoot", c + Vector3.up * .8f, new(.4f, 1.6f, .4f));
            }
            g.Beam(new(-12, 4.4f, 0), new(-4, 4.4f, 0), .22f, "metal", Metal);
            foreach (float x in new[] { -10f, -6f })
            {
                g.Box(new(x, .65f, 0), new(1.45f, .14f, .65f), "wood", Wood);
                for (int side = -1; side <= 1; side += 2)
                {
                    g.Beam(new(x + side * .62f, .7f, 0), new(x + side * .62f, 4.3f, 0),
                        .035f, "metal", Metal);
                    g.Box(new(x + side * .62f, 4.3f, 0), new(.12f, .25f, .13f), "metal", Metal);
                }
                g.Collider("SwingSeat", new(x, .65f, 0), new(1.45f, .3f, .65f));
            }
            TowerSlide(g, new(8, 0, 1));
            for (int side = -1; side <= 1; side += 2)
            {
                CityProps.Bench(g, new(side * 8, 0, -12));
                CityProps.Planter(g, new(side * 16, 0, 11), 3);
                CityProps.Lamp(g, new(side * 17, 0, -11));
            }
        }

        static void TowerSlide(CityStaticGeometry g, Vector3 p)
        {
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 post = p + new Vector3(x * 1.2f, 2, z * 1.2f);
                    g.Box(post, new(.18f, 4, .18f), "wood", Wood);
                    g.Collider("PlayTowerPost", post, new(.2f, 4, .2f));
                    g.Box(p + new Vector3(x * 1.2f, .09f, z * 1.2f), new(.32f, .18f, .32f),
                        "metal", Metal);
                }
            g.Box(p + Vector3.up * 2.2f, new(2.8f, .2f, 2.8f), "wood", Wood);
            g.Cone(p + Vector3.up * 4.3f, 2.3f, 1.1f, "roof", "#7089A1");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(p + new Vector3(side * 1.28f, 2.85f, 0), new(.1f, .12f, 2.5f), "wood", Wood);
                g.Box(p + new Vector3(side * 1.28f, 2.43f, 0), new(.1f, .1f, 2.5f), "wood", Wood);
                for (int bar = -2; bar <= 2; bar++)
                    g.Box(p + new Vector3(side * 1.28f, 2.65f, bar * .42f), new(.07f, .5f, .07f),
                        "wood", Wood);
                g.Beam(p + new Vector3(side * 1.2f, 1.6f, -1.2f),
                    p + new Vector3(side * 1.2f, 2.1f, -.6f), .13f, "wood", Wood);
                g.Beam(p + new Vector3(side * .75f, .1f, -5),
                    p + new Vector3(side * .75f, 2.15f, -1.3f), .12f, "metal", Metal);
            }
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                g.Box(p + new Vector3(0, Mathf.Lerp(.12f, 2.05f, t), Mathf.Lerp(-5, -1.5f, t)),
                    new(1.5f, .1f, .24f), "wood", Wood);
            }
            Vector3 top = p + new Vector3(0, 2.2f, 1.35f);
            Vector3 low = p + new Vector3(0, .25f, 6.2f);
            Quaternion slope = Quaternion.LookRotation(low - top);
            g.RotatedBox((top + low) * .5f, new(1.6f, .1f, Vector3.Distance(top, low)),
                slope, "metal", "#B6C6CA");
            for (int side = -1; side <= 1; side += 2)
            {
                g.RotatedBox((top + low) * .5f + new Vector3(side * .85f, .18f, 0),
                    new(.12f, .35f, Vector3.Distance(top, low)), slope, "metal", "#6D9991");
                g.Box(p + new Vector3(side * .62f, .1f, 6.1f), new(.15f, .2f, .35f), "metal", Metal);
                g.Box(p + new Vector3(side * .62f, .67f, 4), new(.12f, 1.34f, .12f), "metal", Metal);
            }
            g.Box(p + new Vector3(0, .19f, 6.45f), new(1.7f, .14f, .6f), "metal", "#B6C6CA");
            // Khối chơi trang trí chưa leo được; collider bao đúng phần tháp và máng trượt.
            g.Collider("PlayTower", p + Vector3.up * 1.1f, new(2.8f, 2.2f, 2.8f));
            g.Collider("Slide", p + new Vector3(0, .75f, 3.85f), new(1.8f, 1.5f, 5));
        }

        public static void Monument(CityStaticGeometry g)
        {
            g.Cylinder(new(0, .035f, 0), 13.5f, .07f, "paving", "#C7BEA8");
            g.Cylinder(new(0, .2f, 0), 4.2f, .3f, "stone", "#929D96");
            g.Cylinder(new(0, .47f, 0), 3.6f, .24f, "stone", "#B8BEAB");
            g.Box(new(0, 1.5f, 0), new(3.7f, 1.8f, 3.7f), "stone", "#9DA89E");
            g.Box(new(0, 2.46f, 0), new(4.1f, .12f, 4.1f), "stone", "#D2C9AF");
            g.Box(new(0, 1.55f, -1.87f), new(2.2f, .8f, .08f), "metal", "#786848");
            g.Box(new(0, .67f, 0), new(3.95f, .14f, 3.95f), "stone", "#D2C9AF");
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = -1; row <= 1; row += 2)
                    g.Box(new(side * .94f, 1.55f + row * .29f, -1.918f), new(.065f, .065f, .025f),
                        "metal", "#937B4E");
                for (int end = -1; end <= 1; end += 2)
                    g.Box(new(side * 1.72f, 1.56f, end * 1.72f), new(.28f, 1.66f, .28f),
                        "stone", "#D2C9AF");
            }
            // Ba nhánh đồng hướng lên, biểu tượng trồng trọt và phát triển thành phố.
            for (int branch = -1; branch <= 1; branch++)
            {
                g.Beam(new(0, 2.5f, 0), new(branch * 1.6f, 6.7f - Mathf.Abs(branch), 0),
                    .4f, "metal", "#937B4E");
                g.Cone(new(branch * 1.6f, 6.8f - Mathf.Abs(branch), 0), .38f, 1.2f, "metal", "#937B4E");
            }
            g.Collider("MonumentStep", new(0, .2f, 0), new(8.2f, .3f, 1.8f));
            g.Collider("MonumentStep", new(0, .2f, 0), new(7.2f, .3f, 4));
            g.Collider("MonumentStep", new(0, .2f, 0), new(4, .3f, 7.2f));
            g.Collider("MonumentStep", new(0, .2f, 0), new(1.8f, .3f, 8.2f));
            g.Collider("Monument", new(0, 3.7f, 0), new(4.2f, 7.4f, 4.2f));
            for (int side = -1; side <= 1; side += 2)
            {
                CityProps.Bench(g, new(side * 8, 0, -5));
                CityProps.Planter(g, new(side * 8, 0, 5), 4);
                CityProps.Lamp(g, new(side * 10, 0, 0));
            }
        }

        public static void Signal(CityStaticGeometry g, Vector3 p)
        {
            g.Box(p + Vector3.up * .08f, new(.42f, .16f, .42f), "stone", "#AAA89A");
            g.Cylinder(p + Vector3.up * 2, .09f, 4, "metal", Metal);
            for (int bracket = 0; bracket < 2; bracket++)
                g.Box(p + new Vector3(0, 3.08f + bracket * 1.04f, -.08f), new(.15f, .1f, .3f),
                    "metal", Metal);
            g.Box(p + new Vector3(0, 3.6f, -.17f), new(.52f, 1.45f, .35f), "metal", "#35474B");
            for (int lamp = 0; lamp < 3; lamp++)
            {
                string tint = lamp == 0 ? "#C06E50" : lamp == 1 ? "#9C9257" : "#76AF88";
                Vector3 lens = p + new Vector3(0, 4.05f - lamp * .44f, -.365f);
                g.Box(lens, new(.27f, .18f, .055f), "glass", tint);
                g.Box(lens, new(.18f, .27f, .055f), "glass", tint);
                g.Box(p + new Vector3(0, 4.23f - lamp * .44f, -.39f), new(.4f, .05f, .2f),
                    "metal", "#35474B");
                for (int side = -1; side <= 1; side += 2)
                    g.Box(lens + new Vector3(side * .2f, .075f, -.025f), new(.035f, .18f, .15f),
                        "metal", "#35474B");
            }
            g.Box(p + new Vector3(0, 2.4f, 0), new(1.5f, .48f, .08f), "metal", "#557E70");
            g.Beam(p + new Vector3(-.5f, 2.4f, -.06f), p + new Vector3(.5f, 2.4f, -.06f),
                .06f, "metal", "#E1DBC2");
            g.Beam(p + new Vector3(0, 4.18f, 0), p + new Vector3(0, 4.18f, -2.25f),
                .1f, "metal", Metal);
            g.Box(p + new Vector3(0, 4.18f, -2.35f), new(.52f, 1.45f, .35f),
                "metal", "#35474B");
            g.Collider("TrafficSignal", p + Vector3.up * 2, new(.22f, 4, .22f));
        }

        public static void Railing(CityStaticGeometry g, Vector3 a, Vector3 b)
        {
            float length = Vector3.Distance(a, b);
            int spans = Mathf.Max(1, Mathf.CeilToInt(length / 2));
            for (int post = 0; post <= spans; post++)
            {
                Vector3 p = Vector3.Lerp(a, b, post / (float)spans);
                g.Box(p + Vector3.up * .045f, new(.22f, .09f, .22f), "metal", Metal);
                g.Cylinder(p + Vector3.up * .6f, .045f, 1.2f, "metal", Metal);
                g.Cylinder(p + Vector3.up * 1.2f, .065f, .07f, "metal", Metal);
            }
            for (int picket = 1; picket < spans * 4; picket++)
            {
                Vector3 p = Vector3.Lerp(a, b, picket / (spans * 4f));
                g.Beam(p + Vector3.up * .45f, p + Vector3.up * 1.15f, .035f, "metal", Metal);
            }
            foreach (float y in new[] { .45f, 1.15f })
                g.Beam(a + Vector3.up * y, b + Vector3.up * y, .055f, "metal", Metal);
            Vector3 size = new(Mathf.Abs(b.x - a.x) + .1f, 1.2f, Mathf.Abs(b.z - a.z) + .1f);
            g.Collider("Railing", (a + b) * .5f + Vector3.up * .6f, size);
        }
    }
}
