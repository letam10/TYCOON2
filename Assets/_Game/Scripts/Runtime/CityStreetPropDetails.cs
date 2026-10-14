using UnityEngine;

namespace Tycoon
{
    public static class CityStreetPropDetails
    {
        const string Metal = "#647D7B";
        const string Dark = "#58685E";
        const string Wood = "#817154";
        const string Stone = "#AAA89A";

        public static void Bench(CityStaticGeometry g, Vector3 p)
        {
            for (int slat = 0; slat < 4; slat++)
                CityCivicDetails.ChamferedBox(g, p + new Vector3(0, .55f, -.285f + slat * .19f),
                    new(2.5f, .1f, .15f), "wood", Wood);
            for (int slat = 0; slat < 3; slat++)
                CityCivicDetails.ChamferedBox(g, p + new Vector3(0, .8f + slat * .17f, .35f),
                    new(2.5f, .13f, .1f), "wood", Wood);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * .95f;
                g.Box(p + new Vector3(x, .04f, 0), new(.3f, .08f, .72f), "metal", Dark);
                g.Box(p + new Vector3(x, .28f, -.24f), new(.1f, .5f, .1f), "metal", Metal);
                g.Box(p + new Vector3(x, .62f, .35f), new(.1f, 1.18f, .1f), "metal", Metal);
                g.Box(p + new Vector3(x, .46f, .04f), new(.12f, .08f, .72f), "metal", Metal);
                g.Box(p + new Vector3(x, .81f, .02f), new(.14f, .08f, .74f), "wood", Wood);
                g.Box(p + new Vector3(x, .66f, -.27f), new(.08f, .26f, .08f), "metal", Metal);
                for (int end = -1; end <= 1; end += 2)
                    g.Cylinder(p + new Vector3(x, .088f, end * .26f), .045f, .016f, "metal", Metal);
                for (int slat = 0; slat < 3; slat++)
                    g.Box(p + new Vector3(x, .8f + slat * .17f, .292f),
                        new(.045f, .045f, .016f), "metal", Dark);
            }
            g.Box(p + new Vector3(0, .26f, .28f), new(1.9f, .08f, .08f), "metal", Metal);
            g.Collider("Bench", p + new Vector3(0, .605f, .02f), new(2.5f, 1.21f, .8f));
        }

        public static void Planter(CityStaticGeometry g, Vector3 p, float width)
        {
            const string Brick = "#A46E59";
            const string Rim = "#B5A28D";
            g.Box(p + Vector3.up * .08f, new(width, .16f, 1), "stone", Rim);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(p + new Vector3(0, .36f, side * .43f), new(width, .48f, .14f), "brick", Brick);
                g.Box(p + new Vector3(side * (width * .5f - .07f), .36f, 0),
                    new(.14f, .48f, 1), "brick", Brick);
                g.Box(p + new Vector3(0, .61f, side * .46f), new(width + .12f, .14f, .2f), "stone", Rim);
                g.Box(p + new Vector3(side * (width * .5f - .04f), .61f, 0),
                    new(.2f, .14f, .76f), "stone", Rim);
                g.Box(p + new Vector3(0, .31f, side * .505f), new(width, .03f, .018f), "stone", Rim);
            }
            g.Box(p + Vector3.up * .52f, new(width - .28f, .05f, .7f), "soil", "#685239");
            g.Collider("Planter", p + Vector3.up * .34f, new(width + .12f, .68f, 1.12f));
            int shrubs = Mathf.Max(1, Mathf.FloorToInt((width - .2f) / .65f));
            for (int i = 0; i < shrubs; i++)
            {
                float x = (i - (shrubs - 1) * .5f) * .65f;
                g.Cylinder(p + new Vector3(x, .67f, 0), .03f, .27f, "wood", Wood);
                g.Crown(p + new Vector3(x, .85f, 0), new(.72f, .65f, .7f), "#54814F");
                g.Crown(p + new Vector3(x + .1f, 1.1f, -.1f), new(.22f, .2f, .24f), "#C99A80");
            }
        }

        public static void Lamp(CityStaticGeometry g, Vector3 p)
        {
            g.Cylinder(p + Vector3.up * .12f, .25f, .24f, "stone", Stone);
            g.Cylinder(p + Vector3.up * .26f, .18f, .09f, "metal", Dark);
            g.Cylinder(p + Vector3.up * .55f, .12f, .6f, "metal", Metal);
            g.Cone(p + Vector3.up * .91f, .12f, .18f, "metal", Metal);
            g.Cylinder(p + Vector3.up * 2.71f, .075f, 3.72f, "metal", Metal);
            g.Cylinder(p + Vector3.up * 4.54f, .11f, .12f, "metal", Dark);
            g.Beam(p + Vector3.up * 4.56f, p + new Vector3(.8f, 4.56f, 0), .1f, "metal", Metal);
            g.Beam(p + Vector3.up * 4.12f, p + new Vector3(.57f, 4.53f, 0), .065f, "metal", Metal);
            g.Box(p + new Vector3(.8f, 4.48f, 0), new(.7f, .16f, .45f), "metal", Dark);
            g.Box(p + new Vector3(.8f, 4.39f, 0), new(.58f, .035f, .33f), "glass", "#E1DFCC");
            g.Box(p + new Vector3(0, .55f, -.12f), new(.11f, .27f, .035f), "metal", Dark);
            for (int side = -1; side <= 1; side += 2)
                g.Cylinder(p + new Vector3(side * .17f, .251f, 0), .025f, .022f, "metal", Metal);
            g.Collider("LampBase", p + Vector3.up * .15f, new(.5f, .3f, .5f));
            g.Collider("LampPost", p + Vector3.up * 2.3f, new(.2f, 4.6f, .2f));
        }

        public static void Fountain(CityStaticGeometry g, Vector3 p)
        {
            g.Cylinder(p + Vector3.up * .18f, 3.2f, .3f, "stone", Stone);
            g.Cylinder(p + Vector3.up * .38f, 2.7f, .18f, "water", "#548E90");
            for (int segment = 0; segment < 24; segment++)
            {
                float angle = segment * 15;
                Quaternion r = Quaternion.Euler(0, angle, 0);
                g.RotatedBox(p + r * new Vector3(0, .46f, 2.98f), new(.8f, .26f, .38f),
                    r, "stone", "#B5B6A6");
            }
            g.Cylinder(p + Vector3.up * .7f, .5f, 1.1f, "stone", Stone);
            g.Cylinder(p + Vector3.up * 1.3f, 1.4f, .24f, "stone", Stone);
            g.Cylinder(p + Vector3.up * 1.44f, 1.25f, .04f, "water", "#548E90");
            g.Cylinder(p + Vector3.up * 1.9f, .22f, 1, "stone", Stone);
            g.Cone(p + Vector3.up * 2.45f, .42f, .35f, "metal", Metal);
            // Bốn dải va chạm giữ đường vòng thông thoáng ở các góc của bồn tròn.
            g.Collider("Fountain", p + Vector3.up * .3f, new(6.2f, .6f, 1.5f));
            g.Collider("Fountain", p + Vector3.up * .3f, new(5.5f, .6f, 3.2f));
            g.Collider("Fountain", p + Vector3.up * .3f, new(3.2f, .6f, 5.5f));
            g.Collider("Fountain", p + Vector3.up * .3f, new(1.5f, .6f, 6.2f));
            g.Collider("FountainColumn", p + Vector3.up * 1.35f, new(1, 2.7f, 1));
        }
    }
}
