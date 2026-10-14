using UnityEngine;
using Unity.AI.Navigation;

namespace Tycoon
{
    public static class CityOceanEdge
    {
        public static void Build(Transform world)
        {
            var root = new GameObject("CoastalHorizon").transform;
            root.SetParent(world, false);
            var g = new CityStaticGeometry(root);
            var bounds = CityDistricts.WorldBounds;
            float end = bounds.max.x + 3;
            Boundary(267, end, bounds.min.z - 3, bounds.max.z + 3);
            // Lấp dải đất sau cảng và marina; không chồng mặt nước bãi biển ở x247..267.
            Strip(261, 267, bounds.min.z - 3, 11);
            Strip(261, 267, 143, bounds.max.z + 3);
            g.Finish();
            CityOceanHorizon.Build(root, bounds, 267);

            void Strip(float left, float right, float bottom, float top)
            {
                for (float z = bottom; z < top; z += 32)
                {
                    float depth = Mathf.Min(32, top - z);
                    g.Box(new((left + right) * .5f, .035f, z + depth * .5f),
                        new(right - left, .035f, depth), "water", CityOceanHorizon.Tint);
                }
                Boundary(left, right, bottom, top);
            }

            void Boundary(float left, float right, float bottom, float top)
            {
                var boundary = new GameObject("OuterOceanBoundary");
                boundary.transform.SetParent(root, false);
                boundary.transform.localPosition = new((left + right) * .5f, 0, (bottom + top) * .5f);
                var modifier = boundary.AddComponent<NavMeshModifierVolume>();
                modifier.size = new(right - left, 4, top - bottom);
                modifier.area = 1;
                var collider = boundary.AddComponent<BoxCollider>();
                collider.size = new(right - left, .6f, top - bottom);
                collider.center = Vector3.up * .3f;
            }
        }
    }
}
