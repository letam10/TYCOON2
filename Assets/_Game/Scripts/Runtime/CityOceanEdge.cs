using UnityEngine;

namespace Tycoon
{
    public static class CityOceanEdge
    {
        public static void Build(Transform world)
        {
            var root = new GameObject("CoastalHorizon").transform;
            root.SetParent(world, false);
            Bounds bounds = CityDistricts.WorldBounds;
            CityOceanHorizon.Build(root, bounds, WorldHydrologyLayout.CoastX(bounds.center.z));
        }
    }
}
