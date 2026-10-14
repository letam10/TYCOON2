using UnityEngine;

namespace Tycoon
{
    public static class CityRailVehicleModelCoach
    {
        public static void Build(CityVehicleGeometry g, Material paint, float center, float door)
        {
            g.Box(CityVehicleSurface.Dark, new(0, .51f, center), new(2.54f, .18f, 9.4f));
            Roof(g, paint, center, 9.4f);
            foreach (float end in new[] { center - 4.65f, center + 4.65f })
            {
                g.Box(paint, new(0, 1.8f, end), new(2.52f, 2.4f, .12f));
                g.Box(CityVehicleSurface.Dark, new(0, 1.65f, end + .07f), new(.9f, 2.08f, .035f));
                g.Box(CityVehicleSurface.Glass, new(0, 2.01f, end + .094f), new(.63f, .88f, .014f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 1.275f;
                Panel(center - 4.68f, door - .56f);
                Panel(door + .56f, center + 4.68f);
                g.Box(paint, new(x, 2.99f, center), new(.09f, .16f, 9.28f));
                for (int window = 0; window < 8; window++)
                {
                    float z = center - 3.99f + window * 1.12f;
                    if (Mathf.Abs(z - door) < 1.05f) continue;
                    foreach (float edge in new[] { -1f, 1f })
                    {
                        g.Box(CityVehicleSurface.Dark, new(x, 2.16f + edge * .61f, z),
                            new(.045f, .055f, 1.01f));
                        g.Box(CityVehicleSurface.Dark, new(x, 2.16f, z + edge * .478f),
                            new(.045f, 1.26f, .055f));
                    }
                    g.Box(CityVehicleSurface.Glass, new(x + side * .026f, 2.16f, z), new(.015f, 1.15f, .9f));
                    g.Box(paint, new(x, 2.19f, z + .54f), new(.09f, 1.55f, .11f));
                    CityVehicleParts.Seat(g, new(side * .7f, .6f, z), true, .62f);
                }
                g.Box(CityVehicleSurface.Chrome, new(side * 1.21f, .38f, door), new(.27f, .12f, 1.06f));
                g.Box(CityVehicleSurface.Dark, new(side * 1.29f, .2f, door), new(.22f, .1f, 1.06f));

                void Panel(float start, float end)
                {
                    g.Box(paint, new(x, 1.01f, (start + end) * .5f), new(.09f, .82f, end - start));
                    g.Box(CityVehicleSurface.Stripe, new(x + side * .052f, 1.4f, (start + end) * .5f),
                        new(.014f, .11f, end - start));
                }
            }
            g.Box(CityVehicleSurface.Dark, new(0, .34f, center), new(1.6f, .25f, 3.5f));
            for (int vent = 0; vent < 4; vent++)
                g.Box(CityVehicleSurface.Chrome, new(0, 3.34f, center - 2.7f + vent * 1.8f),
                    new(.64f, .07f, .5f));
        }

        public static void Roof(CityVehicleGeometry g, Material paint, float z, float length)
        {
            Vector2[] crown =
            {
                new(-1.28f, 0), new(1.28f, 0), new(1.24f, .12f), new(1.05f, .26f),
                new(.65f, .33f), new(-.65f, .33f), new(-1.05f, .26f), new(-1.24f, .12f)
            };
            g.Loft(paint, new(0, 3, z), new[] { -length * .5f, -length * .47f, length * .47f, length * .5f },
                crown, new[] { .95f, 1f, 1f, .95f });
        }
    }
}
