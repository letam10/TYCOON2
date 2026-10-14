using UnityEngine;

namespace Tycoon
{
    public static class CityCivicModels
    {
        const string Trim = "#D5D1C4";
        const string Metal = "#465B60";
        const string Glass = "#72949B";

        public static void Police(CityStaticGeometry g)
        {
            Shell(g, 24, 18, 6.5f, "#A7B8B9", "#345C78");
            g.Box(new(0, 7.65f, 4), new(8, 2.3f, 6), "plaster", "#A7B8B9");
            g.Box(new(0, 8.88f, 4), new(8.5f, .16f, 6.5f), "metal", Metal);
            g.Collider("PoliceRoofTower", new(0, 7.65f, 4), new(8, 2.3f, 6));
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(new(side * 2.1f, 7.8f, .94f), new(2.5f, 1.1f, .15f), "metal", Trim);
                g.Box(new(side * 2.1f, 7.8f, .85f), new(2.24f, .84f, .04f), "glass", Glass);
            }
            g.Cylinder(new(0, 9.7f, 4), .06f, 1.5f, "metal", Metal);
            g.Beam(new(-.8f, 10.05f, 4), new(.8f, 10.05f, 4), .08f, "metal", Metal);
            for (int side = -1; side <= 1; side += 2)
                g.Beam(new(side * .65f, 8.96f, 4), new(0, 9.45f, 4), .055f, "metal", Metal);
            g.RotatedBox(new(0, 5.3f, -9.23f), new(1.1f, 1.1f, .12f),
                Quaternion.Euler(0, 0, 45), "metal", "#C7AB65");
            g.Box(new(0, 5.3f, -9.32f), new(.34f, .65f, .08f), "metal", "#345C78");
            Forecourt(g, 28, 27);
            g.Box(new(0, 3.6f, -10), new(5.2f, .2f, 2), "metal", Metal);
            for (int side = -1; side <= 1; side += 2)
                g.Beam(new(side * 2.2f, 2.8f, -9.1f), new(side * 2.2f, 3.5f, -10.7f),
                    .12f, "metal", Metal);
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 p = new(i * 10, 0, -12);
                g.Cylinder(p + Vector3.up * .45f, .14f, .9f, "metal", Metal);
                g.Collider("PoliceBollard", p + Vector3.up * .45f, new(.28f, .9f, .28f));
            }
        }

        public static void Fire(CityStaticGeometry g)
        {
            Shell(g, 28, 18, 7, "#B98167", "#AD4C3E", false);
            for (int bay = -1; bay <= 1; bay++)
            {
                float x = bay * 8;
                g.Box(new(x, 2.3f, -9.12f), new(6.4f, 4.5f, .2f), "metal", "#963D33");
                for (int slat = 0; slat < 10; slat++)
                    g.Box(new(x, .25f + slat * .44f, -9.24f), new(6.25f, .035f, .025f), "metal", Metal);
                g.Box(new(x, 3.15f, -9.27f), new(5.2f, .65f, .045f), "glass", Glass);
                g.Box(new(x, 2.3f, -9.31f), new(.12f, 4.5f, .07f), "metal", Trim);
                for (int side = -1; side <= 1; side += 2)
                    g.Box(new(x + side * 3.35f, 2.4f, -9.2f), new(.25f, 4.8f, .4f), "stone", Trim);
                g.Box(new(x, 4.85f, -9.2f), new(6.95f, .2f, .4f), "stone", Trim);
                g.Box(new(x, .8f, -9.34f), new(.7f, .08f, .09f), "metal", Trim);
                g.Box(new(x, .055f, -12.7f), new(6.7f, .04f, 6.8f), "asphalt", "#707771");
            }
            g.Box(new(0, 5.8f, -9.24f), new(1.1f, .34f, .12f), "metal", "#EEE3C7");
            g.Box(new(0, 5.8f, -9.25f), new(.34f, 1.1f, .13f), "metal", "#EEE3C7");
            Forecourt(g, 33, 33);
            g.Cylinder(new(15, .5f, -11), .22f, 1, "metal", "#B84D38");
            g.Beam(new(14.55f, .65f, -11), new(15.45f, .65f, -11), .22f, "metal", "#B84D38");
            g.Cylinder(new(15, .1f, -11), .32f, .2f, "metal", Metal);
            g.Cone(new(15, 1.07f, -11), .25f, .18f, "metal", "#B84D38");
            g.Box(new(15, .63f, -11.24f), new(.3f, .3f, .1f), "metal", Trim);
            g.Collider("FireHydrant", new(15, .5f, -11), new(.9f, 1, .5f));
        }

        public static void Bar(CityStaticGeometry g)
        {
            Shell(g, 20, 14, 4.8f, "#D4BD96", "#447F78");
            g.Box(new(0, 3.2f, -8.7f), new(21, .2f, 3.4f), "roof", "#436963");
            for (int x = -9; x <= 9; x += 18)
            {
                g.Box(new(x, 1.6f, -9.9f), new(.16f, 3.2f, .16f), "wood", "#836549");
                g.Collider("BarCanopyPost", new(x, 1.6f, -9.9f), new(.2f, 3.2f, .2f));
                g.Box(new(x, .08f, -9.9f), new(.32f, .16f, .32f), "stone", Trim);
                g.Beam(new(x, 2.4f, -9.9f), new(x, 3.1f, -9), .12f, "wood", "#836549");
            }
            g.Box(new(0, 3.03f, -10.36f), new(21, .3f, .12f), "wood", "#836549");
            for (int rib = -4; rib <= 4; rib++)
                g.Box(new(rib * 2.2f, 3.08f, -8.7f), new(.1f, .15f, 3.3f), "wood", "#836549");
            g.Cylinder(new(0, 5.14f, -6), .38f, .08f, "metal", Trim);
            g.Cylinder(new(0, 5.35f, -6), .08f, .36f, "metal", Trim);
            g.Cylinder(new(0, 5.8f, -6), .45f, .6f, "glass", "#D5BA6F");
            Forecourt(g, 27, 27);
            CityProps.Bench(g, new(-5, 0, -12));
            CityProps.Bench(g, new(5, 0, -12));
        }

        public static void Arcade(CityStaticGeometry g)
        {
            Shell(g, 30, 20, 6, "#C9BBA8", "#7B6788");
            Forecourt(g, 36, 29);
            g.Box(new(0, 3.7f, -11.55f), new(25, .24f, 3.3f), "metal", Metal);
            g.Box(new(0, 3.48f, -13.15f), new(25, .24f, .12f), "metal", "#7B6788");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(new(side * 11.5f, 1.8f, -12.5f), new(.15f, 3.6f, .15f), "metal", Metal);
                g.Collider("ArcadeCanopyPost", new(side * 11.5f, 1.8f, -12.5f), new(.18f, 3.6f, .18f));
                g.Beam(new(side * 11.5f, 2.8f, -12.5f), new(side * 11.5f, 3.56f, -11.7f),
                    .1f, "metal", Metal);
                CityCivicDetails.ArcadeCabinet(g, new(side * 5, 0, -11.8f));
                CityCivicDetails.ArcadeCabinet(g, new(side * 8, 0, -11.8f));
            }
        }

        static void Shell(CityStaticGeometry g, float width, float depth, float height,
            string wall, string accent, bool frontWindows = true)
        {
            g.Box(new(0, height * .5f, 0), new(width, height, depth), "plaster", wall);
            g.Collider("BuildingShell", new(0, height * .5f, 0), new(width, height, depth));
            g.Box(new(0, .18f, 0), new(width + .3f, .36f, depth + .3f), "stone", "#989F96");
            CityCivicDetails.Roof(g, width, depth, height, wall);
            g.Box(new(0, height - .75f, -depth * .5f - .1f), new(width, .8f, .2f), "metal", accent);
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = -depth * .5f + 3; z <= depth * .5f - 2; z += 4)
                    CityCivicDetails.Window(g, new(side * (width * .5f + .06f), 2.7f, z), side * 90);
                for (float x = -width * .5f + 3; x <= width * .5f - 2; x += 4)
                {
                    if (Mathf.Abs(x) < 3 || side < 0 && !frontWindows) continue;
                    CityCivicDetails.Window(g, new(x, 2.7f, side * (depth * .5f + .06f)), side < 0 ? 180 : 0);
                }
                for (int end = -1; end <= 1; end += 2)
                    g.Box(new(side * (width * .5f - .22f), height * .5f, end * (depth * .5f - .22f)),
                        new(.52f, height, .52f), "stone", Trim);
            }
            CityCivicDetails.ServiceDoor(g, depth);
            if (!frontWindows) return;
            Vector3 door = new(0, 1.5f, -depth * .5f - .16f);
            g.Box(door, new(2.7f, 3, .18f), "metal", Metal);
            g.Box(door + Vector3.back * .11f, new(2.3f, 2.65f, .06f), "glass", Glass);
            g.Box(door + Vector3.back * .16f, new(.08f, 2.7f, .04f), "metal", Trim);
            for (int side = -1; side <= 1; side += 2)
                g.Box(door + new Vector3(side * .18f, -.1f, -.21f), new(.06f, .5f, .05f), "metal", Trim);
        }

        static void Forecourt(CityStaticGeometry g, float width, float depth)
        {
            g.Box(new(0, .025f, 0), new(width, .045f, depth), "paving", "#BEB9AA");
            for (int side = -1; side <= 1; side += 2)
            {
                CityProps.Planter(g, new(side * (width * .5f - 2), 0, -depth * .5f + 2), 2.5f);
                CityProps.Lamp(g, new(side * (width * .5f - 1), 0, depth * .5f - 2));
            }
        }
    }
}
