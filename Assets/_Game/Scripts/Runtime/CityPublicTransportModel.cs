using UnityEngine;

namespace Tycoon
{
    public static class CityPublicTransportModel
    {
        public static Transform Build(int kind, Transform root)
        {
            if (kind == 2)
            {
                var taxi = CityVehicleModel.BuildRoad(CityVehicleKind.Taxi, "#E7B92D", root);
                return taxi.RightDoors[0];
            }
            var rig = root.gameObject.AddComponent<CityVehicleRig>();
            rig.Kind = kind == 1 ? CityVehicleKind.Coach : CityVehicleKind.Bus;
            bool coach = kind == 1;
            float roof = coach ? 3.05f : 2.8f;
            var paint = CityVehicleSurface.Paint(coach ? "#E0DFCF" : "#328F91");
            var g = new CityVehicleGeometry();
            g.Box(CityVehicleSurface.Dark, new(0, .45f, 0), new(2.32f, .18f, 8.5f));
            g.Box(paint, new(0, roof, 0), new(2.37f, .17f, 8.35f), .06f);
            g.Box(paint, new(0, .89f, 4.22f), new(2.33f, .7f, .16f), .06f);
            g.Box(paint, new(0, 1.1f, -4.22f), new(2.33f, 1.1f, .16f), .06f);
            g.Box(CityVehicleSurface.Dark, new(0, .44f, 4.2f), new(2.35f, .17f, .18f));
            g.Box(CityVehicleSurface.Dark, new(0, .44f, -4.2f), new(2.35f, .17f, .18f));
            CityVehicleParts.Windshield(g, 1.12f, 1.08f, 1.24f, roof - .3f, 4.23f, 3.94f);
            CityVehicleParts.Windshield(g, -1.1f, -1.07f, 1.7f, roof - .24f, -4.23f, -4.12f);
            CityVehicleParts.Front(g, 2.45f, 4.305f, .87f);
            g.Box(CityVehicleSurface.Chrome, new(0, 1.52f, -4.31f), new(1.05f, .09f, .035f));
            g.Box(CityVehicleSurface.Stripe, new(0, 1.54f, 4.305f), new(1.2f, .08f, .035f));
            g.Box(CityVehicleSurface.Dark, new(0, roof - .17f, 4.07f), new(1.92f, .21f, .06f));
            for (int letter = 0; letter < 8; letter++)
                g.Box(CityVehicleSurface.Stripe, new(-.55f + letter * .16f, roof - .17f, 4.105f),
                    new(.085f, .08f, .015f));
            for (int side = -1; side <= 1; side += 2)
            {
                Side(g, paint, side, roof, coach);
                g.Beam(CityVehicleSurface.Stripe,
                    new(side * 1.235f, coach ? 1.48f : 1.36f, -3.55f),
                    new(side * 1.235f, coach ? 1.48f : 1.36f, 2.85f), .045f);
                CityVehicleParts.Mirror(g, side * 1.32f, roof - .62f, 3.75f);
                g.Box(CityVehicleSurface.Red, new(side * .92f, .93f, -4.315f), new(.18f, .36f, .035f));
                for (int row = 0; row < (coach ? 5 : 4); row++)
                    CityVehicleParts.Seat(g, new(side * .61f, .54f, -2.7f + row * 1.04f), coach, .62f);
            }
            CityVehicleParts.Seat(g, new(-.62f, .54f, 3.05f), true, .56f);
            g.Box(CityVehicleSurface.Dark, new(-.48f, 1.07f, 3.8f), new(1.1f, .23f, .42f));
            g.Cylinder(CityVehicleSurface.Dark, new(-.61f, 1.2f, 3.52f), .2f, .06f);
            g.Box(CityVehicleSurface.Dark, new(1.09f, .2f, 2.6f), new(.34f, .15f, 1.04f));
            g.Box(CityVehicleSurface.Chrome, new(.9f, .39f, 2.6f), new(.35f, .14f, 1.04f));
            if (coach)
            {
                g.Box(paint, new(0, roof + .11f, -.7f), new(1.3f, .18f, 1.9f), .05f);
                for (int side = -1; side <= 1; side += 2)
                    g.Box(CityVehicleSurface.WarmSeat, new(side * .88f, roof - .39f, -.4f), new(.34f, .07f, 6.5f));
            }
            else
            {
                for (int row = 0; row < 4; row++)
                    g.Beam(CityVehicleSurface.Stripe, new(.35f, .54f, -2.1f + row * 1.1f),
                        new(.35f, roof - .15f, -2.1f + row * 1.1f), .035f);
                g.Beam(CityVehicleSurface.Stripe, new(.35f, roof - .24f, -2.8f),
                    new(.35f, roof - .24f, 2.9f), .035f);
            }
            g.Build(coach ? "CoachHullLuggageInterior" : "CityBusHullInterior", root, rig);
            rig.Wheels = CityVehicleParts.Wheels(root, rig, 2.45f, .44f, -2.65f, 1.25f);
            rig.DoorInsideLocal = new(1.12f, .54f, 2.6f);
            rig.RightDoors = new[]
            {
                CityVehicleParts.Door(root, rig, 1, 1.222f, 2.6f, .54f, roof - .65f, 1.05f, paint, true)
            };
            rig.SetDoorOpen(0);
            CityVehicleParts.Collider(root, new(0, roof * .5f + .2f, 0), new(2.45f, roof - .3f, 8.6f));
            return rig.RightDoors[0];
        }

        static void Side(CityVehicleGeometry g, Material paint, int side, float roof, bool coach)
        {
            float x = side * 1.215f;
            float sill = coach ? 1.39f : 1.22f;
            for (int segment = 0; segment < 100; segment++)
            {
                float za = -4.2f + segment * .084f;
                float zb = za + .084f;
                if (side > 0 && zb > 2.055f && za < 3.145f) continue;
                float Bottom(float z)
                {
                    float distance = Mathf.Min(Mathf.Abs(z + 2.65f), Mathf.Abs(z - 1.25f));
                    return distance < .5f ? .44f + Mathf.Sqrt(.25f - distance * distance) : .52f;
                }
                g.Quad(paint, new(x, Bottom(za), za), new(x, Bottom(zb), zb),
                    new(x, sill, zb), new(x, sill, za));
                g.Quad(paint, new(x, sill, za), new(x, sill, zb), new(x, Bottom(zb), zb), new(x, Bottom(za), za));
            }
            foreach (float z in new[] { -2.65f, 1.25f })
                g.Arch(CityVehicleSurface.Dark, x + side * .008f, z, .44f, .485f, .055f);
            foreach (float z in new[] { -3.46f, -2.11f, -.76f, .59f, 1.71f, 3.65f })
            {
                if (side > 0 && z > 1.5f && z < 3.2f) continue;
                float span = z > 1.5f ? .86f : 1.21f;
                g.Box(CityVehicleSurface.Glass, new(x * .99f, (roof - .2f + sill) * .5f, z),
                    new(.025f, roof - .2f - sill, span));
                g.Box(paint, new(x, (roof + sill) * .5f, z - span * .5f - .04f),
                    new(.08f, roof - sill, .09f));
            }
            if (side < 0)
                g.Box(CityVehicleSurface.Glass, new(x * .99f, (roof + sill) * .5f - .1f, 2.68f),
                    new(.025f, roof - sill - .2f, .95f));
            g.Box(paint, new(x, roof - .12f, 0), new(.12f, .17f, 8.3f));
            if (coach)
            {
                for (int bay = 0; bay < 3; bay++)
                {
                    float z = -1.62f + bay * 1.03f;
                    g.Box(CityVehicleSurface.Dark, new(x + side * .008f, .94f, z), new(.013f, .64f, .018f));
                    g.Box(CityVehicleSurface.Chrome, new(x + side * .016f, 1.2f, z + .4f),
                        new(.035f, .04f, .16f));
                }
            }
            for (int vent = 0; vent < 7; vent++)
                g.Box(CityVehicleSurface.Dark, new(x + side * .008f, .69f + vent * .075f, -3.65f),
                    new(.018f, .024f, .7f));
        }
    }
}
