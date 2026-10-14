using UnityEngine;
using P = Tycoon.IndustrialMachineParts;

namespace Tycoon
{
    public static class IndustrialMachineTextiles
    {
        public static void Build(Transform body, Transform root, MachineStation station, bool loom)
        {
            P.Frame(body, 1.86f, 1.44f);
            if (loom)
                Loom(body, root, station);
            else
                Spinner(body, root, station);
        }

        static void Loom(Transform body, Transform root, MachineStation station)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                P.Box(body, "LoomUpright", new(side * .81f, 1.13f, .26f), new(.13f, 1.42f, .16f), P.Sage);
                P.Box(body, "SideRail", new(side * .81f, .82f, -.04f), new(.12f, .12f, 1.13f), P.Sage);
            }
            P.Box(body, "HeddleCrossbeam", new(0, 1.77f, .26f), new(1.69f, .15f, .17f), P.Cream);
            P.Cylinder(body, "WarpBeam", new(0, 1.37f, .49f), .22f, 1.48f, P.Cream, new(0, 0, 90));
            for (int i = 0; i < 25; i++)
            {
                float x = -.69f + i * .057f;
                P.Pipe(body, "WarpThread", new(x, 1.38f, .47f), new(x, .97f, -.43f), .012f, P.Cream);
                P.Pipe(body, "HeddleEye", new(x, 1.12f, .02f), new(x, 1.59f, .02f), .013f, P.Steel);
            }
            var rotor = P.Rotor(station, root, new(0, .98f, -.45f), new(0, 0, 90));
            P.Cylinder(rotor, "ClothTakeupRoller", Vector3.zero, .23f, 1.49f, P.Iron);
            P.Box(root, "Shuttle", new(0, 1.17f, -.16f), new(.43f, .07f, .13f), P.Copper);
            P.Box(root, "WovenCloth", new(0, 1.08f, -.38f), new(1.27f, .025f, .31f), "#71A5B3", "fabric");
            P.Box(root, "Pedal", new(0, .3f, -.37f), new(.57f, .055f, .28f), P.Cream);
            P.Controls(body, new(.78f, 1.41f, -.16f));
            P.Motor(body, new(.47f, .65f, .34f), .7f);
        }

        static void Spinner(Transform body, Transform root, MachineStation station)
        {
            P.Box(body, "DriveColumn", new(-.45f, 1.01f, .24f), new(.26f, 1.18f, .35f), P.Sage);
            P.Box(body, "SpinningBed", new(0, .93f, .13f), new(1.58f, .17f, .54f), P.Cream);
            var rotor = P.Rotor(station, root, new(-.43f, 1.3f, -.19f), new(90, 0, 0));
            P.Cylinder(rotor, "FlywheelAxle", new(0, .17f, 0), .085f, .45f);
            P.Wheel(rotor, .91f, P.Copper);
            P.Cylinder(body, "SpindleShaft", new(.46f, 1.22f, .13f), .06f, .8f, P.Iron);
            for (int i = 0; i < 2; i++)
                P.Cylinder(body, "SpoolFlange", new(.46f, 1.03f + i * .43f, .13f), .39f, .055f, P.Cream);
            P.Cylinder(body, "WindingBobbin", new(.46f, 1.25f, .13f), .22f, .39f, P.Copper);
            P.Pipe(body, "ThreadGuidePost", new(.74f, 1.05f, .29f), new(.74f, 1.72f, .29f), .055f);
            P.Pipe(body, "ThreadGuideArm", new(.74f, 1.72f, .29f), new(.46f, 1.72f, .13f), .045f);
            IndustrialMachineVessels.Ring(body, "GuideEye", new(.46f, 1.7f, .13f), .055f, .035f, .04f);
            P.Pipe(body, "ThreadPath", new(.46f, 1.69f, .13f), new(.46f, 1.45f, .13f), .014f, P.Cream);
            P.Motor(body, new(-.45f, .66f, .35f), .8f);
            P.Controls(body, new(.62f, .72f, -.27f));
        }
    }
}
