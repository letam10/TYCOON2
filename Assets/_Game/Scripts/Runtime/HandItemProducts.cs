using UnityEngine;

namespace Tycoon
{
    public static partial class HandItemModels
    {
        static void Bread(Transform t)
        {
            Ball(t, "Baked loaf", new Vector3(0, .17f, 0), new Vector3(.75f, .34f, .37f), "#AF773D");
            for (int i = 0; i < 4; i++)
                Box(t, "Scored crust", new Vector3((i - 1.5f) * .14f, .30f, 0),
                    new Vector3(.046f, .018f, .24f), "#DCC296", angle: new Vector3(0, -25, 0));
        }
        static void Cake(Transform t)
        {
            Disc(t, "Cake base", new Vector3(0, .16f, 0), new Vector3(.64f, .15f, .64f), "#C49D73");
            Disc(t, "Cream filling", new Vector3(0, .17f, 0), new Vector3(.66f, .035f, .66f), "#F0E2C8");
            Disc(t, "Berry icing", new Vector3(0, .32f, 0), new Vector3(.66f, .035f, .66f), "#CA8190");
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2 / 5;
                Ball(t, "Cream rosette", new Vector3(Mathf.Cos(angle) * .20f, .39f, Mathf.Sin(angle) * .20f),
                    Vector3.one * .10f, "#F2E8D7");
            }
            Ball(t, "Cherry", new Vector3(0, .40f, 0), Vector3.one * .10f, "#A83B43");
        }
        static void Wool(Transform t)
        {
            for (int i = 0; i < 7; i++)
                Ball(t, "Raw wool tuft", new Vector3((i % 3 - 1) * .13f, .15f + i / 3 * .11f,
                    i % 2 * .14f), new Vector3(.3f, .27f, .29f), i % 2 == 0 ? "#DDD7C8" : "#EEE6D4");
        }
        static void Yarn(Transform t)
        {
            Disc(t, "Wooden bobbin", new Vector3(0, .27f, 0), new Vector3(.14f, .28f, .14f), "#8D6644");
            for (int i = 0; i < 8; i++)
                Disc(t, "Wound thread", new Vector3(0, .075f + i * .055f, 0),
                    new Vector3(.37f, .027f, .37f), i % 2 == 0 ? "#C9AF7F" : "#DAC59D");
            Disc(t, "Bobbin flange", new Vector3(0, .53f, 0), new Vector3(.23f, .018f, .23f), "#986D45");
        }
        static void Cloth(Transform t)
        {
            for (int i = 0; i < 4; i++)
                Box(t, "Folded cloth", new Vector3(i % 2 * .012f, .045f + i * .065f, 0),
                    new Vector3(.61f, .062f, .41f), i % 2 == 0 ? "#608B99" : "#7FA5AE");
            for (int i = 0; i < 5; i++)
                Box(t, "Woven seam", new Vector3((i - 2) * .1f, .274f, 0),
                    new Vector3(.012f, .006f, .38f), "#B0C4BC");
        }
        static void Dough(Transform t)
        {
            Box(t, "Proofing board", new Vector3(0, .025f, 0), new Vector3(.68f, .045f, .42f), "#8D704B");
            for (int i = -1; i <= 1; i += 2)
                Ball(t, "Dough roll", new Vector3(i * .16f, .16f, 0), new Vector3(.29f, .25f, .32f), "#D8C5A0");
        }
        static void Batter(Transform t)
        {
            Disc(t, "Mixing bowl", new Vector3(0, .17f, 0), new Vector3(.54f, .16f, .54f), "#839697", "metal");
            Disc(t, "Cake batter", new Vector3(0, .334f, 0), new Vector3(.47f, .012f, .47f), "#D4B878");
            Box(t, "Whisk handle", new Vector3(.12f, .49f, 0), new Vector3(.045f, .36f, .045f),
                "#9D7851", angle: new Vector3(0, 0, -25));
        }
        static void Dish(Transform t, string id)
        {
            if (id == "corn_soup")
            {
                Disc(t, "Soup bowl", new Vector3(0, .12f, 0), new Vector3(.65f, .12f, .65f), "#D7DACE");
                Disc(t, "Corn soup", new Vector3(0, .247f, 0), new Vector3(.57f, .01f, .57f), "#CDB566");
                for (int i = 0; i < 6; i++)
                    Ball(t, "Corn in soup", new Vector3((i % 3 - 1) * .13f, .26f, (i / 3 - .5f) * .17f),
                        new Vector3(.08f, .025f, .07f), i % 2 == 0 ? "#E6CB76" : "#74904D");
                return;
            }
            Disc(t, "Serving plate", new Vector3(0, .035f, 0), new Vector3(.78f, .03f, .68f), "#D8DFD6");
            Disc(t, "Plate inner glaze", new Vector3(0, .068f, 0),
                new Vector3(.68f, .005f, .58f), "#EDF0E5", "glass");
            if (id == "egg_sandwich")
            {
                for (int i = 0; i < 5; i++)
                    Box(t, "Sandwich layer", new Vector3(0, .08f + i * .043f, 0),
                        new Vector3(.46f, .04f, .35f),
                        i == 0 || i == 4 ? "#BD9458" : i == 1 ? "#719147" : i == 2 ? "#EEE0B1" : "#B7523E");
            }
            else if (id == "pasta")
            {
                for (int i = 0; i < 9; i++)
                    Box(t, "Pasta ribbon", new Vector3((i % 3 - 1) * .13f, .095f + i / 3 * .035f, 0),
                        new Vector3(.057f, .037f, .40f), i % 3 == 0 ? "#B65B3B" : "#CDAE65",
                        angle: new Vector3(0, i * 21, 0));
                Box(t, "Grated cheese", new Vector3(0, .24f, 0), new Vector3(.22f, .025f, .18f), "#E4D3A0");
            }
            else if (id == "beef_soy")
            {
                for (int i = 0; i < 4; i++)
                    Box(t, "Glazed beef", new Vector3((i % 2 - .5f) * .21f, .13f, (i / 2 - .5f) * .19f),
                        new Vector3(.20f, .14f, .17f), "#79513A");
                for (int i = 0; i < 3; i++)
                    Box(t, "Scallion", new Vector3((i - 1) * .11f, .22f, 0),
                        new Vector3(.03f, .02f, .30f), "#799154", angle: new Vector3(0, 30, 0));
            }
            else
            {
                if (id == "meal")
                    Ball(t, "Rice mound", new Vector3(-.16f, .13f, 0), new Vector3(.32f, .19f, .36f), "#E6DFC5");
                for (int i = 0; i < 8; i++)
                    Ball(t, "Cooked vegetables", new Vector3((i % 3 - 1) * .13f + (id == "meal" ? .13f : 0),
                        .115f + i / 6 * .08f, (i / 3 - 1) * .13f), new Vector3(.12f, .095f, .14f),
                        i % 3 == 0 ? "#C98743" : i % 3 == 1 ? "#7C964B" : "#BAB176");
            }
        }
        static void Cash(Transform t)
        {
            for (int i = 0; i < 5; i++)
                Box(t, "Banknote", new Vector3(0, .025f + i * .026f, 0), new Vector3(.63f, .02f, .30f),
                    i % 2 == 0 ? "#739574" : "#ADC1A0");
            Box(t, "Paper band", new Vector3(0, .087f, 0), new Vector3(.14f, .17f, .32f), "#DACDA5");
            Box(t, "Banknote portrait", new Vector3(.18f, .148f, 0), new Vector3(.10f, .006f, .15f), "#4F7359");
            for (int i = 0; i < 3; i++)
                Box(t, "Banknote ink", new Vector3(-.18f, .147f, (i - 1) * .045f),
                    new Vector3(.15f, .006f, .012f), "#365E42");
        }
        static void Crate(Transform t)
        {
            Box(t, "Crate interior", new Vector3(0, .20f, 0), new Vector3(.57f, .38f, .44f), "#806044");
            for (int i = 0; i < 3; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(t, "Wooden side slat", new Vector3(0, .08f + i * .14f, side * .24f),
                        new Vector3(.65f, .115f, .035f), "#B18B5B");
                    Box(t, "Wooden end slat", new Vector3(side * .31f, .08f + i * .14f, 0),
                        new Vector3(.035f, .115f, .48f), "#A17C50");
                }
            for (int side = -1; side <= 1; side += 2)
            {
                Box(t, "Corner brace", new Vector3(side * .23f, .23f, -.27f),
                    new Vector3(.075f, .46f, .03f), "#CAB080");
                for (int y = 0; y < 3; y++)
                    Ball(t, "Crate nail", new Vector3(side * .23f, .08f + y * .14f, -.29f),
                        new Vector3(.025f, .025f, .012f), "#4D5050");
            }
        }
    }
}
