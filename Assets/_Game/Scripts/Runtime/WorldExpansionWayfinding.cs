using UnityEngine;

namespace Tycoon
{
    public static class WorldExpansionWayfinding
    {
        public static void Build(Transform poiRoot, WorldExpansionPoi poi)
        {
            if (!poiRoot || poiRoot.Find("Wayfinding"))
                return;

            Transform root = new GameObject("Wayfinding").transform;
            root.SetParent(poiRoot, false);
            Vector3 local = SignPosition(poi, out Vector3 facing);
            AddSign(root, local, facing, Title(poi.Id), Subtitle(poi.Id), Color(poi.Id));
            Art.Cylinder("GuideMarker", local + Vector3.up * .08f,
                new Vector3(.7f, .08f, .7f), Color(poi.Id), root);
        }

        static Vector3 SignPosition(WorldExpansionPoi poi, out Vector3 facing)
        {
            string routeId = RouteId(poi.Id);
            if (WorldRouteCatalog.TryGet(routeId, out WorldRouteCorridor route)
                && poi.RouteAnchors.Count > 0
                && WorldRouteCatalog.TryGetNearestPoint(routeId, poi.RouteAnchors[0], 32f,
                    out Vector3 routePoint, out _))
            {
                Vector3 tangent = RouteDirection(route, routePoint);
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                Vector3 toPoi = poi.Center - routePoint;
                toPoi.y = 0;
                if (toPoi.sqrMagnitude > .001f && Vector3.Dot(side, toPoi) < 0f)
                    side = -side;
                Vector3 world = routePoint + side * (route.Width * .5f + 4f);
                facing = poi.Center - world;
                facing.y = 0;
                if (facing.sqrMagnitude < .001f)
                    facing = -tangent;
                facing.Normalize();
                return world - poi.Center;
            }

            facing = Vector3.forward;
            return poi.Id switch
            {
                "ForestReserve" => new Vector3(-68, 0, -48),
                "IndustrialYard" => new Vector3(-10, 0, -60),
                "NorthVillage" => new Vector3(-68, 0, -34),
                "EastMarina" => new Vector3(-68, 0, 31),
                "SouthHighway" => new Vector3(-180, 0, -15),
                _ => new Vector3(-68, 0, -34)
            };
        }

        static string RouteId(string poiId)
        {
            return poiId switch
            {
                "ForestReserve" => "expansion-forest-access",
                "IndustrialYard" => "expansion-industrial-approach",
                "NorthVillage" => "expansion-village-road",
                "EastMarina" => "expansion-marina-dock",
                "SouthHighway" => "expansion-south-highway",
                _ => string.Empty
            };
        }

        static Vector3 RouteDirection(WorldRouteCorridor route, Vector3 point)
        {
            float closest = float.PositiveInfinity;
            Vector3 direction = Vector3.forward;
            for (int index = 1; index < route.Nodes.Count; index++)
            {
                Vector3 start = route.Nodes[index - 1];
                Vector3 end = route.Nodes[index];
                Vector3 delta = end - start;
                delta.y = 0;
                if (delta.sqrMagnitude < .001f) continue;
                float amount = Mathf.Clamp01(Vector3.Dot(point - start, delta) / delta.sqrMagnitude);
                Vector3 nearest = start + delta * amount;
                float distance = (point - nearest).sqrMagnitude;
                if (distance >= closest) continue;
                closest = distance;
                direction = delta.normalized;
            }
            return direction;
        }

        static void AddSign(
            Transform parent,
            Vector3 local,
            Vector3 facing,
            string title,
            string subtitle,
            string color)
        {
            Art.Cylinder("GuidePost", local + Vector3.up * 1.05f,
                new Vector3(.14f, 1.05f, .14f), "#536C67", parent);
            Art.Cylinder("GuidePostFoot", local + Vector3.up * .08f,
                new Vector3(.5f, .08f, .5f), "#2C4545", parent);
            Art.Cylinder("GuidePostCap", local + Vector3.up * 2.76f,
                new Vector3(.22f, .08f, .22f), "#B7C7B2", parent);
            GameObject board = Art.Box("GuideBoard", local + Vector3.up * 2.15f,
                new Vector3(4.8f, .92f, .14f), color, parent);
            board.transform.localRotation = Quaternion.LookRotation(facing, Vector3.up);
            GameObject back = Art.Box("GuideBoardBackplate", local + Vector3.up * 2.15f,
                new Vector3(4.7f, .82f, .08f), "#183A3A", parent);
            back.transform.localRotation = board.transform.localRotation;
            GameObject trim = Art.Box("GuideBoardTrim", local + Vector3.up * 2.15f,
                new Vector3(4.94f, 1.06f, .07f), "#C7D4C2", parent);
            trim.transform.localRotation = board.transform.localRotation;
            GameObject stripe = Art.Box("GuideBoardReflectiveStripe", local + Vector3.up * 1.79f,
                new Vector3(4.52f, .07f, .08f), "#E1C879", parent);
            stripe.transform.localRotation = board.transform.localRotation;
            for (int support = -1; support <= 1; support += 2)
            {
                GameObject bracket = Art.Box("GuideBoardBracket_" + support,
                    local + Vector3.up * 1.48f + Vector3.right * (support * 1.55f),
                    new Vector3(.14f, .26f, .28f), "#536C67", parent);
                bracket.transform.localRotation = board.transform.localRotation;
            }
            GameObject arrowUpper = Art.Box("GuideBoardArrowUpper",
                board.transform.TransformPoint(new Vector3(1.65f, .12f, -.1f)),
                new Vector3(.52f, .1f, .035f), "#F2D77B", parent);
            arrowUpper.transform.rotation = board.transform.rotation * Quaternion.Euler(0, 0, -32f);
            GameObject arrowLower = Art.Box("GuideBoardArrowLower",
                board.transform.TransformPoint(new Vector3(1.65f, -.12f, -.1f)),
                new Vector3(.52f, .1f, .035f), "#F2D77B", parent);
            arrowLower.transform.rotation = board.transform.rotation * Quaternion.Euler(0, 0, 32f);
            if (!Art.Catalog)
                return;
            Art.Label(title, local + Vector3.up * 2.3f,
                parent, .1f, "#F4E9C9", true);
            Art.Label(subtitle, local + Vector3.up * 2.02f,
                parent, .065f, "#D6E5D4", true);
        }

        static string Title(string id)
        {
            return id switch
            {
                "ForestReserve" => "LỐI VÀO RỪNG",
                "IndustrialYard" => "CỔNG KHO HÀNG",
                "NorthVillage" => "LÀNG PHÍA BẮC",
                "EastMarina" => "BẾN THUYỀN",
                "SouthHighway" => "XA LỘ PHÍA NAM",
                _ => "KHU VỰC"
            };
        }

        static string Subtitle(string id)
        {
            return id switch
            {
                "ForestReserve" => "ĐI BỘ • GIỮ LỐI TRỐNG",
                "IndustrialYard" => "XE TẢI • GIAO NHẬN",
                "NorthVillage" => "NHÀ DÂN • GIẾNG LÀNG",
                "EastMarina" => "LÊN THUYỀN • CẦU CẢNG",
                "SouthHighway" => "ĐI CHẬM • GIỮ LÀN",
                _ => "THEO TUYẾN ĐÁNH DẤU"
            };
        }

        static string Color(string id)
        {
            return id switch
            {
                "ForestReserve" => "#315D47",
                "IndustrialYard" => "#3D5961",
                "NorthVillage" => "#72553F",
                "EastMarina" => "#356D73",
                "SouthHighway" => "#4C5558",
                _ => "#234B45"
            };
        }
    }
}
