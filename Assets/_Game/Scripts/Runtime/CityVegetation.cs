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
            int branches = tall ? 5 : 7;
            for (int branch = 0; branch < branches; branch++)
            {
                float angle = branch * 2.39996f + p.x * .37f + p.z * .21f;
                float reach = tall ? .42f : .88f + branch % 3 * .14f;
                float y = tall ? 2.3f + branch * .45f : 2.3f + branch % 3 * .47f;
                Vector3 end = new(Mathf.Cos(angle) * reach, y, Mathf.Sin(angle) * reach);
                g.Beam(p + Vector3.up * (y - .7f) * scale, p + end * scale, .14f * scale, "wood", bark);
                Vector3 size = tall ? new(1.65f, 2.5f, 1.55f) : new(2.15f, 1.7f, 1.9f);
                string leaves = branch % 3 == 0 ? "#77904E" : branch % 3 == 1 ? "#527747" : "#65874A";
                g.Crown(p + (end + Vector3.up * .5f) * scale, size * scale, leaves);
            }
            g.Crown(p + Vector3.up * (tall ? 5 : 4.2f) * scale,
                new Vector3(tall ? 1.5f : 2.2f, tall ? 2 : 1.8f, tall ? 1.5f : 2) * scale, "#65874A");
        }

        static void Pine(CityStaticGeometry g, Vector3 p, float scale)
        {
            for (int tier = 0; tier < 4; tier++)
            {
                float radius = 1.6f - tier * .31f;
                Vector3 center = p + new Vector3(tier % 2 * .08f, 2.2f + tier * .8f, 0) * scale;
                g.Cone(center, radius * scale, (2.2f - tier * .18f) * scale,
                    "grass", tier % 2 == 0 ? "#48735A" : "#5A8260");
            }
        }

    }
}
