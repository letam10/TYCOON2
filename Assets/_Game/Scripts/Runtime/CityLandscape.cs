using UnityEngine;

namespace Tycoon
{
    public static class CityLandscape
    {
        public static void Build(CityStaticGeometry g, CityDressingSpace space)
        {
            Park(g, space);
            River(g, space);
            Roadsides(g, space);
            Orchards(g, space);
        }

        static void Park(CityStaticGeometry g, CityDressingSpace space)
        {
            Vector3 p = CityDistricts.ParkCenter;
            if (!space.Take(p, 23, 14, "Park")) return;
            g.Box(p + Vector3.up * .035f, new(22, .06f, 13), "grass", "#7C9163");
            g.Box(p + Vector3.up * .08f, new(22, .07f, 3), "paving", "#C0B8A0");
            g.Box(p + Vector3.up * .085f, new(3, .08f, 13), "paving", "#C0B8A0");
            g.Cylinder(p + Vector3.up * .18f, 3.2f, .3f, "stone", "#AAA89A");
            g.Cylinder(p + Vector3.up * .38f, 2.7f, .18f, "water", "#548E90");
            g.Cylinder(p + Vector3.up * .7f, .5f, 1.1f, "stone", "#AAA89A");
            g.Cylinder(p + Vector3.up * 1.3f, 1.4f, .24f, "stone", "#AAA89A");
            g.Cylinder(p + Vector3.up * 1.44f, 1.25f, .04f, "water", "#548E90");
            g.Cylinder(p + Vector3.up * 1.9f, .22f, 1, "stone", "#AAA89A");
            g.Cone(p + Vector3.up * 2.45f, .42f, .35f, "metal", "#647D7B");
            g.Collider("Fountain", p + Vector3.up * .45f, new(5.8f, .9f, 5.8f));
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = -1; row <= 1; row += 2)
                {
                    Tree(g, p + new Vector3(side * 8, 0, row * 4.8f), 1);
                    CityProps.Bench(g, p + new Vector3(side * 5.2f, 0, row * 3.8f));
                }
                CityProps.Lamp(g, p + new Vector3(side * 10, 0, 1.8f));
            }
        }

        static void River(CityStaticGeometry g, CityDressingSpace space)
        {
            // Sông nằm sau khu phố; mặt cầu thấp giữ lối đi ngang bằng với bờ.
            const float z = 103;
            for (int section = 0; section < 13; section++)
            {
                Vector3 p = new(-119 + section * 21, 0, z);
                if (!space.Take(p, 21, 9)) continue;
                g.Box(p + Vector3.up * .02f, new(21, .035f, 7), "water", "#548E90");
                for (int side = -1; side <= 1; side += 2)
                {
                    g.Box(p + new Vector3(0, .13f, side * 3.8f), new(21, .3f, .6f), "stone", "#AAA89A");
                    if (section is not (2 or 6 or 10))
                        g.Collider("RiverBank", p + new Vector3(0, .13f, side * 3.8f), new(21, .3f, .6f));
                    g.Box(p + new Vector3(0, .055f, side * 4.45f), new(21, .08f, .7f),
                        "paving", "#C0B8A0");
                }
                if (section is 2 or 6 or 10)
                {
                    g.Box(p + Vector3.up * .14f, new(4.8f, .2f, 10), "wood", "#817154");
                    for (int side = -1; side <= 1; side += 2)
                    {
                        g.Box(p + new Vector3(side * 2.5f, 1.05f, 0), new(.14f, .14f, 10),
                            "wood", "#817154");
                        g.Collider("BridgeRailing", p + new Vector3(side * 2.5f, .6f, 0), new(.2f, 1.2f, 10));
                        for (int post = 0; post < 6; post++)
                            g.Box(p + new Vector3(side * 2.5f, .6f, -5 + post * 2), new(.2f, 1.2f, .2f),
                                "wood", "#817154");
                    }
                }
            }
            for (int i = 0; i < 17; i++)
            {
                Vector3 p = new(-123 + i * 16, 0, 112);
                if (space.Take(p, 7, 7)) Tree(g, p, 1.25f);
            }
        }

        static void Roadsides(CityStaticGeometry g, CityDressingSpace space)
        {
            foreach (var road in CityStreetNetwork.Bounds())
            {
                bool horizontal = road.size.x > road.size.z;
                float length = horizontal ? road.size.x : road.size.z;
                float breadth = horizontal ? road.size.z : road.size.x;
                for (float along = -length * .5f + 5; along < length * .5f - 3; along += 13)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 offset = horizontal ? new(along, 0, side * (breadth * .5f + 3.5f))
                            : new(side * (breadth * .5f + 3.5f), 0, along);
                        Vector3 p = road.center + offset;
                        p.y = 0;
                        if (!space.Take(p, 3.8f, 3.8f)) continue;
                        g.Box(p + Vector3.up * .04f, new(3.5f, .07f, 3.5f), "paving", "#AAA89A");
                        g.Box(p + Vector3.up * .09f, new(1.4f, .04f, 1.4f), "soil", "#685239");
                        Tree(g, p, .7f);
                        CityProps.Lamp(g, p + new Vector3(1.1f, 0, 1.1f));
                    }
                }
            }
        }

        static void Orchards(CityStaticGeometry g, CityDressingSpace space)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 11; column++)
                {
                    Vector3 p = new(-109 + column * 22, 0, -60 + row * 5);
                    if (!space.Take(p, 6, 4.5f)) continue;
                    Tree(g, p, .75f + column % 3 * .1f);
                }
            }
            foreach (var p in new[] { new Vector3(-113, 0, 83), new Vector3(-6, 0, 13),
                new Vector3(113, 0, -31), new Vector3(29, 0, -33) })
            {
                if (!space.Take(p, 8, 5)) continue;
                g.Box(p + Vector3.up * .12f, new(7, .2f, 4), "soil", "#685239");
                for (int i = 0; i < 5; i++)
                    g.Crown(p + new Vector3(-2.4f + i * 1.2f, .65f, 0), new(1.6f, 1.2f, 2.8f),
                        i % 2 == 0 ? "#54814F" : "#7C9163");
            }
        }

        static void Tree(CityStaticGeometry g, Vector3 p, float scale)
        {
            int species = Mathf.Abs(Mathf.RoundToInt(p.x * 1.7f + p.z * 2.3f)) % 3;
            CityVegetation.Tree(g, p, scale, species);
        }
    }
}
