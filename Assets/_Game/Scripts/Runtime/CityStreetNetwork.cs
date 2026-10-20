using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityStreetNetwork
    {
        public static IEnumerable<Bounds> Bounds()
        {
            foreach (WorldRouteCorridor corridor in WorldRouteCatalog.BuildStreetNetwork())
            {
                foreach (WorldRouteSegment segment in WorldRouteGeometry.Segments(corridor))
                    yield return WorldRouteGeometry.ToBounds(segment);
            }
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
            foreach (WorldRouteCorridor corridor in WorldRouteCatalog.BuildStreetNetwork())
            {
                int segmentIndex = 0;
                foreach (WorldRouteSegment segment in WorldRouteGeometry.Segments(corridor))
                {
                    BuildSegment(root, corridor.Id, segmentIndex, segment);
                    segmentIndex++;
                }
            }
            IReadOnlyList<WorldRouteJunction> junctions =
                WorldRouteGeometry.FindJunctions(WorldRouteCatalog.BuildStreetNetwork());
            for (int index = 0; index < junctions.Count; index++)
            {
                WorldRouteJunction junction = junctions[index];
                float patchSize = Mathf.Max(2f, junction.Width + 1.8f);
                TownModelParts.Box(
                    "StreetJunctionPatch_" + index,
                    junction.Point + Vector3.up * .05f,
                    new(patchSize, .03f, patchSize),
                    "stone",
                    "#666C6C",
                    root);
            }
            StaticBatchingUtility.Combine(root.gameObject);
        }

        static void BuildSegment(
            Transform root,
            string routeId,
            int segmentIndex,
            WorldRouteSegment segment)
        {
            float yaw = segment.Rotation.eulerAngles.y;
            float length = segment.Length;
            float width = segment.Width;
            TownModelParts.Box(
                "RoadPavement_" + routeId + "_" + segmentIndex,
                segment.Center + Vector3.up * .044f,
                new(width, .028f, length),
                "stone",
                "#666C6C",
                root,
                yaw);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 offset = WorldRouteGeometry.Offset(
                    segment,
                    side * (width * .5f + .9f));
                TownModelParts.Box(
                    "CitySidewalk_" + routeId + "_" + segmentIndex + "_" + side,
                    segment.Center + offset + Vector3.up * .065f,
                    new(1.8f, .04f, length),
                    "paving",
                    "#BEB7A4",
                    root,
                    yaw);
            }
            for (float at = -length * .5f + 2; at < length * .5f - 1; at += 6)
            {
                Vector3 offset = segment.Forward * at;
                GameObject mark = Art.Box(
                    "CityLaneMark",
                    segment.Center + offset + Vector3.up * .069f,
                    new(.11f, .009f, 2.8f),
                    "#EEE1B3",
                    root);
                mark.transform.localRotation = segment.Rotation;
            }
        }
    }
}
