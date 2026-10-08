using UnityEngine;

namespace Tycoon
{
    public static partial class HandItemModels
    {
        static void Carrot(Transform t)
        {
            Part(t, "Tapered carrot", PrimitiveType.Sphere, new Vector3(0, .34f, 0),
                new Vector3(.37f, .66f, .37f), "#E88432");
            for (int i = 0; i < 4; i++)
                Box(t, "Root crease", new Vector3(.015f, .22f + i * .095f, -.064f - i * .021f),
                    new Vector3(.075f + i * .015f, .012f, .016f), "#B96128");
            for (int i = 0; i < 4; i++)
                Box(t, "Carrot leaf", new Vector3((i - 1.5f) * .075f, .74f, 0),
                    new Vector3(.065f, .35f, .055f), "#507F36", angle: new Vector3(0, 0, i * 18 - 27));
        }
        static void Tomato(Transform t)
        {
            Ball(t, "Ripe tomato", new Vector3(0, .29f, 0), new Vector3(.68f, .54f, .65f), "#C64C3C");
            for (int i = 0; i < 5; i++)
                Box(t, "Calyx", new Vector3(0, .55f, 0), new Vector3(.38f, .035f, .055f),
                    "#486E32", angle: new Vector3(0, i * 72, 0));
            Box(t, "Stem", new Vector3(0, .61f, 0), new Vector3(.06f, .15f, .06f), "#586840");
        }
        static void Wheat(Transform t)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * .11f;
                Box(t, "Wheat stalk", new Vector3(x, .36f, 0), new Vector3(.025f, .68f, .025f), "#BE934B");
                for (int j = 0; j < 4; j++)
                    Ball(t, "Wheat grain", new Vector3(x + (j % 2 == 0 ? -.035f : .035f), .55f + j * .055f, 0),
                        new Vector3(.12f, .095f, .075f), "#DDBD74");
            }
            Box(t, "Harvest twine", new Vector3(0, .23f, 0), new Vector3(.53f, .07f, .09f), "#92704F");
        }
        static void Corn(Transform t)
        {
            Ball(t, "Cob", new Vector3(0, .38f, 0), new Vector3(.38f, .7f, .38f), "#CC9F3F");
            for (int y = 0; y < 7; y++)
                for (int x = 0; x < 6; x++)
                {
                    float angle = x * Mathf.PI / 3;
                    Ball(t, "Kernel", new Vector3(Mathf.Cos(angle) * .15f, .15f + y * .075f,
                        Mathf.Sin(angle) * .15f), new Vector3(.105f, .085f, .105f), "#E9C65D");
                }
            for (int i = -1; i <= 1; i += 2)
                Box(t, "Husk", new Vector3(i * .17f, .19f, .04f), new Vector3(.1f, .43f, .14f),
                    "#6F8541", angle: new Vector3(0, 0, -i * 21));
        }
        static void Soybean(Transform t)
        {
            for (int i = 0; i < 3; i++)
            {
                Ball(t, "Soy pod", new Vector3((i - 1) * .15f, .12f, i % 2 * .12f),
                    new Vector3(.19f, .22f, .57f), "#8F9B52");
                for (int j = 0; j < 3; j++)
                    Ball(t, "Pod seed", new Vector3((i - 1) * .15f, .17f, -.15f + j * .14f + i % 2 * .12f),
                        new Vector3(.13f, .15f, .15f), "#B9B976");
            }
        }
        static void Milk(Transform t)
        {
            Disc(t, "Metal milk churn", new Vector3(0, .26f, 0), new Vector3(.48f, .24f, .48f), "#B1BCBB", "metal");
            Disc(t, "Churn shoulder", new Vector3(0, .53f, 0), new Vector3(.39f, .04f, .39f), "#D0D7D0", "metal");
            Disc(t, "Churn lid", new Vector3(0, .60f, 0), new Vector3(.34f, .025f, .34f), "#788B8F", "metal");
            Box(t, "Blue milk band", new Vector3(0, .28f, -.245f), new Vector3(.29f, .15f, .015f), "#6C96A1");
            for (int side = -1; side <= 1; side += 2)
                Box(t, "Churn handle", new Vector3(side * .27f, .46f, 0),
                    new Vector3(.09f, .14f, .06f), "#6D7778", "metal");
        }
        static void Egg(Transform t)
        {
            Ball(t, "Egg shell", new Vector3(0, .29f, 0), new Vector3(.44f, .58f, .44f), "#E9D7B7");
        }
        static void Sack(Transform t, bool feed)
        {
            Ball(t, "Woven sack", new Vector3(0, .3f, 0), new Vector3(.55f, .62f, .38f), feed ? "#AE9464" : "#DCCCAD");
            Box(t, "Folded seam", new Vector3(0, .59f, 0), new Vector3(.36f, .075f, .18f), "#9E835D");
            Box(t, "Sack label", new Vector3(0, .3f, -.19f),
                new Vector3(.29f, .24f, .02f), feed ? "#567544" : "#8F6840");
            for (int i = 0; i < 3; i++)
                Ball(t, "Printed grain", new Vector3((i - 1) * .07f, .3f, -.21f),
                    new Vector3(.04f, .13f, .025f), "#E9DCA7");
        }
        static void Bottle(Transform t, string contents, string cap, bool tall)
        {
            float height = tall ? .38f : .3f;
            Disc(t, "Bottle body", new Vector3(0, height, 0), new Vector3(.4f, height, .4f), contents, "glass");
            Ball(t, "Glass shoulder", new Vector3(0, height * 2, 0), new Vector3(.39f, .2f, .39f), contents);
            Disc(t, "Bottle neck", new Vector3(0, height * 2 + .10f, 0),
                new Vector3(.21f, .10f, .21f), contents, "glass");
            Disc(t, "Screw lid", new Vector3(0, height * 2 + .21f, 0), new Vector3(.24f, .035f, .24f), cap);
            Box(t, "Paper label", new Vector3(0, height, -.20f), new Vector3(.28f, .22f, .018f), "#EEE2C5");
            Box(t, "Label mark", new Vector3(0, height, -.213f), new Vector3(.13f, .07f, .013f), cap);
            for (int i = 0; i < 3; i++)
                Box(t, "Printed label line", new Vector3(0, height - .055f - i * .025f, -.215f),
                    new Vector3(.16f - i * .025f, .009f, .009f), "#876B43");
        }
        static void Cheese(Transform t)
        {
            Box(t, "Cheese block", new Vector3(0, .16f, 0), new Vector3(.60f, .3f, .43f), "#D7AD43");
            Box(t, "Cheese rind", new Vector3(0, .035f, 0), new Vector3(.61f, .045f, .44f), "#AD7C2A");
            for (int i = 0; i < 3; i++)
                Disc(t, "Cheese holes", new Vector3((i - 1) * .18f, .317f, i % 2 * .12f - .06f),
                    new Vector3(.09f, .004f, .09f), "#A87926");
        }
        static void Beef(Transform t)
        {
            Ball(t, "Steak fat rim", new Vector3(0, .1f, 0), new Vector3(.68f, .20f, .46f), "#E3C0A6");
            Ball(t, "Beef cut", new Vector3(0, .14f, 0), new Vector3(.61f, .14f, .39f), "#AB483C");
            for (int i = 0; i < 3; i++)
                Box(t, "Marbling", new Vector3((i - 1) * .13f, .204f, 0), new Vector3(.026f, .01f, .26f),
                    "#D5A397", angle: new Vector3(0, 20 + i * 11, 0));
        }
    }
}
