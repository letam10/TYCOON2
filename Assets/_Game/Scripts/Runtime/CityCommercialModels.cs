using UnityEngine;

namespace Tycoon
{
    public static class CityCommercialModels
    {
        public static void Cafe(CityStaticGeometry g)
        {
            CityCommercialDetails.Court(g, 30, 28);
            CityCommercialDetails.Shell(g, 22, 13, 4.5f, "#B79980");
            CityCommercialDetails.Doors(g, -6.65f);
            foreach (float x in new[] { -7f, -3.8f, 3.8f, 7f })
                CityCommercialDetails.Window(g, new(x, 2.2f, -6.61f), 2.7f, 2.7f);
            for (int side = -1; side <= 1; side += 2)
                foreach (float z in new[] { -3f, 1f, 4f })
                    CityCommercialDetails.Window(g, new(side * 11.1f, 2.2f, z), 2.4f, 2.4f, 90);
            g.Box(new(0, 3.5f, -8.3f), new(23, .18f, 3.3f), "roof", "#5B7566");
            CityCommercialDetails.Banner(g, new(0, 4.15f, -6.72f), 5.7f, "#BEA36B");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(new(side * 10.5f, 1.7f, -9.7f), new(.13f, 3.4f, .13f), "metal", "#52645E");
                g.Collider("CafeCanopyPost", new(side * 10.5f, 1.7f, -9.7f), new(.18f, 3.4f, .18f));
                Vector3 table = new(side * 6, 0, -10.9f);
                g.Cylinder(table + Vector3.up * .93f, .8f, .09f, "wood", "#A48865");
                g.Cylinder(table + Vector3.up * .45f, .1f, .87f, "metal", "#52645E");
                g.Cylinder(table + Vector3.up * .055f, .35f, .08f, "metal", "#52645E");
                g.Collider("CafeTable", table + Vector3.up * .5f, new(1.6f, 1, 1.6f));
                for (int chair = -1; chair <= 1; chair += 2)
                    CityCommercialDetails.Chair(g, table + new Vector3(chair * 1.23f, 0, 0), chair * 90);
                g.Cylinder(table + new Vector3(.18f, 1.04f, 0), .1f, .15f, "ceramic", "#DAD5C6");
            }
            CityCommercialDetails.RoofPlant(g, new(7, 4.62f, 3));
        }

        public static void Bank(CityStaticGeometry g)
        {
            CityCommercialDetails.Court(g, 35, 34);
            CityCommercialDetails.Shell(g, 26, 18, 7.2f, "#C5BAA4");
            CityCommercialDetails.Doors(g, -9.17f, 3.2f);
            for (int column = -2; column <= 2; column++)
            {
                if (column == 0) continue;
                Vector3 p = new(column * 4.8f, 0, -10.3f);
                g.Cylinder(p + Vector3.up * .22f, .66f, .3f, "stone", "#AAA695");
                g.Cylinder(p + Vector3.up * 3.1f, .43f, 5.5f, "stone", "#DAD5C6");
                g.Cylinder(p + Vector3.up * 5.95f, .6f, .24f, "stone", "#AAA695");
                g.Collider("BankColumn", p + Vector3.up * 3, new(1.2f, 6, 1.2f));
            }
            for (int side = -1; side <= 1; side += 2)
                foreach (float z in new[] { -5f, 0f, 5f })
                    CityCommercialDetails.Window(g, new(side * 13.1f, 3.5f, z), 2.7f, 3.4f, 90);
            foreach (float x in new[] { -8f, -4f, 4f, 8f })
            {
                CityCommercialDetails.Window(g, new(x, 3.5f, -9.1f), 2.3f, 3.6f);
                CityCommercialDetails.Window(g, new(x, 3.5f, 9.1f), 2.7f, 3.2f);
            }
            g.Box(new(0, 6.2f, -10.3f), new(24, .36f, 2.1f), "stone", "#DAD5C6");
            CityCommercialDetails.Banner(g, new(0, 6.95f, -9.3f), 7, "#A99057");
            g.Cylinder(new(0, 8.1f, -8.4f), .55f, .13f, "metal", "#B69D63");
            CityCommercialDetails.RoofPlant(g, new(8, 7.65f, 5));
        }

        public static void Hotel(CityStaticGeometry g)
        {
            CityCommercialDetails.Court(g, 44, 32);
            CityCommercialDetails.Shell(g, 34, 20, 13.6f, "#B3B8B1");
            CityCommercialDetails.Doors(g, -10.15f, 3.1f);
            g.Box(new(0, 3.6f, -12), new(11, .3f, 4.1f), "metal", "#536A70");
            CityCommercialDetails.Banner(g, new(0, 4.3f, -10.2f), 8, "#BFA06B");
            for (int floor = 0; floor < 4; floor++)
            {
                float y = 1.95f + floor * 3.25f;
                for (int x = -3; x <= 3; x++)
                {
                    if (floor == 0 && Mathf.Abs(x) < 2) continue;
                    CityCommercialDetails.Window(g, new(x * 4.5f, y, -10.1f), 2.8f, 2.1f);
                    CityCommercialDetails.Window(g, new(x * 4.5f, y, 10.1f), 2.8f, 2.1f);
                }
                for (int side = -1; side <= 1; side += 2)
                    foreach (float z in new[] { -6.5f, -2.2f, 2.2f, 6.5f })
                        CityCommercialDetails.Window(g, new(side * 17.1f, y, z), 2.7f, 2.1f, 90);
                g.Box(new(0, 3.5f + floor * 3.25f, -10.25f), new(34.4f, .12f, .35f),
                    "stone", "#D4D0C2");
            }
            CityCommercialDetails.RoofPlant(g, new(-10, 14.05f, 5));
            CityCommercialDetails.RoofPlant(g, new(10, 14.05f, 5));
            CityProps.Bench(g, new(-13, 0, -13));
            CityProps.Bench(g, new(13, 0, -13));
        }

        public static void Theater(CityStaticGeometry g)
        {
            CityCommercialDetails.Court(g, 41, 30);
            CityCommercialDetails.Shell(g, 32, 20, 7.8f, "#AA887E");
            g.Box(new(0, 3.6f, -12), new(26, .42f, 4.3f), "metal", "#6B606D");
            CityCommercialDetails.Banner(g, new(0, 4.7f, -10.3f), 17, "#B7A471");
            for (int door = -1; door <= 1; door++)
            {
                CityCommercialDetails.Window(g, new(door * 5, 1.85f, -10.15f), 3.5f, 3.2f);
                g.Box(new(door * 5, 1.65f, -10.36f), new(.065f, .65f, .045f), "metal", "#C1B99B");
            }
            for (int side = -1; side <= 1; side += 2)
            {
                CityCommercialDetails.Window(g, new(side * 16.1f, 2.9f, -6), 2.2f, 3.8f, 90);
                CityCommercialDetails.Window(g, new(side * 16.1f, 2.9f, 6), 2.2f, 3.8f, 90);
                g.Box(new(side * 13.6f, 2.1f, -10.25f), new(2.6f, 3.2f, .2f), "metal", "#43565B");
                g.Box(new(side * 13.6f, 2.1f, -10.38f), new(2.35f, 2.9f, .04f), "fabric", "#A99165");
            }
            for (int lamp = -10; lamp <= 10; lamp += 2)
                g.Cylinder(new(lamp, 3.34f, -13.5f), .12f, .06f, "ceramic", "#DFD7AD");
            CityCommercialDetails.RoofPlant(g, new(10, 8.25f, 5));
        }
    }
}
