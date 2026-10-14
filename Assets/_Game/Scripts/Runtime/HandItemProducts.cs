using UnityEngine;

namespace Tycoon
{
    public static partial class HandItemModels
    {
        static void Bread(Transform t)
        {
            Sculpt(t, "Baked loaf", "loaf", Vector3.zero, new Vector3(.77f, .31f, .38f), "#AF773D");
            for (int i = 0; i < 4; i++)
                Sculpt(t, "Scored crust", "score", new Vector3((i - 1.5f) * .13f, .268f, 0),
                    new Vector3(.38f, .28f, .28f), "#DCC296");
        }

        static void Cake(Transform t)
        {
            for (int i = 0; i < 3; i++)
            {
                Disc(t, "Cake base sponge", new Vector3(0, .07f + i * .105f, 0),
                    new Vector3(.60f, .047f, .60f), "#C49D73");
                Disc(t, "Cream filling", new Vector3(0, .115f + i * .105f, 0),
                    new Vector3(.61f, .014f, .61f), "#F0E2C8");
            }
            Disc(t, "Berry icing", new Vector3(0, .345f, 0), new Vector3(.62f, .018f, .62f), "#CA8190");
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3;
                Vector3 p = new(Mathf.Cos(angle) * .205f, .361f, Mathf.Sin(angle) * .205f);
                Sculpt(t, "Piped cream rosette", "tomato", p, new Vector3(.115f, .075f, .115f), "#F2E8D7");
                Sculpt(t, "Strawberry", "pod", p + Vector3.up * .054f,
                    new Vector3(.065f, .095f, .065f), "#A83B43", "berry");
            }
        }

        static void Wool(Transform t)
        {
            for (int i = 0; i < 7; i++)
                Sculpt(t, "Raw wool tuft", "wool", new Vector3((i % 3 - 1) * .13f,
                    i / 3 * .09f, (i % 2 - .5f) * .13f), new Vector3(.30f, .26f, .29f),
                    i % 2 == 0 ? "#DDD7C8" : "#EEE6D4", "fiber");
            for (int i = 0; i < 3; i++)
                Sculpt(t, "Wool curl", "curl", new Vector3((i - 1) * .12f, .32f, 0),
                    new Vector3(.15f, .15f, .15f), "#EEE6D4", "fiber");
        }

        static void Yarn(Transform t)
        {
            Disc(t, "Wooden bobbin", new Vector3(0, .27f, 0), new Vector3(.12f, .27f, .12f), "#8D6644");
            Sculpt(t, "Wound thread core", "loaf", new Vector3(0, .05f, 0),
                new Vector3(.34f, .43f, .34f), "#C9AF7F", "fiber");
            for (int i = 0; i < 12; i++)
                Sculpt(t, "Wound thread", "ring", new Vector3(0, .075f + i * .033f, 0),
                    new Vector3(.38f, .25f, .38f), i % 2 == 0 ? "#C9AF7F" : "#DAC59D", "fiber");
            for (int i = 0; i < 2; i++)
                Disc(t, "Bobbin flange", new Vector3(0, .025f + i * .49f, 0),
                    new Vector3(.28f, .015f, .28f), "#986D45");
        }

        static void Cloth(Transform t)
        {
            for (int i = 0; i < 4; i++)
                Sculpt(t, "Folded cloth", "cloth", new Vector3(i % 2 * .012f, i * .042f, 0),
                    new Vector3(.61f, .064f, .40f), i % 2 == 0 ? "#608B99" : "#7FA5AE", "fiber");
            for (int i = -1; i <= 1; i += 2)
                Sculpt(t, "Woven seam", "strand", new Vector3(i * .22f, .183f, 0),
                    new Vector3(.18f, .15f, .31f), "#B0C4BC", "fiber");
        }

        static void Dough(Transform t)
        {
            Sculpt(t, "Proofing board", "cloth", Vector3.zero, new Vector3(.69f, .035f, .44f), "#8D704B", "wood");
            for (int i = -1; i <= 1; i += 2)
                Sculpt(t, "Dough roll", "loaf", new Vector3(i * .16f, .03f, 0),
                    new Vector3(.29f, .22f, .32f), "#D8C5A0", "dough");
            for (int i = 0; i < 3; i++)
                Sculpt(t, "Flour dust on dough", "score", new Vector3(-.16f, .22f, (i - 1) * .035f),
                    new Vector3(.12f, .08f, .18f), "#EEE2C5", "dough");
        }

        static void Batter(Transform t)
        {
            Sculpt(t, "Mixing bowl", "bowl", Vector3.zero, new Vector3(.58f, .29f, .58f), "#839697", "metal");
            Disc(t, "Cake batter", new Vector3(0, .245f, 0), new Vector3(.48f, .008f, .48f), "#D4B878");
            Sculpt(t, "Batter swirl", "curl", new Vector3(0, .257f, 0),
                new Vector3(.35f, .10f, .35f), "#E4D3A0");
            Sculpt(t, "Whisk handle", "strand", new Vector3(.15f, .36f, 0), new Vector3(.7f, .7f, .30f),
                "#9D7851", "wood", new Vector3(65, 0, -25));
            for (int i = 0; i < 3; i++)
                Sculpt(t, "Whisk wire", "ring", new Vector3(.11f, .27f, 0), new Vector3(.11f, .11f, .16f),
                    "#B1BCBB", "metal", new Vector3(80, i * 60, 0));
        }

        static void Cash(Transform t)
        {
            for (int i = 0; i < 6; i++)
                Sculpt(t, "Banknote", "cloth", new Vector3(0, i * .018f, 0), new Vector3(.63f, .019f, .30f),
                    i % 2 == 0 ? "#739574" : "#ADC1A0", "paper");
            Box(t, "Paper band", new Vector3(0, .057f, 0), new Vector3(.12f, .12f, .30f), "#DACDA5");
            Ball(t, "Banknote portrait", new Vector3(.16f, .110f, 0), new Vector3(.12f, .004f, .13f), "#4F7359");
            for (int i = 0; i < 3; i++)
                Box(t, "Banknote ink", new Vector3(-.16f, .11f, (i - 1) * .04f),
                    new Vector3(.14f, .004f, .008f), "#365E42");
        }

        static void Crate(Transform t)
        {
            Box(t, "Wooden crate floor", new Vector3(0, .035f, 0), new Vector3(.62f, .055f, .47f), "#806044");
            for (int i = 0; i < 3; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(t, "Wooden side slat", new Vector3(0, .10f + i * .12f, side * .24f),
                        new Vector3(.65f, .095f, .035f), "#B18B5B");
                    Box(t, "Wooden end slat", new Vector3(side * .31f, .10f + i * .12f, 0),
                        new Vector3(.035f, .095f, .48f), "#A17C50");
                }
            }
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Box(t, "Corner brace", new Vector3(x * .265f, .215f, z * .255f),
                        new Vector3(.065f, .39f, .035f), "#CAB080");
                    for (int y = 0; y < 3; y++)
                        Ball(t, "Iron nail", new Vector3(x * .265f, .10f + y * .12f, z * .275f),
                            new Vector3(.018f, .018f, .009f), "#4D5050");
                }
            }
        }
    }
}
