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
            Vector3 local = SignPosition(poi);
            AddSign(root, local, Title(poi.Id), Subtitle(poi.Id), Color(poi.Id));
            Art.Cylinder("GuideMarker", local + Vector3.up * .08f,
                new Vector3(.7f, .08f, .7f), Color(poi.Id), root);
        }

        static Vector3 SignPosition(WorldExpansionPoi poi)
        {
            return poi.Id switch
            {
                "ForestReserve" => new Vector3(-68, 0, -48),
                "IndustrialYard" => new Vector3(-68, 0, -44),
                "NorthVillage" => new Vector3(-68, 0, -34),
                "EastMarina" => new Vector3(-68, 0, 31),
                "SouthHighway" => new Vector3(-180, 0, -15),
                _ => new Vector3(-68, 0, -34)
            };
        }

        static void AddSign(
            Transform parent,
            Vector3 local,
            string title,
            string subtitle,
            string color)
        {
            Art.Cylinder("GuidePost", local + Vector3.up * 1.05f,
                new Vector3(.14f, 1.05f, .14f), "#536C67", parent);
            Art.Box("GuideBoard", local + Vector3.up * 2.15f,
                new Vector3(4.8f, .92f, .14f), color, parent);
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
