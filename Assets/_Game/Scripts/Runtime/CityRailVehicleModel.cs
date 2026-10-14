using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityRailVehicleModel
    {
        public static Transform Build(Transform parent, string color)
        {
            var root = TownModelParts.Root("LocomotiveAndTwoPassengerCoaches", Vector3.zero, parent);
            var rig = root.gameObject.AddComponent<CityVehicleRig>();
            rig.Kind = CityVehicleKind.Train;
            rig.WheelRadius = .38f;
            rig.DoorInsideLocal = new(1.3f, .6f, -10);
            var wheels = new List<Transform>();
            var left = new List<Transform>();
            var right = new List<Transform>();
            var g = new CityVehicleGeometry();
            var paint = CityVehicleSurface.Paint(color);
            Locomotive(g, paint);
            Unit("Locomotive", 0, 8.8f);
            for (int coach = 0; coach < 2; coach++)
            {
                float z = -9.55f - coach * 9.85f;
                float door = z - .45f;
                CityRailVehicleModelCoach.Build(g, paint, z, door);
                Unit("PassengerCoach" + (coach + 1), z, 9.4f);
                left.Add(CityVehicleParts.Door(root, rig, -1, 1.29f, door, .6f, 2.3f, 1.04f, paint, true));
                right.Add(CityVehicleParts.Door(root, rig, 1, 1.29f, door, .6f, 2.3f, 1.04f, paint, true));
                float gap = z + 4.925f;
                g.Box(CityVehicleSurface.Dark, new(0, 1.86f, gap), new(1.64f, 2.37f, .43f), .02f);
                for (int rib = 0; rib < 5; rib++)
                    g.Box(CityVehicleSurface.Rubber, new(0, 1.86f, gap - .2f + rib * .1f),
                        new(1.72f, 2.43f, .035f));
                g.Beam(CityVehicleSurface.Chrome, new(0, .47f, gap - .32f), new(0, .47f, gap + .32f), .15f);
            }
            g.Build("RailHullWindowsInterior", root, rig);
            rig.Wheels = wheels.ToArray();
            rig.LeftDoors = left.ToArray();
            rig.RightDoors = right.ToArray();
            rig.SetDoorOpen(0);
            return root;

            void Unit(string name, float z, float length)
            {
                var unit = TownModelParts.Root(name, new(0, 0, z), root);
                CityVehicleParts.Collider(unit, new(0, 1.86f, 0), new(2.56f, 2.64f, length));
                foreach (float bogie in new[] { z - length * .31f, z + length * .31f })
                {
                    g.Box(CityVehicleSurface.Dark, new(0, .52f, bogie), new(2.16f, .22f, 1.55f));
                    for (int side = -1; side <= 1; side += 2)
                    {
                        g.Box(CityVehicleSurface.Dark, new(side * 1.06f, .48f, bogie), new(.18f, .27f, 1.42f));
                        foreach (float offset in new[] { -.48f, .48f })
                            g.Cylinder(CityVehicleSurface.Chrome, new(side * 1.16f, .5f, bogie + offset),
                                .14f, .06f, true, 16);
                    }
                    foreach (float axle in new[] { bogie - .48f, bogie + .48f })
                        wheels.Add(Axle(root, rig, axle));
                }
            }
        }

        static Transform Axle(Transform root, CityVehicleRig rig, float z)
        {
            var pivot = TownModelParts.Root("TrainWheelAxlePivot", new(0, .38f, z), root);
            var g = new CityVehicleGeometry();
            g.Cylinder(CityVehicleSurface.Dark, Vector3.zero, .09f, 2.02f, true, 16);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Cylinder(CityVehicleSurface.Dark, new(side * .72f, 0, 0), .38f, .16f, true, 28);
                g.Cylinder(CityVehicleSurface.Chrome, new(side * .805f, 0, 0), .29f, .028f, true, 28);
                g.Cylinder(CityVehicleSurface.Dark, new(side * .824f, 0, 0), .13f, .037f, true, 20);
            }
            g.BuildSingle("SteelWheelPair", pivot, rig);
            return pivot;
        }

        static void Locomotive(CityVehicleGeometry g, Material paint)
        {
            g.Box(CityVehicleSurface.Dark, new(0, .65f, 0), new(2.55f, .24f, 8.7f));
            Vector2[] nose =
            {
                new(-1.1f, -.4f), new(1.1f, -.4f), new(1.27f, -.2f), new(1.22f, .35f),
                new(.94f, .47f), new(-.94f, .47f), new(-1.22f, .35f), new(-1.27f, -.2f)
            };
            g.Loft(paint, new(0, 1.22f, 0), new[] { -4.34f, -4.1f, 3.82f, 4.35f },
                nose, new[] { .93f, 1f, 1f, .86f });
            g.Box(paint, new(0, 2.23f, -.77f), new(2.45f, 1.45f, 6.74f), .07f);
            CityRailVehicleModelCoach.Roof(g, paint, 0, 8.65f);
            CityVehicleParts.Windshield(g, 1.12f, 1.01f, 1.75f, 2.91f, 4.24f, 3.68f);
            g.Beam(paint, new(0, 1.76f, 4.25f), new(0, 2.91f, 3.69f), .085f);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(CityVehicleSurface.Glass, new(side * 1.235f, 2.36f, 3.03f), new(.025f, .92f, 1.02f));
                g.Box(CityVehicleSurface.Stripe, new(side * 1.24f, 1.58f, 0), new(.035f, .16f, 8.1f));
                g.Cylinder(CityVehicleSurface.Dark, new(side * .83f, 1.22f, 4.36f), .205f, .08f);
                g.Cylinder(CityVehicleSurface.Light, new(side * .83f, 1.22f, 4.411f), .145f, .025f);
                for (int vent = 0; vent < 19; vent++)
                    g.Box(CityVehicleSurface.Dark, new(side * 1.237f, 2.23f, -3.6f + vent * .26f),
                        new(.022f, .92f, .04f));
                g.Box(CityVehicleSurface.Chrome, new(side * .95f, .73f, 4.18f), new(.18f, .2f, .24f));
                g.Beam(CityVehicleSurface.Chrome, new(side * 1.05f, .9f, 3.91f),
                    new(side * 1.05f, 1.75f, 3.91f), .035f);
            }
            g.Box(CityVehicleSurface.Red, new(0, .57f, 4.29f), new(2.1f, .43f, .16f), .05f);
            g.Box(CityVehicleSurface.Dark, new(0, .48f, 4.46f), new(.3f, .19f, .34f));
            g.Cylinder(CityVehicleSurface.Light, new(0, 2.99f, 3.79f), .14f, .08f);
            g.Box(CityVehicleSurface.Dark, new(0, 3.35f, -1.75f), new(.58f, .2f, .84f));
            CityVehicleParts.Seat(g, new(-.6f, 1.14f, 3.15f), false, .56f);
        }
    }
}
