using UnityEngine;

namespace Tycoon
{
    public static class CityVehicleGeometryTrucks
    {
        public static void Build(Transform root, CityVehicleRig rig, string color, bool openBed = false)
        {
            bool small = rig.Kind == CityVehicleKind.SmallTruck;
            bool tank = rig.Kind == CityVehicleKind.Tanker;
            float length = small ? 5.4f : 8.2f;
            float width = small ? 2.05f : 2.45f;
            float roof = small ? 2.25f : 2.85f;
            float radius = small ? .4f : .48f;
            float front = length * .5f;
            float cab = front - .92f;
            float bodyLength = length - 2.15f;
            float bodyZ = -1.025f;
            var paint = CityVehicleSurface.Paint(color);
            var g = new CityVehicleGeometry();
            g.Box(CityVehicleSurface.Dark, new(0, .59f, 0), new(width * .66f, .25f, length - .14f));
            Cab(g, paint, width, roof, front);
            g.Box(CityVehicleSurface.Chrome, new(0, .42f, front - .06f), new(width * .95f, .18f, .21f));
            CityVehicleParts.Front(g, width, front + .015f, 1.02f);
            g.Box(CityVehicleSurface.Dark, new(0, 1.18f, front + .045f),
                new(width * .72f, .12f, .035f));
            g.Box(CityVehicleSurface.Chrome, new(0, .72f, front + .04f),
                new(width * .38f, .08f, .035f));
            g.Box(CityVehicleSurface.Dark, new(0, roof - 1.11f, front - .54f), new(width * .87f, .22f, .34f));
            if (tank) Tank(g, width, bodyLength, bodyZ);
            else Cargo(g, width, bodyLength, bodyZ, small, openBed);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * width * .5f;
                g.Arch(paint, x, cab, radius, radius + .06f, .11f);
                g.Box(CityVehicleSurface.Dark, new(x * .98f, radius * .58f, cab - .57f), new(.14f, .25f, .13f));
                g.Box(CityVehicleSurface.Chrome, new(x * .9f, .63f, cab - .58f), new(.24f, .1f, .52f));
                g.Box(CityVehicleSurface.Chrome, new(x * .99f, 1.22f, cab - .55f),
                    new(.055f, .045f, .18f));
                g.Box(CityVehicleSurface.Light, new(x * .99f, .92f, cab + .02f),
                    new(.055f, .09f, .12f));
                CityVehicleParts.Mirror(g, x + side * .13f, roof - .65f, front - .36f);
                CityVehicleParts.Seat(g, new(side * width * .23f, .92f, cab - .28f), false, .57f);
                g.Box(CityVehicleSurface.Red, new(x * .8f, .72f, -front - .015f), new(.28f, .17f, .04f));
                if (!small)
                {
                    foreach (float rear in new[] { -2.05f, -3.12f })
                        g.Arch(CityVehicleSurface.Dark, x, rear, radius, radius + .07f, .1f);
                    for (int bar = 0; bar < 2; bar++)
                        g.Box(CityVehicleSurface.Chrome, new(x * .93f, .48f + bar * .2f, -.4f),
                            new(.06f, .065f, 1.8f));
                }
            }
            g.Build("TruckCabAndLoad", root, rig);
            rig.Wheels = small ? CityVehicleParts.Wheels(root, rig, width, radius, -1.68f, cab) :
                CityVehicleParts.Wheels(root, rig, width, radius, -3.12f, -2.05f, cab);
            rig.DoorInsideLocal = new(width * .44f, .85f, cab - .1f);
            rig.RightDoors = new[] { Door(1) };
            rig.LeftDoors = new[] { Door(-1) };
            CityVehicleParts.Collider(root, new(0, 1.55f, 0), new(width, 2.5f, length));

            Transform Door(int side) => CityVehicleParts.Door(root, rig, side, width * .46f, cab - .2f,
                .89f, roof - .96f, .93f, paint, false);
        }

        static void Cab(CityVehicleGeometry g, Material paint, float width, float roof, float front)
        {
            float x = width * .5f;
            float belt = roof - .97f;
            Vector2[] shape =
            {
                new(-x * .88f, 0), new(x * .88f, 0), new(x, .16f),
                new(x * .93f, .65f), new(x * .76f, .79f), new(-x * .76f, .79f),
                new(-x * .93f, .65f), new(-x, .16f)
            };
            g.Loft(paint, new(0, belt - .74f, 0), new[] { front - .53f, front - .22f, front },
                shape, new[] { 1f, 1f, .96f });
            g.Box(paint, new(0, roof, front - 1), new(width * .93f, .13f, 1.72f), .06f);
            g.Box(paint, new(0, roof - .66f, front - 1.85f), new(width * .92f, 1.4f, .13f));
            CityVehicleParts.Windshield(g, x * .89f, x * .84f, belt + .05f, roof - .15f,
                front - .16f, front - .49f);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Beam(paint, new(side * x * .92f, belt, front - .2f),
                    new(side * x * .88f, roof, front - .49f), .09f);
                g.Box(paint, new(side * x * .94f, roof - .57f, front - 1.73f), new(.1f, 1.15f, .19f));
            }
        }

        static void Cargo(CityVehicleGeometry g, float width, float length, float z, bool small, bool openBed)
        {
            var cargo = small ? CityVehicleSurface.Paint("#B4B7AC") : CityVehicleSurface.Chrome;
            g.Box(cargo, new(0, 1.01f, z), new(width, .3f, length));
            if (openBed)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    g.Box(cargo, new(side * width * .48f, 1.30f, z), new(.07f, .28f, length));
                    for (int rib = 0; rib < 6; rib++)
                        g.Box(CityVehicleSurface.Dark, new(side * width * .502f, 1.30f,
                            z - length * .42f + rib * length * .168f), new(.016f, .27f, .035f));
                }
                g.Box(cargo, new(0, 1.30f, z - length * .49f), new(width, .28f, .07f));
                return;
            }
            if (small)
            {
                Vector2[] canopy =
                {
                    new(-width * .48f, 0), new(width * .48f, 0), new(width * .48f, .96f),
                    new(width * .36f, 1.22f), new(0, 1.31f), new(-width * .36f, 1.22f),
                    new(-width * .48f, .96f)
                };
                g.Loft(CityVehicleSurface.Rubber, new(0, 1.15f, z),
                    new[] { -length * .49f, -length * .36f, 0, length * .36f, length * .49f },
                    canopy, new[] { .97f, 1f, .985f, 1f, .97f });
                for (int side = -1; side <= 1; side += 2)
                    for (int i = 0; i < 6; i++)
                    {
                        float pz = z - length * .46f + i * length * .18f;
                        g.Beam(CityVehicleSurface.Chrome, new(side * width * .49f, 1.03f, pz),
                            new(side * width * .49f, 1.55f, pz + .15f), .022f);
                    }
            }
            else
            {
                g.Box(cargo, new(0, 2.07f, z), new(width, 2.04f, length), .07f);
                for (int side = -1; side <= 1; side += 2)
                    for (int rib = 0; rib < 16; rib++)
                        g.Box(CityVehicleSurface.Dark, new(side * (width * .5f + .005f),
                            1.19f + rib * .116f, z), new(.012f, .017f, length - .12f));
                for (int side = -1; side <= 1; side += 2)
                    g.Box(CityVehicleSurface.Dark, new(side * width * .23f, 2.01f, z - length * .5f - .02f),
                        new(.045f, 1.75f, .045f));
            }
        }

        static void Tank(CityVehicleGeometry g, float width, float length, float z)
        {
            float radius = width * .43f;
            g.Cylinder(CityVehicleSurface.Chrome, new(0, 1.94f, z), radius, length - .08f, false, 40);
            for (int band = 0; band < 4; band++)
            {
                float pz = z - length * .39f + band * length * .26f;
                g.Cylinder(CityVehicleSurface.Dark, new(0, 1.94f, pz), radius + .012f, .048f, false, 40);
                g.Box(CityVehicleSurface.Dark, new(0, .94f, pz), new(width * .74f, .21f, .14f));
            }
            g.Box(CityVehicleSurface.Chrome, new(0, 3.02f, z), new(.66f, .06f, length * .85f));
            for (int side = -1; side <= 1; side += 2)
            {
                g.Beam(CityVehicleSurface.Chrome, new(side * .34f, 3.24f, z - length * .42f),
                    new(side * .34f, 3.24f, z + length * .42f), .035f);
                for (int i = 0; i < 4; i++)
                {
                    float pz = z - length * .4f + i * length * .26f;
                    g.Beam(CityVehicleSurface.Chrome, new(side * .34f, 3.02f, pz),
                        new(side * .34f, 3.24f, pz), .035f);
                }
            }
            for (int rung = 0; rung < 7; rung++)
                g.Box(CityVehicleSurface.Chrome, new(0, 1.05f + rung * .28f, z - length * .5f),
                    new(.58f, .04f, .06f));
            for (int side = -1; side <= 1; side += 2)
                g.Box(CityVehicleSurface.Chrome, new(side * .3f, 1.91f, z - length * .5f), new(.04f, 1.85f, .06f));
        }
    }
}
