using UnityEngine;
using P = Tycoon.IndustrialMachineParts;

namespace Tycoon
{
    public static class IndustrialMachineProcessing
    {
        public static void Mill(Transform body, Transform root, MachineStation station, bool feed)
        {
            P.Frame(body);
            if (feed)
            {
                for (int side = -1; side <= 1; side += 2)
                    P.Box(body, "DrumSaddle", new(side * .46f, .53f, .05f), new(.15f, .2f, .61f), P.Iron);
                P.Cylinder(body, "MixingDrum", new(0, 1, .05f), .85f, 1.28f, P.Sage, new(0, 0, 90));
                IndustrialMachineVessels.Hopper(body, new(-.38f, 1.7f, .05f), .62f, .4f);
                P.Box(body, "DrumInspectionHatch", new(.22f, 1.35f, .05f), new(.4f, .08f, .34f), P.Cream);
                P.Pipe(body, "DischargeChute", new(.28f, .77f, -.12f), new(.28f, .54f, -.57f), .25f);
                var rotor = P.Rotor(station, root, new(.73f, 1, .05f), new(0, 0, 90));
                P.Cylinder(rotor, "DrumDriveShaft", new(0, .06f, 0), .09f, .22f);
                P.Wheel(rotor, .58f);
                P.Motor(body, new(-.56f, .64f, .46f), .8f);
            }
            else
            {
                P.Box(body, "MillingTower", new(-.16f, .87f, .08f), new(.93f, .86f, .8f), P.Sage);
                IndustrialMachineVessels.Hopper(body, new(-.16f, 1.66f, .08f), 1.05f, .64f);
                P.Panel(body, new(-.16f, .85f, -.34f), new(.64f, .47f));
                P.Box(body, "FlourDischarge", new(-.13f, .53f, -.53f), new(.36f, .18f, .31f), P.Cream);
                var rotor = P.Rotor(station, root, new(.58f, 1.05f, .03f), new(0, 0, 90));
                P.Cylinder(rotor, "MillDriveShaft", new(0, .13f, 0), .1f, .36f);
                P.Wheel(rotor, .76f);
                P.Motor(body, new(.55f, .62f, .4f), .7f);
            }
            P.Controls(body, new(.66f, .79f, -.55f));
        }

        public static void Liquids(Transform body, Transform root, MachineStation station, string recipe)
        {
            P.Frame(body);
            if (recipe == "milkbottler")
            {
                Bottler(body, root, station);
                return;
            }
            bool cheese = recipe == "cheesemaker";
            bool soy = recipe == "soyextractor";
            Vector3 vat = new(-.27f, 1.04f, .1f);
            P.Box(body, "VesselCradle", new(-.27f, .52f, .1f), new(.91f, .12f, .85f), P.Iron);
            IndustrialMachineVessels.Vat(body, vat, 1.06f, .98f, soy ? P.Copper : P.Steel);
            P.Box(body, "MixerBridge", new(-.27f, 1.72f, .1f), new(1.13f, .12f, .2f), P.Sage);
            for (int side = -1; side <= 1; side += 2)
                P.Box(body, "BridgePost", new(-.27f + side * .49f, 1.44f, .1f), new(.09f, .52f, .13f));
            P.Cylinder(body, "GearMotor", new(-.27f, 1.85f, .1f), .26f, .2f, P.Iron);
            var rotor = P.Rotor(station, root, new(-.27f, 1.21f, .1f));
            P.Cylinder(rotor, "AgitatorShaft", new(0, .11f, 0), .065f, .82f);
            P.Box(rotor, cheese ? "CurdHarp" : soy ? "PressBlade" : "SaucePaddle",
                new(0, -.18f, 0), new(.81f, cheese ? .26f : .15f, .065f), P.Steel);
            if (cheese)
            {
                for (int i = 0; i < 7; i++)
                    P.Pipe(rotor, "CurdWire", new(-.34f + i * .11f, -.32f, -.09f),
                        new(-.34f + i * .11f, -.02f, -.09f), .016f);
                P.Box(body, "PressHousing", new(.64f, .95f, .21f), new(.4f, .85f, .48f), P.Cream);
                P.Cylinder(body, "PressScrew", new(.64f, 1.53f, .21f), .08f, .31f, P.Iron);
            }
            else if (soy)
            {
                P.Cylinder(body, "FilterColumn", new(.61f, 1.02f, .3f), .35f, 1.06f, P.Cream);
                for (int i = 0; i < 3; i++)
                    IndustrialMachineVessels.Ring(body, "FilterClamp", new(.61f, .64f + i * .38f, .3f),
                        .195f, .17f, .045f, P.Iron);
                P.Pipe(body, "FiltrateReturn", new(.21f, 1.48f, .3f), new(.61f, 1.48f, .3f));
            }
            else
            {
                P.Cylinder(body, "HeaterJacket", new(-.27f, .61f, .1f), 1.09f, .16f, P.Copper);
                P.Motor(body, new(.61f, .81f, .32f), .85f);
            }
            P.Pipe(body, "SanitaryOutlet", new(-.27f, .68f, -.39f), new(-.27f, .68f, -.67f), .1f);
            P.Cylinder(body, "ValveHandle", new(-.27f, .8f, -.59f), .2f, .03f, P.Copper);
            P.Controls(body, new(.65f, 1.04f, -.39f));
        }

        static void Bottler(Transform body, Transform root, MachineStation station)
        {
            P.Cylinder(body, "ReservoirStand", new(-.48f, .71f, .28f), .4f, .49f, P.Iron);
            P.Cylinder(body, "MilkReservoir", new(-.48f, 1.4f, .28f), .64f, .91f);
            P.Cylinder(body, "ReservoirCap", new(-.48f, 1.87f, .28f), .69f, .08f, P.Cream);
            P.Box(body, "FillingDeck", new(.13f, .81f, -.18f), new(1.45f, .15f, .63f), P.Sage);
            P.Box(body, "FillingGantry", new(.47f, 1.76f, .03f), new(.93f, .14f, .22f));
            P.Box(body, "GantryPost", new(.79f, 1.15f, .19f), new(.13f, 1.2f, .14f));
            P.Pipe(body, "MilkFeed", new(-.48f, 1.8f, .28f), new(.54f, 1.8f, .28f));
            for (int i = 0; i < 2; i++)
            {
                P.Pipe(body, "FillingDrop", new(.12f + i * .38f, 1.75f, .07f),
                    new(.12f + i * .38f, 1.49f, .07f));
                P.Cylinder(root, "Nozzle", new(.12f + i * .38f, 1.4f, .07f), .06f, .22f);
            }
            var rotor = P.Rotor(station, root, new(.28f, .93f, -.08f));
            P.Cylinder(rotor, "IndexingTurntable", Vector3.zero, .77f, .07f, P.Iron);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                IndustrialMachineVessels.Ring(rotor, "BottleLocator",
                    new(Mathf.Cos(a) * .27f, .055f, Mathf.Sin(a) * .27f), .07f, .055f, .04f);
            }
            P.Controls(body, new(-.65f, .8f, -.53f));
        }
    }
}
