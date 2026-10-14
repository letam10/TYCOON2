using UnityEngine;

namespace Tycoon
{
    public static class CityMarinaModels
    {
        public static void Build(CityStaticGeometry g)
        {
            g.Box(new(-18, .045f, 0), new(24, .07f, 42), "paving", "#B7B09C");
            g.Box(new(12, .035f, 0), new(36, .035f, 42), "water", "#346E7B");
            foreach (float z in new[] { -12f, 12f })
            {
                g.Box(new(2, .18f, z), new(30, .22f, 3.2f), "wood", "#A18B68");
                g.Collider("MarinaPier", new(2, .08f, z), new(30, .4f, 3.2f));
                g.Ramp(new(-15, .02f, z), new(-12, .28f, z), "wood", "#A18B68");
                for (int x = -10; x <= 16; x += 3)
                {
                    g.Box(new(x, .305f, z), new(.025f, .015f, 3), "wood", "#786B57");
                    g.Cylinder(new(x, .4f, z + Mathf.Sign(z) * 1.45f), .12f, .8f,
                        "wood", "#786B57");
                    g.Box(new(x, .34f, z - Mathf.Sign(z) * 1.25f), new(.4f, .12f, .18f),
                        "metal", "#778B83");
                }
                CityLeisureModels.Railing(g, new(-11, .29f, z + Mathf.Sign(z) * 1.4f),
                    new(16, .29f, z + Mathf.Sign(z) * 1.4f));
            }
            CityBoatModels.Fishing(g, new(1.5f, 0, 0));
            CityBoatModels.Yacht(g, new(11, 0, 0));
            CityProps.Bench(g, new(-23, 0, 7));
            CityProps.Planter(g, new(-26, 0, -7), 2.4f);
            CityProps.Lamp(g, new(-27, 0, -18));
            CityProps.Lamp(g, new(-27, 0, 18));
            TicketShelter(g);
        }

        static void TicketShelter(CityStaticGeometry g)
        {
            Vector3 p = new(-21, 0, -1);
            g.Box(p + Vector3.up * .12f, new(5, .24f, 4), "stone", "#A8AD9A");
            g.Box(p + Vector3.up * 1.6f, new(4.6f, 2.8f, 3.6f), "wood", "#AC9677");
            g.Collider("MarinaKiosk", p + Vector3.up * 1.6f, new(4.6f, 3.2f, 3.6f));
            g.Box(p + new Vector3(0, 2.05f, -1.84f), new(3.3f, 1, .08f), "glass", "#658E98");
            g.Box(p + new Vector3(0, 1.48f, -2.02f), new(3.7f, .14f, .6f), "wood", "#8E7C63");
            g.Box(p + Vector3.up * 3.16f, new(5.1f, .28f, 4.1f), "roof", "#5D7772");
            CityCommercialDetails.Banner(g, p + new Vector3(0, 2.95f, -1.9f), 3.3f, "#BAC39F");
        }
    }
}
