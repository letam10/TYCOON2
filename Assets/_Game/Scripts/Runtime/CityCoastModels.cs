using UnityEngine;
using Unity.AI.Navigation;

namespace Tycoon
{
    public static class CityCoastModels
    {
        public static void Beach(CityStaticGeometry g)
        {
            // Dải cát và nước có chung ranh giới, tránh lớp đất xanh nhô xuyên mặt nước.
            for (int row = 0; row < 4; row++)
            {
                float z = -49.5f + row * 33;
                g.Sculpt(CityCoastWaterMesh.BeachSand(34, 33, z + 77), new(-3, 0, z), "sand", "#E8D6AB");
                g.Sculpt(CityCoastWaterMesh.BeachWater(20, 33, z + 77), new(24, 0, z), "ocean", CityOceanHorizon.Tint);
                g.Box(new(-27, .055f, z), new(8, .07f, 33), "paving", "#C5BEA8");
            }
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = new(-11 + (i % 2) * 10, 0, -50 + i * 14);
                CityBeachDetails.Umbrella(g, p);
                CityBeachDetails.Lounger(g, p + new Vector3(1.5f, 0, .8f));
                CityProps.Lamp(g, new(-28.5f, 0, p.z));
            }
            CityLeisureModels.Railing(g, new(-22, 0, -64), new(-22, 0, -10));
            CityLeisureModels.Railing(g, new(-22, 0, 10), new(-22, 0, 64));
        }

        public static void Harbor(CityStaticGeometry g)
        {
            g.Box(new(-18, .045f, 0), new(24, .07f, 62), "paving", "#9EA9A2");
            for (int row = 0; row < 2; row++)
                g.Sculpt(CityCoastWaterMesh.Create(36, 31), new(12, 0, -15.5f + row * 31), "water", CityOceanHorizon.Tint);
            foreach (float z in new[] { -18f, 18f })
            {
                g.Box(new(2, .18f, z), new(30, .22f, 3.2f), "wood", "#A18B68");
                g.Collider("HarborPier", new(2, .08f, z), new(30, .4f, 3.2f));
                g.Ramp(new(-15, .02f, z), new(-12, .28f, z), "wood", "#A18B68");
                for (int x = -10; x <= 16; x += 3)
                {
                    g.Box(new(x, .305f, z), new(.025f, .015f, 3), "wood", "#786B57");
                    g.Cylinder(new(x, .4f, z + 1.45f), .12f, .8f, "wood", "#786B57");
                }
                CityLeisureModels.Railing(g, new(-11, .29f, z - 1.4f), new(16, .29f, z - 1.4f));
            }
            CityBoatModels.Cargo(g, new(6, 0, 6));
            Crane(g, new(-15, 0, -6));
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new(-21, .1f, 12 + i * 5);
                g.Box(p + Vector3.up * 1.2f, new(5, 2.4f, 3.6f), "metal", "#82978B");
                g.Collider("HarborContainer", p + Vector3.up * 1.2f, new(5, 2.4f, 3.6f));
                for (int rib = 0; rib < 10; rib++)
                    g.Box(p + new Vector3(-2.3f + rib * .5f, 1.2f, -1.83f),
                        new(.055f, 2.25f, .06f), "metal", "#526C6C");
            }
            CityProps.Lamp(g, new(-27, 0, -26));
            CityProps.Lamp(g, new(-27, 0, 26));
        }

        static void Crane(CityStaticGeometry g, Vector3 p)
        {
            g.Box(p + Vector3.up * .2f, new(4, .4f, 4), "stone", "#9EA9A2");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Beam(p + new Vector3(side * 1.5f, .4f, -1), p + new Vector3(0, 8, 0),
                    .3f, "metal", "#B6A064");
                g.Beam(p + new Vector3(side * 1.5f, .4f, 1), p + new Vector3(0, 8, 0),
                    .3f, "metal", "#B6A064");
            }
            g.Beam(p + new Vector3(-3, 8, 0), p + new Vector3(10, 8, 0), .45f, "metal", "#B6A064");
            g.Beam(p + new Vector3(0, 10, 0), p + new Vector3(10, 8, 0), .12f, "metal", "#526C6C");
            g.Beam(p + new Vector3(0, 8, 0), p + new Vector3(0, 10, 0), .2f, "metal", "#B6A064");
            g.Beam(p + new Vector3(9.4f, 8, 0), p + new Vector3(9.4f, 3.5f, 0), .045f, "metal", "#526C6C");
            g.Box(p + new Vector3(-2.5f, 7.7f, 0), new(1.8f, 1.2f, 2), "metal", "#687B79");
            g.Collider("HarborCrane", p + Vector3.up * 4, new(3.3f, 8, 3));
        }

        public static void BlockWater(Transform root, string id)
        {
            if (id == "Beach") Block(new(24, 0, 0), new(20, 4, 132));
            else if (id == "Marina")
            {
                Block(new(5.5f, 0, -17.3f), new(23, 4, 7.4f));
                Block(new(5.5f, 0, 0), new(23, 4, 20.8f));
                Block(new(5.5f, 0, 17.3f), new(23, 4, 7.4f));
                Block(new(23.5f, 0, 0), new(13, 4, 42));
            }
            else
            {
                // Chừa nguyên hai bến gỗ để người chơi đi ra bến; nước giữa bến không đi được.
                Block(new(5.5f, 0, -25.3f), new(23, 4, 11.4f));
                Block(new(5.5f, 0, 0), new(23, 4, 32.8f));
                Block(new(5.5f, 0, 25.3f), new(23, 4, 11.4f));
                Block(new(23.5f, 0, 0), new(13, 4, 62));
            }

            void Block(Vector3 center, Vector3 size)
            {
                var water = new GameObject("WaterNavigationBoundary");
                water.transform.SetParent(root, false);
                water.transform.localPosition = center;
                var modifier = water.AddComponent<NavMeshModifierVolume>();
                modifier.size = size;
                modifier.area = 1;
                var boundary = water.AddComponent<BoxCollider>();
                boundary.size = new(size.x, .6f, size.z);
                boundary.center = Vector3.up * .3f;
            }
        }
    }
}
