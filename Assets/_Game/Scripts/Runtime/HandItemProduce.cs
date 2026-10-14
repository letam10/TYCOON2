using UnityEngine;

namespace Tycoon
{
    public static partial class HandItemModels
    {
        static void Carrot(Transform t)
        {
            Part(t, "Tapered carrot", PrimitiveType.Sphere, new Vector3(0, .43f, 0),
                new Vector3(.28f, .86f, .28f), "#E88432");
            Ball(t, "Carrot crown", new Vector3(0, .846f, 0),
                new Vector3(.052f, .054f, .052f), "#527638");
            for (int i = 0; i < 4; i++)
                Part(t, "Carrot leaf", PrimitiveType.Cube, new Vector3(0, .84f, 0),
                    new Vector3(.20f, .23f, .12f), i % 2 == 0 ? "#507F36" : "#659542",
                    angle: new Vector3(i % 2 == 0 ? -12 : 12, i * 45, i * 18 - 27));
        }

        static void Tomato(Transform t)
        {
            Sculpt(t, "Ripe tomato lobes", "tomato", Vector3.zero, new Vector3(.68f, .52f, .65f), "#C64C3C");
            for (int i = 0; i < 5; i++)
                Sculpt(t, "Pointed calyx", "leaf", new Vector3(0, .49f, 0), new Vector3(.075f, .21f, .16f),
                    "#486E32", angle: new Vector3(78, i * 72, 0));
            Sculpt(t, "Tomato stem", "strand", new Vector3(0, .54f, 0), new Vector3(1, 1, .12f),
                "#586840", angle: new Vector3(90, 0, 0));
        }

        static void Wheat(Transform t)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * .075f;
                float height = .66f + .045f * (2 - Mathf.Abs(i - 2));
                Sculpt(t, "Wheat stalk", "strand", new Vector3(x, .32f, 0), new Vector3(.4f, .4f, .64f),
                    "#BE934B", angle: new Vector3(90, 0, 0));
                for (int j = 0; j < 5; j++)
                {
                    float side = j % 2 == 0 ? -1 : 1;
                    Part(t, "Wheat grain", PrimitiveType.Sphere, new Vector3(x + side * .028f,
                        height - j * .045f, .005f), new Vector3(.062f, .11f, .055f), "#DDBD74",
                        angle: new Vector3(0, 0, side * 32));
                }
                Sculpt(t, "Wheat awn", "strand", new Vector3(x, height + .055f, 0),
                    new Vector3(.12f, .12f, .19f), "#DDBD74", angle: new Vector3(90, 0, 0));
            }
            Sculpt(t, "Harvest twine", "ring", new Vector3(0, .22f, 0),
                new Vector3(.42f, .5f, .105f), "#92704F", "fiber");
        }

        static void Corn(Transform t)
        {
            Sculpt(t, "Cob", "pod", Vector3.zero, new Vector3(.28f, .72f, .28f), "#CC9F3F");
            for (int row = 0; row < 7; row++)
            {
                for (int side = 0; side < 6; side++)
                {
                    float angle = side * Mathf.PI / 3;
                    float radius = .12f * Mathf.Sin((row + 1) * Mathf.PI / 9);
                    Ball(t, "Kernel", new Vector3(Mathf.Cos(angle) * radius, .11f + row * .075f,
                        Mathf.Sin(angle) * radius), new Vector3(.085f, .072f, .085f), "#E9C65D");
                }
            }
            for (int side = -1; side <= 1; side += 2)
                Sculpt(t, "Curved husk", "leaf", new Vector3(side * .04f, .035f, .055f),
                    new Vector3(.19f, .46f, .3f), "#6F8541", angle: new Vector3(-10, side * 30, -side * 25));
        }

        static void Soybean(Transform t)
        {
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3((i - 1) * .15f, .10f, -.24f + i % 2 * .08f);
                Sculpt(t, "Soy pod with seed bulges", "pod", p, new Vector3(.18f, .52f, .18f),
                    "#8F9B52", angle: new Vector3(90, 0, 0));
                Sculpt(t, "Pod seam", "vein", p + new Vector3(0, .085f, .26f),
                    new Vector3(.15f, .2f, .49f), "#B9B976");
            }
            Ball(t, "Loose soybean", new Vector3(.25f, .08f, -.15f), new Vector3(.12f, .13f, .12f), "#D4C88F");
        }

        static void Milk(Transform t)
        {
            Sculpt(t, "Metal milk churn", "churn", Vector3.zero, new Vector3(.55f, .66f, .55f),
                "#B1BCBB", "metal");
            Disc(t, "Churn lid", new Vector3(0, .666f, 0), new Vector3(.40f, .016f, .40f), "#788B8F", "metal");
            Sculpt(t, "Lid loop", "handle", new Vector3(0, .69f, 0), new Vector3(.17f, .2f, .14f),
                "#6D7778", "metal", new Vector3(-90, 0, 0));
            for (int side = -1; side <= 1; side += 2)
                Sculpt(t, "Churn handle", "handle", new Vector3(side * .25f, .49f, 0),
                    new Vector3(.13f, .23f, .21f), "#6D7778", "metal", new Vector3(0, 0, side * 90));
            Disc(t, "Churn base rim", new Vector3(0, .035f, 0),
                new Vector3(.535f, .012f, .535f), "#788B8F", "metal");
        }

        static void Egg(Transform t)
        {
            Ball(t, "Egg shell", new Vector3(0, .29f, 0), new Vector3(.44f, .58f, .44f), "#E9D7B7");
        }

        static void Sack(Transform t, bool feed)
        {
            Sculpt(t, "Woven sack folds", "sack", Vector3.zero, new Vector3(.59f, .60f, .37f),
                feed ? "#AE9464" : "#DCCCAD", "fiber");
            Sculpt(t, "Gathered sack mouth", "cloth", new Vector3(0, .55f, 0),
                new Vector3(.30f, .10f, .18f), "#9E835D", "fiber");
            Sculpt(t, "Tied twine", "ring", new Vector3(0, .56f, 0),
                new Vector3(.22f, .25f, .13f), "#92704F", "fiber");
            Box(t, "Sack label", new Vector3(0, .30f, -.181f),
                new Vector3(.25f, .20f, .012f), feed ? "#567544" : "#8F6840");
            for (int i = 0; i < 3; i++)
                Ball(t, "Printed grain", new Vector3((i - 1) * .065f, .30f, -.192f),
                    new Vector3(.03f, .10f, .012f), "#E9DCA7");
        }

        static void Bottle(Transform t, string contents, string cap, bool tall)
        {
            float height = tall ? .89f : .76f;
            Sculpt(t, "Bottle shoulder and neck", "bottle", Vector3.zero,
                new Vector3(.40f, height, .40f), contents, "glass");
            Disc(t, "Screw lid", new Vector3(0, height, 0), new Vector3(.19f, .026f, .19f), cap, "metal");
            for (int i = 0; i < 3; i++)
                Sculpt(t, "Closure thread", "ring", new Vector3(0, height - .005f + i * .015f, 0),
                    new Vector3(.205f, .12f, .205f), cap, "metal");
            Box(t, "Paper label", new Vector3(0, height * .38f, -.182f),
                new Vector3(.25f, .22f, .012f), "#EEE2C5");
            Ball(t, "Label seal", new Vector3(0, height * .41f, -.192f),
                new Vector3(.09f, .08f, .012f), cap);
            for (int i = 0; i < 3; i++)
                Box(t, "Printed label line", new Vector3(0, height * .38f - .047f - i * .024f, -.193f),
                    new Vector3(.15f - i * .02f, .009f, .006f), "#876B43");
        }

        static void Cheese(Transform t)
        {
            Sculpt(t, "Cheese rind", "cheese", Vector3.zero, new Vector3(.80f, .035f, .58f), "#AD7C2A");
            Sculpt(t, "Cheese wedge", "cheese", new Vector3(0, .022f, 0),
                new Vector3(.78f, .25f, .56f), "#D7AD43");
            for (int i = 0; i < 4; i++)
                Ball(t, "Cheese pore", new Vector3(-.09f + i % 2 * .18f, .23f, (i / 2 - .5f) * .15f),
                    new Vector3(.07f, .012f, .054f), "#A87926");
        }

        static void Beef(Transform t)
        {
            Sculpt(t, "Steak fat rim", "steak", Vector3.zero, new Vector3(.72f, .13f, .49f), "#E3C0A6");
            Sculpt(t, "Beef cut", "steak", new Vector3(0, .055f, 0),
                new Vector3(.64f, .11f, .42f), "#AB483C");
            for (int i = 0; i < 4; i++)
                Sculpt(t, "Marbling vein", "vein", new Vector3((i - 1.5f) * .10f, .158f, 0),
                    new Vector3(.35f, .10f, .22f), "#D5A397", angle: new Vector3(0, i * 21, 0));
        }
    }
}
