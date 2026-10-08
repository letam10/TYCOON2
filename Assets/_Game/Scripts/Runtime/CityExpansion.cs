using UnityEngine;

namespace Tycoon
{
    public static class CityExpansion
    {
        public static string District(Station station)
        {
            if (station.Id is "checkout_farm" or "shelf_farm_1" or "shelf_livestock") return "farm";
            return station.AreaId;
        }

        public static void Apply(GameSession game, Transform world)
        {
            foreach (var station in game.Stations)
            {
                if (station is PurchasePad or ConveyorStation) continue;
                Vector3 delta = CityDistricts.Offset(District(station));
                station.transform.position += delta;
                station.InteractionPoint += delta;
                if (station is TableStation table) table.Seat += delta;
            }
            foreach (Transform child in world)
            {
                if (child.GetComponent<Station>()) continue;
                string district = child.name switch
                {
                    "StarterSelling" => "farm",
                    "FarmShopDecor" => "farm_shop",
                    "ProcessingDecor" => "processing",
                    "BakeryDecor" => "bakery",
                    "RestaurantDecor" => "restaurant",
                    _ => child.name.StartsWith("Pen_") ? "farm" : FloorDistrict(child.name)
                };
                if (!string.IsNullOrEmpty(district)) child.position += CityDistricts.Offset(district);
            }
        }

        static string FloorDistrict(string name)
        {
            if (name.Contains("NÔNG SẢN")) return "farm_shop";
            if (name.Contains("CHẾ BIẾN")) return "processing";
            if (name.Contains("SIÊU THỊ")) return "supermarket";
            if (name.Contains("TIỆM BÁNH")) return "bakery";
            if (name.Contains("NHÀ HÀNG")) return "restaurant";
            return "";
        }
    }
}
