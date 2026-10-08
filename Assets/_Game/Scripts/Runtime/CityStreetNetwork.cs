using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityStreetNetwork
    {
        static Bounds Road(Vector3 a, Vector3 b, float width)
        {
            Vector3 size = new(Mathf.Abs(b.x - a.x), 2, Mathf.Abs(b.z - a.z));
            if (size.x < .1f) size.x = width;
            else size.z = width;
            return new Bounds((a + b) * .5f, size);
        }

        public static IEnumerable<Bounds> Bounds()
        {
            yield return Road(new(-116, 0, -45), new(145, 0, -45), 8);
            yield return Road(new(-105, 0, 29), new(112, 0, 29), 8);
            yield return Road(new(-105, 0, -45), new(-105, 0, 29), 6);
            yield return Road(new(102, 0, -45), new(102, 0, 91), 7);
            yield return Road(new(46, 0, 29), new(46, 0, 91), 6);
            yield return Road(new(9, 0, 91), new(102, 0, 91), 6);
            yield return Road(new(9, 0, 81), new(9, 0, 91), 5);
            yield return Road(new(-82, 0, 29), new(-82, 0, 65), 5);
            yield return Road(new(-82, 0, 65), new(-66.7f, 0, 65), 5);
            yield return Road(new(-35, 0, 10), new(-35, 0, 29), 5);
            yield return Road(new(-1, 0, -7), new(21, 0, -7), 5);
            yield return Road(new(21, 0, -7), new(21, 0, 29), 5);
            yield return Road(new(79, 0, -18), new(102, 0, -18), 5);
            yield return Road(new(46, 0, 56), new(57, 0, 56), 5);
        }

        public static List<Vector3> Access(string id)
        {
            Vector3 dock = TruckRoutes.Dock(id);
            return id switch
            {
                "storage_farm" => new() { dock, new(-35, 0, 29) },
                "storage_farm_shop" => new() { dock, new(21, 0, -7), new(21, 0, 29) },
                "storage_processing" => new() { dock, new(102, 0, -18), new(102, 0, 29) },
                "storage_supermarket" => new() { dock, new(46, 0, 56), new(46, 0, 29) },
                "storage_bakery" => new() { dock, new(9, 0, 91), new(46, 0, 91), new(46, 0, 29) },
                _ => new() { dock, new(-82, 0, 65), new(-82, 0, 29) }
            };
        }

        public static void Build(Transform world)
        {
            var root = new GameObject("CityStreets").transform;
            root.SetParent(world, false);
            foreach (var road in Bounds())
            {
                Vector3 center = road.center;
                bool horizontal = road.size.x > road.size.z;
                float length = horizontal ? road.size.x : road.size.z;
                float width = horizontal ? road.size.z : road.size.x;
                TownModelParts.Box("RoadPavement", center + Vector3.up * .044f,
                    new(road.size.x, .028f, road.size.z), "stone", "#666C6C", root);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 offset = horizontal ? new(0, 0, side * (width * .5f + .9f))
                        : new(side * (width * .5f + .9f), 0, 0);
                    Vector3 size = horizontal ? new(length, .04f, 1.8f) : new(1.8f, .04f, length);
                    TownModelParts.Box("CitySidewalk", center + offset + Vector3.up * .065f,
                        size, "paving", "#BEB7A4", root);
                }
                for (float at = -length * .5f + 2; at < length * .5f - 1; at += 6)
                {
                    Vector3 offset = horizontal ? new(at, .069f, 0) : new(0, .069f, at);
                    Vector3 size = horizontal ? new(2.8f, .009f, .11f) : new(.11f, .009f, 2.8f);
                    Art.Box("CityLaneMark", center + offset, size, "#EEE1B3", root);
                }
            }
            StaticBatchingUtility.Combine(root.gameObject);
        }
    }
}
