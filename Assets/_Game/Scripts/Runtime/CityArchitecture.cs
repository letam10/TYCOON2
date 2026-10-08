using UnityEngine;

namespace Tycoon
{
    public static class CityArchitecture
    {
        public static void Farm(CityStaticGeometry g, CityDressingSpace space)
        {
            foreach (float z in new[] { -13f, 9f })
            {
                Vector3 p = new(-91, 0, z);
                if (space.Take(p, 12, 19, "Greenhouse")) Greenhouse(g, p);
            }
            Vector3 silo = new(-84, 0, -33);
            if (space.Take(silo, 15, 9, "Silo"))
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 p = silo + Vector3.right * (i * 6 - 3);
                    g.Cylinder(p + Vector3.up * .2f, 2.6f, .4f, "stone", "#AAA89A");
                    g.Cylinder(p + Vector3.up * 3, 2.05f, 5.6f, "metal", "#B4BCB6");
                    g.Cone(p + Vector3.up * 6.2f, 2.25f, 1.2f, "metal", "#647D7B");
                    for (int ring = 0; ring < 6; ring++)
                        g.Cylinder(p + Vector3.up * (.6f + ring), 2.1f, .09f, "metal", "#647D7B");
                    for (int rung = 0; rung < 12; rung++)
                        g.Box(p + new Vector3(0, .6f + rung * .45f, -2.12f), new(.7f, .07f, .12f),
                            "metal", "#647D7B");
                    for (int side = -1; side <= 1; side += 2)
                        g.Box(p + new Vector3(side * .38f, 3, -2.14f), new(.09f, 5.4f, .09f),
                            "metal", "#647D7B");
                    g.Collider("Silo", p + Vector3.up * 3, new(4.4f, 6, 4.4f));
                }
            }
            Vector3 barn = new(-115, 0, 47);
            if (space.Take(barn, 19, 13, "Barn")) House(g, barn, 16, 10, 5, "#A46E59", "#58685E", true);
        }

        static void Greenhouse(CityStaticGeometry g, Vector3 p)
        {
            g.Box(p + Vector3.up * .11f, new(10, .1f, 17), "paving", "#ACA99A");
            g.Collider("GreenhouseFloor", p + Vector3.up * .11f, new(10, .1f, 17));
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 wall = p + new Vector3(side * 4.4f, 1.48f, 0);
                g.Box(wall, new(.15f, 2.64f, 16), "glass", "#A0BFC0");
                g.Collider("GreenhouseWall", wall, new(.15f, 2.64f, 16));
                for (int row = 0; row < 3; row++)
                {
                    Vector3 bed = p + new Vector3(side * 2.5f, .41f, -5.4f + row * 5.2f);
                    g.Box(bed, new(2.4f, .5f, 4.3f), "wood", "#817154");
                    g.Collider("GreenhouseBed", bed, new(2.4f, .5f, 4.3f));
                    g.Box(bed + Vector3.up * .26f, new(2.1f, .06f, 4), "soil", "#685239");
                    for (int plant = 0; plant < 4; plant++)
                        g.Crown(bed + new Vector3(0, .6f, -1.4f + plant * .9f), new(1.3f, .7f, .7f),
                            "#54814F");
                }
                g.Beam(p + new Vector3(side * 4.5f, 2.8f, -8), p + new Vector3(side * 4.5f, 2.8f, 8),
                    .16f, "metal", "#E1DFCC");
            }
            for (int rib = 0; rib < 7; rib++)
            {
                float z = -8 + rib * 16f / 6;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 eave = p + new Vector3(side * 4.5f, 2.8f, z);
                    g.Beam(p + new Vector3(side * 4.5f, .2f, z), eave, .13f, "metal", "#E1DFCC");
                    g.Beam(eave, p + new Vector3(0, 4.5f, z), .15f, "metal", "#E1DFCC");
                }
            }
            g.Beam(p + new Vector3(0, 4.5f, -8), p + new Vector3(0, 4.5f, 8), .2f, "metal", "#E1DFCC");
            g.Box(p + new Vector3(0, 3.1f, -8.1f), new(4, .6f, .18f), "wood", "#58685E");
        }

        public static void Businesses(CityStaticGeometry g, CityDressingSpace space)
        {
            Backdrop("restaurant", 28, "#D6C9AB", "#58685E", 5.2f);
            Backdrop("bakery", 14, "#C99A80", "#85584C", 4.7f);
            Backdrop("supermarket", 29, "#D7D8CA", "#58685E", 5.5f);
            Vector3 factory = CityDistricts.Center("processing") + new Vector3(0, 0, 28);
            if (space.Take(factory, 32, 10, "Factory facade"))
            {
                House(g, factory, 30, 8, 6, "#AFAFA4", "#58685E", false);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 door = factory + new Vector3(-9 + i * 9, 2.05f, -4.12f);
                    g.Box(door, new(5.5f, 3.3f, .13f), "metal", "#647D7B");
                    for (int rib = 0; rib < 8; rib++)
                        g.Box(door + Vector3.up * (-1.4f + rib * .4f), new(5.5f, .05f, .22f),
                            "metal", "#B4BCB6");
                }
                g.Cylinder(factory + new Vector3(11, 8, 2), .8f, 5, "brick", "#A46E59");
                g.Cylinder(factory + new Vector3(11, 10.5f, 2), 1.05f, .3f, "metal", "#647D7B");
            }

            void Backdrop(string area, float width, string wall, string roof, float height)
            {
                var bounds = CityDistricts.Footprint(area);
                Vector3 p = bounds.center + new Vector3(0, 0, bounds.extents.z + 2);
                if (area == "bakery") p += new Vector3(10, 0, -4);
                if (area == "supermarket") p += Vector3.forward * 6.5f;
                if (!space.Take(p, width + 2, 6, area + " facade")) return;
                House(g, p, width, 4, height, wall, roof, true);
                for (float x = -width * .4f; x < width * .45f; x += 3)
                {
                    g.Box(p + new Vector3(x, 2.5f, -2.7f), new(2.8f, .15f, 1.3f), "roof", roof);
                    g.Box(p + new Vector3(x, 2.2f, -3.25f), new(2.8f, .45f, .12f), "plaster", wall);
                }
            }
        }

        public static void Neighborhoods(CityStaticGeometry g, CityDressingSpace space)
        {
            string[] colors = { "#D6C9AB", "#C99A80", "#B3BBB1", "#B5A28D" };
            for (int side = 0; side < 2; side++)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 p = new(side == 0 ? -121 : 128, 0, -22 + i * 20);
                    float width = 12 + i % 3 * 2;
                    if (!space.Take(p, width + 5, 16, "Neighborhood house")) continue;
                    House(g, p, width, 10, i % 2 == 0 ? 7 : 5, colors[i % 4], "#58685E", i % 2 == 0);
                    CityProps.Planter(g, p + new Vector3(-width * .35f, 0, -6.3f), 3);
                    CityProps.Bench(g, p + new Vector3(width * .3f, 0, -6.3f));
                    g.Box(p + new Vector3(0, .045f, -6), new(width + 3, .08f, 4), "paving", "#AAA89A");
                }
            }
        }

        static void House(CityStaticGeometry g, Vector3 p, float width, float depth, float height,
            string wall, string roof, bool pitched)
        {
            Vector3 foundation = p + Vector3.up * .23f;
            g.Box(foundation, new(width + .6f, .34f, depth + .6f), "stone", "#AAA89A");
            g.Collider("BuildingFoundation", foundation, new(width + .6f, .34f, depth + .6f));
            Vector3 body = p + Vector3.up * ((height + .4f) * .5f);
            g.Box(body, new(width, height - .4f, depth), "plaster", wall);
            g.Collider("Building", body, new(width, height - .4f, depth));
            if (pitched)
                CityBuildingDetails.PitchedRoof(g, p, width, depth, height, roof, wall);
            else g.Box(p + Vector3.up * height, new(width + .8f, .25f, depth + .8f), "roof", roof);
            CityBuildingDetails.Facade(g, p, width, depth, height, roof, depth >= 8 && width < 20);
            float chimney = height + (pitched ? depth * .22f : 0);
            g.Box(p + new Vector3(width * .3f, chimney + .4f, 0), new(1.1f, 1.5f, 1.1f),
                "brick", "#A46E59");
            g.Box(p + new Vector3(width * .3f, chimney + 1.16f, 0), new(1.35f, .14f, 1.35f),
                "stone", "#C2BDA8");
        }
    }
}
