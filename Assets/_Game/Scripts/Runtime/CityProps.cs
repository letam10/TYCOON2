using UnityEngine;

namespace Tycoon
{
    public static class CityProps
    {
        public static void Districts(CityStaticGeometry g, CityDressingSpace space)
        {
            Terrace(g, space, new(-22, 0, 64));
            Parking(g, space, new(91, 0, 57));
            ServiceYard(g, space, new(32, 0, 42), false);
            ServiceYard(g, space, new(90, 0, 4), true);
            MarketStalls(g, space);
            Irrigation(g, space);
        }

        static void Terrace(CityStaticGeometry g, CityDressingSpace space, Vector3 p)
        {
            if (!space.Take(p, 10, 20, "Terrace")) return;
            g.Box(p + Vector3.up * .04f, new(9, .07f, 19), "paving", "#C0B8A0");
            for (int row = 0; row < 3; row++)
            {
                Vector3 table = p + new Vector3(0, 0, -6 + row * 6);
                g.Cylinder(table + Vector3.up * .99f, 1.05f, .12f, "wood", "#A5825D");
                g.Cylinder(table + Vector3.up * .925f, 1.01f, .05f, "metal", "#365B55");
                g.Cylinder(table + Vector3.up * .49f, .12f, .86f, "metal", "#647D7B");
                g.Cylinder(table + Vector3.up * .085f, .43f, .05f, "metal", "#647D7B");
                g.Cylinder(table + Vector3.up * 1.7f, .07f, 3.4f, "wood", "#817154");
                g.Cone(table + Vector3.up * 3.4f, 2.15f, .7f, "roof", "#D6C9AB");
                g.Collider("TerraceTable", table + Vector3.up * .555f, new(2.1f, .99f, 2.1f));
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 chair = table + Vector3.right * side * 1.56f;
                    g.Box(chair + Vector3.up * .49f, new(.66f, .1f, .76f), "wood", "#A5825D");
                    g.Box(chair + Vector3.up * .58f, new(.62f, .08f, .71f), "fabric", "#AEC4AE");
                    g.Box(chair + new Vector3(side * .27f, .945f, 0), new(.105f, .29f, .76f),
                        "wood", "#A5825D");
                    g.Box(chair + new Vector3(side * .202f, .935f, 0), new(.035f, .22f, .63f),
                        "fabric", "#AEC4AE");
                    for (int leg = -1; leg <= 1; leg += 2)
                    {
                        g.Box(chair + new Vector3(-side * .24f, .275f, leg * .285f),
                            new(.075f, .43f, .075f), "metal", "#647D7B");
                        g.Box(chair + new Vector3(side * .265f, .57f, leg * .285f),
                            new(.075f, 1.02f, .075f), "metal", "#647D7B");
                        g.Box(chair + new Vector3(0, .29f, leg * .285f), new(.51f, .055f, .05f),
                            "metal", "#647D7B");
                    }
                    g.Collider("TerraceChair", chair + Vector3.up * .575f, new(.66f, 1.03f, .76f));
                }
                g.Cylinder(table + Vector3.up * 1.11f, .22f, .12f, "ceramic", "#D6C9AB");
                g.Crown(table + Vector3.up * 1.27f, new(.38f, .3f, .38f), "#77904E");
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Planter(g, p + new Vector3(side * 3.5f, 0, -8), 2.5f);
                Planter(g, p + new Vector3(side * 3.5f, 0, 8), 2.5f);
            }
        }

        static void Parking(CityStaticGeometry g, CityDressingSpace space, Vector3 p)
        {
            if (!space.Take(p, 12, 27, "Parking")) return;
            g.Box(p + Vector3.up * .035f, new(11, .06f, 26), "asphalt", "#777E77");
            for (int i = 0; i < 7; i++)
            {
                g.Box(p + new Vector3(1, .08f, -11 + i * 3.4f), new(7.5f, .025f, .1f),
                    "plaster", "#E1DFCC");
                if (i < 6)
                {
                    g.Box(p + new Vector3(3.8f, .17f, -9.3f + i * 3.4f), new(.4f, .25f, 1.8f),
                        "stone", "#AAA89A");
                    g.Collider("ParkingStop", p + new Vector3(3.8f, .17f, -9.3f + i * 3.4f),
                        new(.4f, .25f, 1.8f));
                }
            }
            Vector3 shelter = p + new Vector3(-3.8f, 0, 7);
            g.Box(shelter + Vector3.up * 2.7f, new(2.7f, .15f, 5.5f), "roof", "#58685E");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(shelter + new Vector3(-1, 1.35f, side * 2.5f), new(.13f, 2.7f, .13f),
                    "metal", "#647D7B");
                g.Collider("CartShelterPost", shelter + new Vector3(-1, 1.35f, side * 2.5f),
                    new(.13f, 2.7f, .13f));
            }
            for (int i = 0; i < 5; i++) Cart(g, shelter + new Vector3(0, 0, -1.6f + i * .7f));
            Lamp(g, p + new Vector3(-4.5f, 0, -11));
        }

        static void Cart(CityStaticGeometry g, Vector3 p)
        {
            g.Box(p + Vector3.up * .55f, new(.8f, .06f, 1), "metal", "#B4BCB6");
            for (int side = -1; side <= 1; side += 2)
            {
                for (int bar = 0; bar < 3; bar++)
                    g.Box(p + new Vector3(side * .4f, .65f + bar * .16f, 0), new(.035f, .035f, 1),
                        "metal", "#B4BCB6");
                for (int end = -1; end <= 1; end += 2)
                    g.Cylinder(p + new Vector3(side * .3f, .18f, end * .36f), .12f, .2f,
                        "metal", "#647D7B");
            }
            g.Box(p + new Vector3(0, 1.05f, -.6f), new(.86f, .09f, .12f), "metal", "#58685E");
            for (int end = -1; end <= 1; end += 2)
            {
                for (int bar = 0; bar < 4; bar++)
                    g.Box(p + new Vector3(-.36f + bar * .24f, .77f, end * .5f), new(.035f, .48f, .035f),
                        "metal", "#B4BCB6");
            }
            g.Collider("ShoppingCart", p + new Vector3(0, .55f, -.06f), new(.86f, 1.1f, 1.2f));
        }

        static void ServiceYard(CityStaticGeometry g, CityDressingSpace space, Vector3 p, bool factory)
        {
            if (!space.Take(p, 11, 14, factory ? "Factory service yard" : "Market service yard")) return;
            g.Box(p + Vector3.up * .04f, new(10, .07f, 13), "paving", "#AAA89A");
            for (int row = 0; row < 3; row++)
            {
                Vector3 pallet = p + new Vector3(2.6f, 0, -4 + row * 3.5f);
                for (int slat = 0; slat < 5; slat++)
                    g.Box(pallet + new Vector3(-.9f + slat * .45f, .25f, 0), new(.34f, .18f, 2.2f),
                        "wood", "#817154");
                for (int block = 0; block < 2; block++)
                    g.Box(pallet + new Vector3(0, .1f, block == 0 ? -.7f : .7f), new(2.2f, .2f, .25f),
                        "wood", "#817154");
                if (factory)
                {
                    g.Cylinder(pallet + Vector3.up * .9f, .65f, 1.25f, "metal", "#647D7B");
                    for (int hoop = 0; hoop < 2; hoop++)
                        g.Cylinder(pallet + Vector3.up * (.45f + hoop * .85f), .67f, .09f,
                            "metal", "#58685E");
                    g.Cylinder(pallet + new Vector3(.3f, 1.55f, 0), .1f, .06f, "metal", "#B4BCB6");
                }
                else
                {
                    g.Box(pallet + Vector3.up * .8f, new(1.8f, .9f, 1.6f), "wood", "#B5A28D");
                    for (int side = -1; side <= 1; side += 2)
                    {
                        g.Box(pallet + new Vector3(0, .8f, side * .82f), new(1.8f, .14f, .07f),
                            "wood", "#817154");
                        g.Box(pallet + new Vector3(side * .58f, .8f, 0), new(.14f, .94f, 1.66f),
                            "wood", "#817154");
                    }
                }
                g.Collider("ServicePallet", pallet + Vector3.up * .8f, new(2.2f, 1.6f, 2.2f));
            }
            for (int i = 0; i < 5; i++)
                g.Box(p + new Vector3(-3.6f, .09f, -4 + i * 2), new(.25f, .04f, 1),
                    "plaster", "#D6C9AB", -35);
            Lamp(g, p + new Vector3(-4, 0, 5.5f));
        }

        static void MarketStalls(CityStaticGeometry g, CityDressingSpace space)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = CityDistricts.Center("farm_shop")
                    + new Vector3(-12 + i * 10, 0, i == 2 ? 18.5f : 20);
                if (!space.Take(p, 5.5f, 5, "Market stall")) continue;
                g.Box(p + Vector3.up * .5f, new(4, 1, 1.6f), "wood", "#817154");
                g.Box(p + Vector3.up * 2.9f, new(5, .18f, 3.7f), "roof", "#58685E");
                for (int side = -1; side <= 1; side += 2)
                {
                    g.Box(p + new Vector3(side * 2.1f, 1.5f, .5f), new(.15f, 3, .15f),
                        "wood", "#817154");
                    g.Collider("MarketStallPost", p + new Vector3(side * 2.1f, 1.5f, .5f), new(.15f, 3, .15f));
                }
                for (int crate = 0; crate < 3; crate++)
                {
                    Vector3 box = p + new Vector3(-1.3f + crate * 1.3f, 1.2f, 0);
                    g.Box(box, new(1.1f, .4f, 1.2f), "wood", "#B5A28D");
                    g.Crown(box + Vector3.up * .3f, new(.85f, .4f, .9f), "#54814F");
                }
                g.Collider("MarketStall", p + Vector3.up * .5f, new(4, 1, 1.6f));
            }
        }

        static void Irrigation(CityStaticGeometry g, CityDressingSpace space)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = new(-89 + i * 12, 0, 22);
                if (!space.Take(p, 9, 2)) continue;
                g.Box(p + Vector3.up * .14f, new(8, .2f, .55f), "stone", "#AAA89A");
                g.Box(p + Vector3.up * .255f, new(7.8f, .025f, .35f), "water", "#548E90");
                g.Box(p + new Vector3(3.5f, .7f, .7f), new(.15f, 1.4f, .15f), "metal", "#647D7B");
                g.Box(p + new Vector3(3.5f, 1.2f, .7f), new(.8f, .1f, .1f), "metal", "#647D7B");
                g.Collider("IrrigationTrough", p + Vector3.up * .14f, new(8, .2f, .55f));
                g.Collider("IrrigationValve", p + new Vector3(3.5f, .7f, .7f), new(.2f, 1.4f, .2f));
            }
        }

        public static void Planter(CityStaticGeometry g, Vector3 p, float width)
        {
            CityStreetPropDetails.Planter(g, p, width);
        }

        public static void Bench(CityStaticGeometry g, Vector3 p)
        {
            CityStreetPropDetails.Bench(g, p);
        }

        public static void Lamp(CityStaticGeometry g, Vector3 p)
        {
            CityStreetPropDetails.Lamp(g, p);
        }
    }
}
