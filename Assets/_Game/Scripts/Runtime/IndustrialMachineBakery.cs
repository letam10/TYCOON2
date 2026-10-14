using UnityEngine;
using P = Tycoon.IndustrialMachineParts;

namespace Tycoon
{
    public static class IndustrialMachineBakery
    {
        public static void Mixer(Transform body, Transform root, MachineStation station, bool cake)
        {
            P.Frame(body, 1.72f, 1.37f);
            P.Box(body, "MixerColumn", new(0, 1.18f, .45f), new(.48f, 1.4f, .36f), cake ? P.Cream : P.Sage);
            P.Box(body, "PlanetaryHead", new(0, 1.84f, .01f), new(.7f, .29f, 1.16f), cake ? P.Cream : P.Sage);
            P.Box(body, "BowlCradle", new(0, .56f, -.13f), new(1.04f, .17f, .94f), P.Iron);
            IndustrialMachineVessels.Vat(body, new(0, .96f, -.14f), 1.03f, .69f);
            var rotor = P.Rotor(station, root, new(0, 1.25f, -.14f));
            P.Cylinder(rotor, "MixingShaft", new(0, .14f, 0), .07f, .62f);
            if (cake)
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    Vector3 edge = new(Mathf.Cos(a) * .3f, -.22f, Mathf.Sin(a) * .3f);
                    P.Pipe(rotor, "WhiskWire", new(0, .05f, 0), edge, .024f);
                    P.Pipe(rotor, "WhiskReturn", edge, new(0, -.37f, 0), .024f);
                }
            }
            else
            {
                P.Pipe(rotor, "DoughHookArm", new(0, .01f, 0), new(.26f, -.08f, 0), .08f);
                P.Pipe(rotor, "DoughHookBend", new(.26f, -.08f, 0), new(.23f, -.32f, .13f), .08f);
                P.Pipe(rotor, "DoughHookTip", new(.23f, -.32f, .13f), new(.06f, -.36f, .19f), .08f);
            }
            for (int side = -1; side <= 1; side += 2)
                P.Pipe(body, "BowlHandle", new(side * .5f, 1.08f, -.22f),
                    new(side * .66f, 1.08f, -.22f), .07f);
            P.Box(body, "ControlBracket", new(.35f, 1.51f, .31f), new(.28f, .1f, .1f), P.Iron);
            P.Controls(body, new(.6f, 1.51f, .21f));
            P.Motor(body, new(0, 1.65f, .53f), .72f);
        }

        public static void Oven(Transform body, bool cake)
        {
            P.Frame(body, 1.86f, 1.49f, .3f);
            P.Box(body, "InsulatedOvenShell", new(0, 1.04f, .06f), new(1.77f, 1.44f, 1.39f), P.Cream);
            int doors = cake ? 1 : 2;
            for (int i = 0; i < doors; i++)
            {
                float y = cake ? 1.02f : .74f + i * .58f;
                float height = cake ? 1.05f : .49f;
                P.Box(body, "OvenDoorGasket", new(-.18f, y, -.65f), new(1.16f, height, .09f), P.Rubber);
                P.Box(body, "OvenDoorFrame", new(-.18f, y, -.708f), new(1.07f, height - .08f, .052f));
                P.Box(body, "ViewingWindow", new(-.18f, y, -.741f),
                    new(.8f, height - .21f, .02f), "#394C4C", "glass");
                P.Pipe(body, "DoorHandle", new(-.6f, y + height * .33f, -.81f),
                    new(.24f, y + height * .33f, -.81f), .055f);
                for (int side = -1; side <= 1; side += 2)
                    P.Pipe(body, "HandleStandOff", new(-.18f + side * .42f, y + height * .33f, -.73f),
                        new(-.18f + side * .42f, y + height * .33f, -.81f), .045f);
            }
            P.Controls(body, new(.63f, 1.42f, -.66f));
            P.Panel(body, new(.63f, .77f, -.66f), new(.32f, .53f), P.Cream);
            P.Box(body, "ExtractorCanopy", new(0, 2.02f, .09f), new(1.89f, .16f, 1.42f));
            for (int side = -1; side <= 1; side += 2)
                P.Box(body, "CanopySupport", new(side * .79f, 1.88f, .59f), new(.06f, .28f, .07f));
            P.Cylinder(body, "ExhaustDuct", new(0, 2.2f, .47f), .35f, .23f, P.Iron);
            for (int i = 0; i < 6; i++)
                P.Box(body, "ExtractorSlat", new(-.58f + i * .23f, 1.93f, .05f), new(.08f, .035f, .86f), P.Iron);
        }
    }
}
