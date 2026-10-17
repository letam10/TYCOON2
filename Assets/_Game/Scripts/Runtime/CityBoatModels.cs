using UnityEngine;

namespace Tycoon
{
    public static class CityBoatModels
    {
        public static void Fishing(CityStaticGeometry g, Vector3 p)
        {
            Hull(g, p, new(.8f, .8f, .8f), "#447782");
            Cabin(g, p + new Vector3(0, .92f, -.9f), new(2.2f, 1.55f, 2.6f), "#D0C8B2");
            g.Box(p + new Vector3(0, 2.56f, -.9f), new(2.5f, .14f, 2.9f), "metal", "#5E7774");
            g.Cylinder(p + new Vector3(0, 3.15f, -.8f), .045f, 1.15f, "metal", "#667D78");
            g.Beam(p + new Vector3(-.7f, 3.45f, -.8f), p + new Vector3(.7f, 3.45f, -.8f),
                .06f, "metal", "#667D78");
            g.Box(p + new Vector3(-.7f, 1.06f, -2.85f), new(.9f, .3f, 1.1f), "wood", "#A69A79");
            g.Box(p + new Vector3(.7f, 1.15f, -2.8f), new(.65f, .5f, .8f), "metal", "#557975");
            g.Cylinder(p + new Vector3(0, 1.12f, 2.4f), .45f, .28f, "metal", "#82988E");
            Lifebuoy(g, p + new Vector3(1.2f, 1.85f, -.9f));
            Cleat(g, p + new Vector3(0, 1.01f, 3.8f));
        }

        public static void Yacht(CityStaticGeometry g, Vector3 p)
        {
            Hull(g, p, new(1.12f, .95f, 1.12f), "#DED8C9");
            Cabin(g, p + new Vector3(0, 1.1f, -.45f), new(3.3f, 1.55f, 3.8f), "#DED8C9");
            g.Box(p + new Vector3(0, 2.79f, -.55f), new(3.65f, .2f, 4.3f), "metal", "#D6D2C4");
            g.Box(p + new Vector3(0, 3.04f, -1.1f), new(2.4f, .3f, 2.2f), "fabric", "#AEB9B0");
            g.Box(p + new Vector3(0, 1.37f, 3.15f), new(2.3f, .35f, 2.2f), "fabric", "#C5C0AD");
            g.Box(p + new Vector3(0, 1.23f, -4.45f), new(4.1f, .16f, .65f), "wood", "#AC9775");
            g.Cylinder(p + new Vector3(0, 3.8f, -.4f), .07f, 1.4f, "metal", "#728781");
            g.Box(p + new Vector3(0, 4.35f, -.4f), new(1.6f, .1f, .3f), "metal", "#D6D2C4");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Cylinder(p + new Vector3(side * 1.42f, 1.55f, 3.52f), .055f, .08f,
                    "glass", side < 0 ? "#D8B85C" : "#D7E5D8");
                g.Beam(p + new Vector3(side * 1.55f, 1.56f, 2.05f),
                    p + new Vector3(side * 1.55f, 1.56f, 4.62f), .035f, "metal", "#83958D");
                for (int post = 0; post < 3; post++)
                    g.Cylinder(p + new Vector3(side * 1.55f, 1.35f, 2.22f + post * 1.1f),
                        .035f, .42f, "metal", "#83958D");
            }
            g.Beam(p + new Vector3(-1.55f, 1.56f, 4.62f),
                p + new Vector3(1.55f, 1.56f, 4.62f), .035f, "metal", "#83958D");
            g.Cylinder(p + new Vector3(0, 1.48f, 4.9f), .07f, .08f, "glass", "#E7D06E");
            Cleat(g, p + new Vector3(0, 1.22f, 5.6f));
            Lifebuoy(g, p + new Vector3(-1.73f, 1.98f, -1.2f));
        }

        public static void Cargo(CityStaticGeometry g, Vector3 p)
        {
            Hull(g, p, Vector3.one, "#3C667B");
            Cabin(g, p + new Vector3(0, 1.15f, -1.3f), new(3, 1.8f, 3.4f), "#D3CEC0");
            g.Box(p + new Vector3(0, 3.09f, -1.3f), new(3.4f, .16f, 3.8f), "metal", "#A7B8AF");
            g.Cylinder(p + new Vector3(0, 4.1f, -1.3f), .055f, 2, "metal", "#526C6C");
            g.Beam(p + new Vector3(-1, 4.6f, -1.3f), p + new Vector3(1, 4.6f, -1.3f),
                .055f, "metal", "#526C6C");
            for (int side = -1; side <= 1; side += 2)
            {
                Lifebuoy(g, p + new Vector3(side * 1.56f, 2.1f, -1.3f));
                Cleat(g, p + new Vector3(side * 1.7f, 1.28f, 3.8f));
            }
        }

        static void Hull(CityStaticGeometry g, Vector3 p, Vector3 scale, string color)
        {
            // Một thân kín theo tiết diện, không chắp hộp tạo mũi tàu lõm.
            g.Sculpt(CityHarborHull.Build(), p, scale, Quaternion.identity, "metal", color);
            g.Box(p + Vector3.Scale(new(0, 1.13f, 0), scale),
                Vector3.Scale(new(4.3f, .12f, 8.8f), scale), "wood", "#B5A17D");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(p + Vector3.Scale(new(side * 2.25f, 1.24f, 0), scale),
                    Vector3.Scale(new(.12f, .18f, 8.7f), scale), "metal", "#D4CCB7");
                for (int z = -4; z <= 4; z += 2)
                    g.Cylinder(p + Vector3.Scale(new(side * 2.18f, 1.5f, z), scale),
                        .025f, .6f * scale.y, "metal", "#83958D");
                Vector3 a = p + Vector3.Scale(new(side * 2.18f, 1.78f, -4), scale);
                Vector3 b = p + Vector3.Scale(new(side * 2.18f, 1.78f, 4), scale);
                Vector3 c = p + Vector3.Scale(new(0, 1.78f, 6.1f), scale);
                g.Beam(a, b, .035f, "metal", "#83958D");
                g.Beam(b, c, .035f, "metal", "#83958D");
            }
        }

        static void Cabin(CityStaticGeometry g, Vector3 bottom, Vector3 size, string color)
        {
            g.Box(bottom + Vector3.up * (size.y * .5f), size, "metal", color);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(bottom + new Vector3(side * (size.x * .5f + .018f), size.y * .7f, 0),
                    new(.04f, size.y * .4f, size.z * .76f), "glass", "#658E98");
                g.Box(bottom + new Vector3(side * (size.x * .5f + .045f), size.y * .7f, 0),
                    new(.04f, size.y * .42f, .055f), "metal", "#83958D");
            }
            g.Box(bottom + new Vector3(0, size.y * .7f, size.z * .5f + .02f),
                new(size.x * .86f, size.y * .4f, .045f), "glass", "#658E98");
            g.Box(bottom + new Vector3(0, size.y * .43f, -size.z * .5f - .025f),
                new(size.x * .3f, size.y * .76f, .055f), "metal", "#8D9C91");
            g.Box(bottom + new Vector3(size.x * .09f, size.y * .4f, -size.z * .5f - .07f),
                new(.04f, .2f, .04f), "metal", "#D4CCB7");
        }

        static void Lifebuoy(CityStaticGeometry g, Vector3 p)
        {
            // Bốn miếng phao tạo lỗ thật; tâm không bị bít bởi một đĩa đặc.
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                g.RotatedBox(p + new Vector3(0, Mathf.Cos(a) * .25f, Mathf.Sin(a) * .25f),
                    new(.11f, .14f, .18f), Quaternion.Euler(i * 30, 0, 0), "fabric",
                    i % 3 == 0 ? "#E0D8C2" : "#B96D49");
            }
        }

        static void Cleat(CityStaticGeometry g, Vector3 p)
        {
            g.Box(p, new(.3f, .08f, .15f), "metal", "#83958D");
            g.Box(p + Vector3.up * .12f, new(.4f, .05f, .08f), "metal", "#83958D");
            g.Box(p + Vector3.up * .06f, new(.06f, .12f, .06f), "metal", "#83958D");
        }
    }
}
