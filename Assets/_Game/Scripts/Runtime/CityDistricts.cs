using UnityEngine;

namespace Tycoon
{
    public static class CityDistricts
    {
        public static readonly Vector3 ParkCenter = new(-19, 0, 42);
        public const int Revision = 2;
        public const float GroundWidth = 282.842712f;
        public const float GroundDepth = 192.333044f;
        public static Vector3 WorldCenter => new(8, 0, 22);
        public static Bounds WorldBounds => new(WorldCenter, new(GroundWidth, 4, GroundDepth));
        public static Vector3 PlayerSpawn => new(-48.5f, .05f, -23.5f);
        public static Vector3 Gate => new(105, 0, -45);
        public static Vector3 CustomerSpawn(int index) => new(143, 0, -45 + (index % 3 - 1) * .6f);

        // Độ dịch từ layout revision 1; không thay ID hoặc kích thước nhân vật.
        public static Vector3 Offset(string area) => area switch
        {
            "farm" or "livestock" => new(-33, 0, -20),
            "farm_shop" => new(-8, 0, -20),
            "processing" => new(31, 0, -20),
            "supermarket" or "market" => new(30, 0, 24),
            "bakery" => new(6, 0, 29),
            "restaurant" => new(-32, 0, 21),
            _ => Vector3.zero
        };

        public static Vector3 Center(string area) => area switch
        {
            "farm" or "livestock" => new(-58, 0, 0),
            "farm_shop" => new(-1, 0, -14),
            "processing" => new(67, 0, -13),
            "supermarket" or "market" => new(70, 0, 57),
            "bakery" => new(17, 0, 67),
            "restaurant" => new(-52, 0, 60),
            _ => new(5, 0, 24)
        };

        public static Bounds Footprint(string area)
        {
            Vector3 size = area switch
            {
                "farm" or "livestock" => new(72, 3, 58),
                "farm_shop" => new(32, 3, 28),
                "processing" => new(38, 3, 40),
                "supermarket" or "market" => new(38, 3, 36),
                "bakery" => new(38, 3, 36),
                "restaurant" => new(44, 3, 38),
                _ => new(54, 3, 28)
            };
            return new Bounds(Center(area), size);
        }
    }
}
