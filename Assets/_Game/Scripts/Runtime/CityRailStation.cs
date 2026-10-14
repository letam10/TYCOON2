using UnityEngine;
using Unity.AI.Navigation;

namespace Tycoon
{
    public static class CityRailStation
    {
        public static void Build(Transform world, bool signage = true)
        {
            var root = TownModelParts.Root("CentralRailwayStation", Vector3.zero, world);
            var g = new CityStaticGeometry(root);
            CityRailNetwork.BuildTracks(g);
            foreach (float z in CityRailNetwork.Tracks)
            {
                var strip = TownModelParts.Root("RailwayNavigationExclusion", new(6, 1.8f, z), root);
                var exclusion = strip.gameObject.AddComponent<NavMeshModifierVolume>();
                exclusion.area = 1;
                exclusion.center = Vector3.zero;
                exclusion.size = new(272, 4.2f, 3.4f);
            }
            foreach (float z in CityRailNetwork.Platforms) Platform(g, z);
            Hall(g, root);
            Bridge(g, root);
            Fence(g, -58.95f);
            Fence(g, -69.05f);
            g.Finish();
            if (signage)
            {
                Art.Label("GA TRUNG TÂM", new(-17, 3.1f, -49.2f), root, .17f);
                Art.Label("SÂN GA 1", new(5, 2.9f, -57.5f), root, .13f);
                Art.Label("SÂN GA 2", new(5, 2.9f, -70.5f), root, .13f);
                Art.Label("LỐI QUA HAI SÂN GA", new(-15.5f, 2.25f, -54), root, .105f);
            }
        }

        static void Platform(CityStaticGeometry g, float z)
        {
            g.Box(new(10, .035f, z), new(66, .17f, 3), "paving", "#B7B5A3");
            g.Collider("RailPlatform", new(10, .035f, z), new(66, .17f, 3));
            float edge = z > -64 ? z - 1.28f : z + 1.28f;
            g.Box(new(10, .124f, edge), new(65, .01f, .19f), "plaster", "#DCC074");
            for (int i = 0; i < 5; i++)
            {
                float x = -18 + i * 11;
                g.Box(new(x, 1.53f, z + .92f), new(.16f, 2.9f, .16f), "metal", "#526B68");
                g.Collider("PlatformColumn", new(x, 1.53f, z + .92f), new(.16f, 2.9f, .16f));
                if (i is 0 or 3) CityProps.Bench(g, new(i == 3 ? x - 5 : x + 2, .12f, z + .6f));
            }
            g.Box(new(9, 3.02f, z), new(57, .16f, 3.65f), "wood", "#698C85");
            g.Box(new(9, 3.15f, z), new(57.3f, .11f, 3.8f), "tile", "#A1765E");
        }

        static void Hall(CityStaticGeometry g, Transform root)
        {
            Vector3 p = new(-17, 0, -51.5f);
            g.Box(p + new Vector3(0, .045f, 0), new(15, .1f, 6), "paving", "#CBC5AE");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(p + new Vector3(side * 7.2f, 1.6f, 0), new(.24f, 3.2f, 5.6f), "plaster", "#E1D8BD");
                g.Collider("StationSideWall", p + new Vector3(side * 7.2f, 1.6f, 0), new(.24f, 3.2f, 5.6f));
                foreach (int front in new[] { -1, 1 })
                {
                    g.Box(p + new Vector3(side * 4.9f, 1.6f, front * 2.8f), new(4.1f, 3.2f, .24f),
                        "plaster", "#E1D8BD");
                    g.Collider("StationFacade", p + new Vector3(side * 4.9f, 1.6f, front * 2.8f),
                        new(4.1f, 3.2f, .24f));
                    g.Box(p + new Vector3(side * 4.9f, 1.85f, front * 2.94f), new(2.7f, 1.3f, .04f),
                        "glass", "#728F96");
                }
                g.RotatedBox(p + new Vector3(0, 3.55f, side * 1.65f), new(15.8f, .18f, 3.65f),
                    Quaternion.Euler(side * 18, 0, 0), "tile", "#A1765E");
            }
            g.Box(p + new Vector3(0, 3.28f, 0), new(15.5f, .18f, 6.4f), "wood", "#8F8066");
            g.Box(p + new Vector3(-5.8f, .9f, -.7f), new(.7f, 1.8f, .8f), "metal", "#557F79");
            g.Box(p + new Vector3(-5.41f, 1.16f, -.7f), new(.04f, .55f, .55f), "glass", "#3F5863");
            g.Collider("TicketMachine", p + new Vector3(-5.8f, .9f, -.7f), new(.7f, 1.8f, .8f));
            g.Box(p + new Vector3(0, 3.1f, 3.02f), new(5.2f, .6f, .12f), "metal", "#446B65");
        }

        static void Bridge(CityStaticGeometry g, Transform root)
        {
            const float north = -55.8f;
            const float south = -72.8f;
            float height = CityRailNetwork.BridgeHeight;
            float x = CityRailNetwork.BridgeX;
            g.Box(new(x, height - .09f, (north + south) * .5f), new(3, .18f, north - south + 2.6f),
                "paving", "#BEBCA9");
            g.Collider("StationFootbridge", new(x, height - .09f, (north + south) * .5f),
                new(3, .18f, north - south + 2.6f));
            // Dốc nối mép sàn, không chui dưới sàn làm Recast cắt mất khoảng thông đầu.
            foreach (float z in new[] { north, south }) Ramp(g, root, new(x - 1.46f, height, z));
            for (int side = -1; side <= 1; side += 2)
            {
                float edgeX = x + side * 1.45f;
                g.Box(new(edgeX, height + .55f, -64), new(.08f, 1.1f, 14), "metal", "#526B68");
                g.Collider("FootbridgeRailing", new(edgeX, height + .55f, -64), new(.08f, 1.1f, 14));
                foreach (float z in new[] { north, south })
                {
                    g.Box(new(x + side * 1.75f, height * .5f, z), new(.25f, height, .25f),
                        "metal", "#526B68");
                    g.Collider("BridgeColumn", new(x + side * 1.75f, height * .5f, z),
                        new(.25f, height, .25f));
                }
            }
        }

        static void Ramp(CityStaticGeometry g, Transform root, Vector3 top)
        {
            Vector3 bottom = top + new Vector3(-CityRailNetwork.RampLength, -top.y, 0);
            Vector3 center = (bottom + top) * .5f - Vector3.up * .065f;
            float length = Vector3.Distance(top, bottom);
            float angle = Mathf.Atan2(top.y, CityRailNetwork.RampLength) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            Vector3 size = new(length, .13f, 2.6f);
            g.RotatedBox(center, size, rotation, "paving", "#BEBCA9");
            CityRailRampCollider.Build(root, bottom, top);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 a = bottom + new Vector3(0, .9f, side * 1.26f);
                Vector3 b = top + new Vector3(0, .9f, side * 1.26f);
                g.Beam(a, b, .075f, "metal", "#526B68");
                for (int i = 0; i <= 6; i++)
                {
                    Vector3 p = Vector3.Lerp(a, b, i / 6f) - Vector3.up * .45f;
                    g.Box(p, new(.06f, .9f, .06f), "metal", "#526B68");
                }
            }
        }

        static void Fence(CityStaticGeometry g, float z)
        {
            // Chừa hai cửa toa tại x=0/20, không để hành khách bước xuyên hàng rào.
            foreach (var span in new[] { new Vector2(-130, -3), new(3, 17), new(23, 142) })
            {
                float center = (span.x + span.y) * .5f;
                float length = span.y - span.x;
                g.Box(new(center, .62f, z), new(length, .07f, .07f), "metal", "#65776C");
                g.Box(new(center, 1.05f, z), new(length, .055f, .055f), "metal", "#65776C");
                g.Collider("RailSafetyFence", new(center, .58f, z), new(length, 1.16f, .08f));
                for (float x = span.x; x < span.y; x += 3)
                    g.Box(new(x, .58f, z), new(.065f, 1.16f, .065f), "metal", "#65776C");
            }
        }
    }
}
