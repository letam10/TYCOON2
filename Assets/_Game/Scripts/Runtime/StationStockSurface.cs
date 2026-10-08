using UnityEngine;

namespace Tycoon
{
    public static class StationStockSurface
    {
        public static void Build(Station station, Vector3 surface, Vector2 size, bool legs)
        {
            var root = TownModelParts.Root("StockSupport", Vector3.zero, station.transform);
            TownModelParts.Box("StockTray", surface - Vector3.up * .04f,
                new Vector3(size.x, .08f, size.y), "wood", "#B7996F", root);
            var solid = root.gameObject.AddComponent<BoxCollider>();
            solid.center = surface - Vector3.up * .06f;
            solid.size = new Vector3(size.x, .12f, size.y);
            if (!legs) return;
            float height = surface.y - .08f;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var at = surface + new Vector3(x * (size.x * .5f - .1f),
                        -surface.y + height * .5f, z * (size.y * .5f - .1f));
                    TownModelParts.Box("StockTrayLeg", at, new Vector3(.1f, height, .1f),
                        "metal", "#61716B", root);
                }
            }
        }
    }
}
