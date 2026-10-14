using UnityEngine;

namespace Tycoon
{
    public static class CityCommercialDetails
    {
        const string Trim = "#D4D0C2";
        const string Frame = "#43565B";
        const string Glass = "#789EA7";

        public static void Shell(CityStaticGeometry g, float width, float depth, float height, string wall)
        {
            g.Box(new(0, .18f, 0), new(width + .5f, .36f, depth + .5f), "stone", "#9B9E93");
            g.Box(new(0, height * .5f + .18f, 0), new(width, height, depth), "brick", wall);
            g.Collider("CommercialBody", new(0, height * .5f, 0), new(width, height, depth));
            g.Box(new(0, height + .25f, 0), new(width + .8f, .25f, depth + .8f), "stone", Trim);
            g.Box(new(0, height + .44f, 0), new(width + .3f, .12f, depth + .3f), "roof", Frame);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(new(side * (width * .5f - .18f), height * .5f, -depth * .5f - .05f),
                    new(.34f, height, .2f), "stone", Trim);
                g.Box(new(side * (width * .5f - .18f), height * .5f, depth * .5f + .05f),
                    new(.34f, height, .2f), "stone", Trim);
            }
        }

        public static void Window(CityStaticGeometry g, Vector3 p, float width, float height, float yaw = 0)
        {
            Quaternion r = Quaternion.Euler(0, yaw, 0);
            g.RotatedBox(p, new(width, height, .17f), r, "metal", Frame);
            g.RotatedBox(p + r * Vector3.forward * .1f, new(width - .18f, height - .18f, .025f),
                r, "glass", Glass);
            g.RotatedBox(p + r * Vector3.back * .1f, new(width - .18f, height - .18f, .025f),
                r, "glass", Glass);
            g.RotatedBox(p, new(.065f, height - .12f, .25f), r, "metal", Trim);
            g.RotatedBox(p + Vector3.down * (height * .5f + .075f), new(width + .24f, .15f, .4f),
                r, "stone", Trim);
        }

        public static void Doors(CityStaticGeometry g, float z, float height = 2.8f)
        {
            Window(g, new(0, height * .5f + .18f, z), 3.2f, height);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(new(side * .18f, 1.45f, z - .19f), new(.055f, .62f, .07f), "metal", Trim);
                g.Cylinder(new(side * 2.1f, .48f, z - 1.1f), .13f, .96f, "metal", Frame);
                g.Collider("EntranceBollard", new(side * 2.1f, .48f, z - 1.1f), new(.3f, .96f, .3f));
            }
            g.Box(new(0, .055f, z - .8f), new(3.5f, .08f, 1.2f), "fabric", "#606C63");
        }

        public static void Court(CityStaticGeometry g, float width, float depth)
        {
            g.Box(new(0, .025f, 0), new(width, .045f, depth), "paving", "#BBB5A5");
            for (int side = -1; side <= 1; side += 2)
            {
                CityProps.Planter(g, new(side * (width * .5f - 2), 0, -depth * .5f + 2), 2.3f);
                CityProps.Lamp(g, new(side * (width * .5f - 1), 0, depth * .5f - 1));
            }
        }

        public static void RoofPlant(CityStaticGeometry g, Vector3 p)
        {
            g.Box(p + Vector3.up * .42f, new(2.5f, .84f, 1.5f), "metal", "#8A9891");
            g.Cylinder(p + Vector3.up * .92f, .48f, .16f, "metal", Frame);
            for (int i = 0; i < 6; i++)
                g.Box(p + new Vector3(-1.05f + i * .42f, .42f, -.77f), new(.055f, .6f, .035f),
                    "metal", Frame);
        }

        public static void Banner(CityStaticGeometry g, Vector3 p, float width, string color)
        {
            g.Box(p, new(width, .72f, .18f), "metal", Frame);
            g.Box(p + Vector3.back * .11f, new(width - .2f, .54f, .045f), "metal", color);
            for (int side = -1; side <= 1; side += 2)
                g.Box(p + new Vector3(side * (width * .5f - .22f), 0, -.16f), new(.08f, .45f, .035f),
                    "metal", Trim);
        }

        public static void Chair(CityStaticGeometry g, Vector3 p, float yaw)
        {
            Quaternion r = Quaternion.Euler(0, yaw, 0);
            g.RotatedBox(p + Vector3.up * .48f, new(.62f, .1f, .7f), r, "wood", "#A48865");
            g.RotatedBox(p + r * new Vector3(0, .86f, .3f), new(.62f, .58f, .09f),
                r, "wood", "#A48865");
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    g.RotatedBox(p + r * new Vector3(x * .25f, .24f, z * .28f),
                        new(.075f, .48f, .075f), r, "metal", Frame);
            g.Collider("CafeChair", p + Vector3.up * .6f, new(.7f, 1.2f, .7f));
        }
    }
}
