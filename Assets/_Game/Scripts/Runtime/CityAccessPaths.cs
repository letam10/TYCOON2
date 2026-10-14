using UnityEngine;

namespace Tycoon
{
    public static class CityAccessPaths
    {
        public static void Build(Transform world)
        {
            var root = new GameObject("LandmarkFootpaths").transform;
            root.SetParent(world, false);
            var g = new CityStaticGeometry(root);
            foreach (var landmark in world.GetComponentsInChildren<CityLandmark>())
            {
                Vector3 a = world.InverseTransformPoint(landmark.Approach);
                if (landmark.Id == "CoffeeHouse")
                {
                    Vector3 b = new(-181, 0, a.z);
                    Pave(a, b, 2.6f, "paving", "#C0B8A0");
                    Pave(b, new(-181, 0, -120), 2.6f, "paving", "#C0B8A0");
                }
                else if (landmark.Id is "PoliceStation" or "FireStation" or "Bank")
                    Pave(a, new(-204, 0, a.z), 2.6f, "paving", "#C0B8A0");
                else if (landmark.Id is "Harbor" or "Marina" or "Beach")
                    Pave(a, new(199, 0, a.z), 2.6f, "paving", "#C0B8A0");
                else if (landmark.Id == "SeasideBar")
                    Pave(a, new(181, 0, a.z), 2.6f, "paving", "#C0B8A0");
                else
                    Pave(a, new(a.x, 0, 164), 2.6f, "paving", "#C0B8A0");
            }
            BuildCorridorRoutes(root, g);
            g.Finish();

            void Pave(Vector3 a, Vector3 b, float width, string surface, string tint)
            {
                a.y = 0;
                b.y = 0;
                Vector3 delta = b - a;
                if (delta.sqrMagnitude < .001f) return;
                float length = delta.magnitude;
                Vector3 p = (a + b) * .5f;
                g.Box(p + Vector3.up * .025f, new(width, .05f, length), surface, tint,
                    Quaternion.LookRotation(delta).eulerAngles.y);
                Vector3 size = Mathf.Abs(delta.x) > Mathf.Abs(delta.z)
                    ? new(length + .05f, .08f, width) : new(width, .08f, length + .05f);
                // Lối lát là vùng đi bộ, không phải tường; trigger giữ vùng định vị mà không chặn capsule.
                g.Collider("PedestrianLink", p + Vector3.up * .01f, size, true);
            }
        }

        static void BuildCorridorRoutes(Transform root, CityStaticGeometry g)
        {
            var signs = new GameObject("RouteSigns").transform;
            signs.SetParent(root, false);
            foreach (var corridor in WorldRouteCatalog.BuildDefault())
            {
                string surface = "paving";
                string tint = "#C0B8A0";
                float width = corridor.Width;
                bool drawCorridor = corridor.Kind != RouteKind.Rail;
                if (corridor.Kind == RouteKind.FarmPath)
                {
                    surface = "soil";
                    tint = "#9A754C";
                }
                else if (corridor.Kind == RouteKind.Highway)
                {
                    surface = "stone";
                    tint = "#596365";
                }
                else if (corridor.Kind == RouteKind.Dock)
                {
                    surface = "wood";
                    tint = "#A18B68";
                }
                if (drawCorridor)
                {
                    for (int node = 1; node < corridor.Nodes.Count; node++)
                        Segment(g, corridor.Nodes[node - 1], corridor.Nodes[node], width, surface, tint);
                }
                Sign(signs, corridor.Nodes[0], SignText(corridor.Id));
            }
        }

        static void Segment(
            CityStaticGeometry g,
            Vector3 start,
            Vector3 end,
            float width,
            string surface,
            string tint)
        {
            Vector3 delta = end - start;
            if (delta.sqrMagnitude < .001f) return;
            Vector3 center = (start + end) * .5f;
            float length = delta.magnitude;
            g.Box(center + Vector3.up * .028f, new(width, .055f, length), surface, tint,
                Quaternion.LookRotation(delta).eulerAngles.y);
            Vector3 triggerSize = Mathf.Abs(delta.x) > Mathf.Abs(delta.z)
                ? new(length + width, .08f, width) : new(width, .08f, length + width);
            g.Collider("RouteCorridor", center + Vector3.up * .01f, triggerSize, true);
        }

        static void Sign(Transform parent, Vector3 position, string text)
        {
            var root = new GameObject("RouteSign_" + text).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Art.Cylinder("SignPost", Vector3.up * 1.15f, new(.08f, 1.15f, .08f),
                "#526C6C", root);
            Art.Box("SignBoard", Vector3.up * 2.25f, new(2.8f, .62f, .08f),
                "#3D7165", root);
            if (Art.Catalog)
                Art.Label(text, Vector3.up * 2.25f + Vector3.back * .06f, root,
                    .16f, "#F2E8C7", true);
        }

        static string SignText(string id)
        {
            return id switch
            {
                "farm-dirt-path" => "ĐƯỜNG ĐẤT NÔNG TRẠI",
                "farm-service-loop" => "VÒNG DỊCH VỤ NÔNG TRẠI",
                "sidewalk-crossings" => "VỈA HÈ VÀ QUA ĐƯỜNG",
                "highway-ring" => "ĐƯỜNG CAO TỐC VÀNH ĐAI",
                "rail-mainline" => "TUYẾN ĐƯỜNG SẮT CHÍNH",
                "dock-harbor-approach" => "LỐI VÀO BẾN CẢNG",
                "forest-access" => "LỐI MÒN RỪNG NGUYÊN SINH",
                "expansion-forest-access" => "ĐƯỜNG VÀO RỪNG MỞ RỘNG",
                "expansion-industrial-approach" => "ĐƯỜNG VÀO KHU CÔNG NGHIỆP",
                "expansion-village-road" => "ĐƯỜNG LÀNG PHÍA BẮC",
                "expansion-marina-dock" => "CẦU CẢNG BẾN THUYỀN",
                "expansion-south-highway" => "ĐƯỜNG CAO TỐC PHÍA NAM",
                _ => id
            };
        }
    }
}
