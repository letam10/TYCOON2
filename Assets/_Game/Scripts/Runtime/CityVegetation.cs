using UnityEngine;

namespace Tycoon
{
    public static class CityVegetation
    {
        public static void Tree(CityStaticGeometry g, Vector3 p, float scale, int species)
        {
            species = ((species % 3) + 3) % 3;
            float height = species == 1 ? 4.6f : 3.4f;
            float radius = species == 1 ? .15f : .2f;
            string bark = species == 1 ? "#A1987B" : "#766046";
            g.Cylinder(p + Vector3.up * height * .5f * scale, radius * scale, height * scale, "wood", bark);
            for (int root = 0; root < 4; root++)
            {
                float angle = root * Mathf.PI * .5f + p.x;
                Vector3 end = new(Mathf.Cos(angle) * .48f, .06f, Mathf.Sin(angle) * .48f);
                g.Beam(p + Vector3.up * .36f * scale, p + end * scale, .14f * scale, "wood", bark);
            }
            if (species == 2) Pine(g, p, scale);
            else Deciduous(g, p, scale, species == 1, bark);
            g.Collider("TreeTrunk", p + Vector3.up * height * .5f * scale,
                new Vector3(radius * 2, height, radius * 2) * scale);
            g.Collider("TreeRoots", p + Vector3.up * .13f * scale, new Vector3(.85f, .26f, .85f) * scale);
        }

        static void Deciduous(CityStaticGeometry g, Vector3 p, float scale, bool tall, string bark)
        {
            int branches = tall ? 6 : 8;
            for (int branch = 0; branch < branches; branch++)
            {
                float angle = branch * 2.39996f + p.x * .37f + p.z * .21f;
                float reach = tall ? .5f + branch % 2 * .12f : .72f + branch % 3 * .1f;
                float y = tall ? 2.35f + branch * .41f : 2.1f + branch % 3 * .42f;
                Vector3 end = new(Mathf.Cos(angle) * reach, y, Mathf.Sin(angle) * reach);
                g.Beam(p + Vector3.up * (y - .7f) * scale, p + end * scale, .14f * scale, "wood", bark);
                Vector3 crown = p + (end + Vector3.up * .52f) * scale;
                Vector3 size = tall ? new(1.42f, 2.25f, 1.36f) : new(1.86f, 1.48f, 1.7f);
                float variation = Mathf.Abs(Mathf.Sin((p.x + p.z) * .17f + branch * 1.31f));
                string leaves = variation > .72f ? "#789B59" : variation > .36f ? "#5F884C" : "#4C7547";
                g.Crown(crown, size * scale, leaves);
                if (branch % 2 == 0)
                    g.Crown(crown + new Vector3(Mathf.Cos(angle + 1.2f) * .24f, .45f,
                        Mathf.Sin(angle + 1.2f) * .24f) * scale,
                        size * (tall ? .46f : .52f) * scale, tall ? "#86A563" : "#729650");
            }
            g.Crown(p + Vector3.up * (tall ? 4.95f : 4.17f) * scale,
                new Vector3(tall ? 1.38f : 1.92f, tall ? 1.78f : 1.58f, tall ? 1.34f : 1.76f) * scale,
                "#65874A");
            g.Crown(p + Vector3.up * (tall ? 4.52f : 3.62f) * scale,
                new Vector3(tall ? .92f : 1.28f, tall ? 1.06f : 1.14f, tall ? .9f : 1.2f) * scale,
                tall ? "#789B59" : "#527747");
        }

        static void Pine(CityStaticGeometry g, Vector3 p, float scale)
        {
            for (int tier = 0; tier < 5; tier++)
            {
                float radius = 1.5f - tier * .27f;
                float offset = tier % 2 == 0 ? -.07f : .08f;
                Vector3 center = p + new Vector3(offset, 1.85f + tier * .78f, 0) * scale;
                g.Cone(center, radius * scale, (2.12f - tier * .16f) * scale,
                    "grass", tier % 2 == 0 ? "#48735A" : "#5A8260");
            }
            g.Cone(p + Vector3.up * 5.45f * scale, .34f * scale, .8f * scale,
                "grass", "#6A9366");
        }

    }
}
