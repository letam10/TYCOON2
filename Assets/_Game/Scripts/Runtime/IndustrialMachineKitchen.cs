using UnityEngine;
using P = Tycoon.IndustrialMachineParts;

namespace Tycoon
{
    public static class IndustrialMachineKitchen
    {
        public static void Build(Transform body, string recipe)
        {
            P.Frame(body, 1.84f, 1.45f, .24f);
            P.Box(body, "RangeCabinet", new(0, .64f, .07f), new(1.78f, .76f, 1.34f), P.Sage);
            P.Box(body, "StainlessWorktop", new(0, 1.04f, .04f), new(1.91f, .08f, 1.43f));
            for (int side = -1; side <= 1; side += 2)
            {
                P.Panel(body, new(side * .45f, .62f, -.624f), new(.75f, .52f), P.Cream);
                P.Pipe(body, "CabinetPull", new(side * .45f - .14f, .78f, -.71f),
                    new(side * .45f + .14f, .78f, -.71f), .037f);
            }
            P.Box(body, "Splashback", new(0, 1.32f, .7f), new(1.83f, .5f, .055f));
            for (int i = 0; i < 4; i++)
                P.Cylinder(body, "HeatDial", new(-.63f + i * .42f, .94f, -.674f),
                    .083f, .043f, P.Iron, new(90, 0, 0));
            Hood(body);
            switch (recipe)
            {
                case "cook_sandwich":
                    SandwichPress(body);
                    break;
                case "cook_beef":
                    Griddle(body);
                    break;
                case "cook_vegetables":
                    Wok(body);
                    break;
                case "cook_soup":
                    Pot(body, new(.35f, 1.37f, .12f), .72f, .49f);
                    PrepBoard(body);
                    break;
                case "cook_pasta":
                    Pot(body, new(.34f, 1.37f, .15f), .68f, .49f);
                    P.Box(body, "PastaBasketHandle", new(.34f, 1.64f, -.39f), new(.08f, .06f, .36f), P.Iron);
                    P.Box(body, "BasketRim", new(.34f, 1.62f, .1f), new(.43f, .04f, .39f));
                    Pot(body, new(-.49f, 1.24f, .08f), .42f, .21f);
                    break;
                default:
                    Pot(body, new(.37f, 1.29f, .17f), .62f, .33f);
                    PrepBoard(body);
                    P.Box(body, "PlateShelf", new(-.46f, 1.23f, .47f), new(.59f, .07f, .29f), P.Cream);
                    break;
            }
        }

        static void Hood(Transform body)
        {
            P.Box(body, "ExtractorHood", new(0, 2.12f, .14f), new(1.97f, .18f, 1.29f));
            P.Box(body, "HoodFlue", new(0, 2.29f, .43f), new(.45f, .12f, .37f), P.Iron);
            for (int side = -1; side <= 1; side += 2)
                P.Box(body, "HoodSupport", new(side * .82f, 1.76f, .67f), new(.055f, .6f, .06f));
            for (int i = 0; i < 7; i++)
                P.Box(body, "GreaseBaffle", new(-.68f + i * .23f, 2.013f, .15f),
                    new(.08f, .035f, .82f), P.Iron);
        }

        static void Burner(Transform body, Vector3 at, float diameter)
        {
            P.Cylinder(body, "BurnerWell", at, diameter, .045f, P.Rubber);
            for (int i = 0; i < 2; i++)
                P.Box(body, "PanSupport", at + Vector3.up * .033f,
                    new(diameter, .035f, .045f), P.Iron, angle: new(0, i * 90, 0));
        }

        static void Pot(Transform body, Vector3 at, float diameter, float height)
        {
            Burner(body, new(at.x, 1.106f, at.z), diameter + .06f);
            IndustrialMachineVessels.Vat(body, at, diameter, height);
            for (int side = -1; side <= 1; side += 2)
                P.Pipe(body, "PotHandle", at + new Vector3(side * diameter * .46f, .06f, 0),
                    at + new Vector3(side * (diameter * .5f + .1f), .06f, 0), .053f, P.Iron);
        }

        static void PrepBoard(Transform body)
        {
            P.Box(body, "CuttingBoard", new(-.48f, 1.107f, -.15f), new(.67f, .05f, .61f), P.Copper, "wood");
            P.Box(body, "KnifeBlade", new(-.51f, 1.14f, -.15f), new(.32f, .014f, .075f));
            P.Box(body, "KnifeHandle", new(-.74f, 1.151f, -.15f), new(.17f, .029f, .05f), P.Iron);
        }

        static void SandwichPress(Transform body)
        {
            P.Box(body, "PressBase", new(.27f, 1.19f, .07f), new(.91f, .21f, .87f), P.Iron);
            P.Box(body, "PressUpperPlate", new(.27f, 1.53f, .28f), new(.87f, .09f, .8f),
                P.Steel, angle: new(-32, 0, 0));
            P.Cylinder(body, "PressHinge", new(.27f, 1.32f, .52f), .1f, .94f, P.Iron, new(0, 0, 90));
            P.Pipe(body, "PressHandle", new(-.03f, 1.8f, -.11f), new(.57f, 1.8f, -.11f), .07f, P.Iron);
            for (int side = -1; side <= 1; side += 2)
                P.Pipe(body, "PressHandleArm", new(.27f + side * .3f, 1.73f, -.05f),
                    new(.27f + side * .3f, 1.8f, -.11f), .04f);
            P.Box(body, "BreadBoard", new(-.6f, 1.107f, -.16f), new(.39f, .05f, .73f), P.Copper, "wood");
        }

        static void Griddle(Transform body)
        {
            P.Box(body, "GriddlePlate", new(.15f, 1.135f, .06f), new(1.25f, .1f, .89f), P.Iron);
            for (int i = 0; i < 10; i++)
                P.Box(body, "GriddleRidge", new(-.39f + i * .12f, 1.196f, .06f), new(.023f, .022f, .8f));
            P.Box(body, "GreaseChannel", new(.15f, 1.16f, -.44f), new(1.28f, .055f, .055f), P.Copper);
            P.Box(body, "SpatulaBlade", new(-.7f, 1.11f, -.08f), new(.16f, .014f, .2f));
            P.Box(body, "SpatulaHandle", new(-.7f, 1.13f, .13f), new(.055f, .04f, .24f), P.Iron);
        }

        static void Wok(Transform body)
        {
            Burner(body, new(.28f, 1.11f, .09f), .72f);
            IndustrialMachineVessels.Vat(body, new(.28f, 1.29f, .09f), .79f, .28f, P.Iron);
            P.Pipe(body, "WokHandle", new(.28f, 1.36f, -.27f), new(.28f, 1.43f, -.66f), .07f, P.Copper);
            PrepBoard(body);
        }
    }
}
