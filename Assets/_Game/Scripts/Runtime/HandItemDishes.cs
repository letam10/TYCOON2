using UnityEngine;

namespace Tycoon
{
    public static partial class HandItemModels
    {
        static void Dish(Transform t, string id)
        {
            if (id == "corn_soup")
            {
                Soup(t);
                return;
            }
            Sculpt(t, "Serving plate", "plate", Vector3.zero, new Vector3(.78f, .07f, .68f),
                "#D8DFD6", "ceramic");
            switch (id)
            {
                case "egg_sandwich": Sandwich(t);
                    break;
                case "pasta": Pasta(t);
                    break;
                case "beef_soy": GlazedBeef(t);
                    break;
                case "meal": Meal(t);
                    break;
                case "soy_vegetables": SoyVegetables(t);
                    break;
                default: throw new System.ArgumentException("Thiếu mẫu món ăn: " + id);
            }
        }

        static void Soup(Transform t)
        {
            Sculpt(t, "Soup bowl", "bowl", Vector3.zero, new Vector3(.66f, .25f, .66f),
                "#D7DACE", "ceramic");
            Disc(t, "Corn soup", new Vector3(0, .211f, 0), new Vector3(.55f, .008f, .55f), "#CDB566");
            for (int i = 0; i < 9; i++)
                Ball(t, "Corn in soup", new Vector3((i % 3 - 1) * .12f, .228f, (i / 3 - 1) * .115f),
                    new Vector3(.065f, .027f, .055f), "#E6CB76");
            for (int i = 0; i < 3; i++)
                Sculpt(t, "Soup herb leaf", "leaf", new Vector3((i - 1) * .12f, .235f, -.035f),
                    new Vector3(.06f, .09f, .045f), "#74904D", angle: new Vector3(90, i * 71, 0));
            Sculpt(t, "Cream swirl", "curl", new Vector3(0, .229f, 0),
                new Vector3(.40f, .04f, .40f), "#F0E2C8");
        }

        static void Sandwich(Transform t)
        {
            for (int i = 0; i < 2; i++)
            {
                float y = .048f + i * .17f;
                Sculpt(t, "Sandwich crust", "cloth", new Vector3(0, y, 0),
                    new Vector3(.48f, .075f, .37f), "#BD9458", "baked");
                Sculpt(t, "Sandwich crumb", "cloth", new Vector3(0, y + .018f, 0),
                    new Vector3(.43f, .063f, .32f), "#E2CEA6", "baked");
            }
            for (int i = 0; i < 3; i++)
                Sculpt(t, "Sandwich lettuce leaf", "leaf", new Vector3((i - 1) * .12f, .123f, -.18f),
                    new Vector3(.17f, .34f, .12f), "#719147", angle: new Vector3(90, i * 12 - 12, 0));
            Ball(t, "Fried egg white", new Vector3(0, .16f, 0), new Vector3(.43f, .06f, .32f), "#EEE0B1");
            Ball(t, "Egg yolk", new Vector3(.12f, .185f, -.08f), new Vector3(.14f, .055f, .13f), "#DDA743");
            for (int i = -1; i <= 1; i += 2)
                Disc(t, "Tomato slice", new Vector3(i * .11f, .198f, -.025f),
                    new Vector3(.20f, .011f, .21f), "#B7523E");
        }

        static void Pasta(Transform t)
        {
            Disc(t, "Tomato sauce", new Vector3(0, .049f, 0), new Vector3(.53f, .008f, .43f), "#B65B3B");
            for (int i = 0; i < 11; i++)
            {
                Vector3 p = new((i % 3 - 1) * .085f, .080f + i / 4 * .035f, (i / 3 - 1) * .055f);
                Sculpt(t, "Curved pasta ribbon", "curl", p, new Vector3(.30f, .55f, .28f),
                    i % 3 == 0 ? "#D4B878" : "#CDAE65", angle: new Vector3(0, i * 47, 0));
            }
            for (int i = 0; i < 5; i++)
                Sculpt(t, "Grated cheese curl", "score", new Vector3((i - 2) * .045f, .171f, 0),
                    new Vector3(.1f, .1f, .10f), "#E4D3A0", angle: new Vector3(0, i * 30, 0));
            Sculpt(t, "Basil leaf", "leaf", new Vector3(-.05f, .188f, -.035f),
                new Vector3(.12f, .17f, .08f), "#486E32", angle: new Vector3(85, -30, 0));
        }

        static void GlazedBeef(Transform t)
        {
            Disc(t, "Soy glaze", new Vector3(0, .049f, 0), new Vector3(.52f, .007f, .44f), "#44291E");
            for (int i = 0; i < 5; i++)
                Sculpt(t, "Glazed beef slice", "steak", new Vector3((i - 2) * .087f, .06f + i % 2 * .016f, 0),
                    new Vector3(.13f, .075f, .31f), "#79513A", "glass", new Vector3(0, -15, 0));
            for (int i = 0; i < 4; i++)
                Sculpt(t, "Scallion ring", "ring", new Vector3((i - 1.5f) * .075f, .155f, i % 2 * .07f - .035f),
                    new Vector3(.07f, .15f, .07f), "#799154");
            for (int i = 0; i < 6; i++)
                Ball(t, "Sesame seed", new Vector3((i % 3 - 1) * .105f, .144f, (i / 3 - .5f) * .15f),
                    new Vector3(.025f, .01f, .012f), "#E9DCA7");
        }

        static void Meal(Transform t)
        {
            Ball(t, "Rice mound", new Vector3(-.16f, .12f, 0), new Vector3(.30f, .17f, .32f), "#E6DFC5");
            for (int i = 0; i < 8; i++)
                Ball(t, "Rice grain", new Vector3(-.23f + i % 3 * .065f, .178f + i % 2 * .008f,
                    (i / 3 - 1) * .065f), new Vector3(.025f, .018f, .045f), "#F2E8D7");
            for (int i = 0; i < 4; i++)
                Sculpt(t, "Cooked carrot slice", "tomato", new Vector3(.08f + i % 2 * .115f, .065f,
                    (i / 2 - .5f) * .16f), new Vector3(.105f, .045f, .11f), "#C98743");
            for (int i = 0; i < 3; i++)
                Sculpt(t, "Broccoli crown", "wool", new Vector3(.07f + i * .08f, .09f, .06f),
                    new Vector3(.12f, .09f, .13f), "#7C964B");
        }

        static void SoyVegetables(Transform t)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                var p = new Vector3(Mathf.Cos(angle) * .11f, .06f + i % 3 * .021f, Mathf.Sin(angle) * .09f);
                Sculpt(t, "Cooked greens leaf", "leaf", p, new Vector3(.20f, .25f, .17f),
                    i % 2 == 0 ? "#7C964B" : "#719147", angle: new Vector3(78, -i * 45, 0));
            }
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 - .5f) * .24f;
                float z = (i / 2 - .5f) * .18f;
                Sculpt(t, "Cooked carrot slice", "tomato", new Vector3(x, .13f, z),
                    new Vector3(.10f, .04f, .10f), "#C98743");
                Ball(t, "Cooked soybean", new Vector3(x * .6f, .14f, z * 1.4f),
                    new Vector3(.065f, .055f, .06f), "#B9B976");
                Ball(t, "Cooked soybean", new Vector3(x * .8f, .15f, z * .6f),
                    new Vector3(.060f, .052f, .06f), "#D4C88F");
            }
        }
    }
}
