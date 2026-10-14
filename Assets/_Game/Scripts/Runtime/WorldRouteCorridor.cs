using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public enum RouteKind
    {
        FarmPath,
        Sidewalk,
        Highway,
        Rail,
        Dock
    }

    [Serializable]
    public sealed class WorldRouteCorridor
    {
        [SerializeField]
        readonly string id;

        [SerializeField]
        readonly RouteKind kind;

        [SerializeField]
        readonly Vector3[] nodeValues;

        [SerializeField]
        readonly float width;

        [SerializeField]
        readonly bool reservesPlacement;

        readonly IReadOnlyList<Vector3> readOnlyNodes;

        public string Id => id;

        public RouteKind Kind => kind;

        public IReadOnlyList<Vector3> Nodes => readOnlyNodes;

        public float Width => width;

        public bool ReservesPlacement => reservesPlacement;

        public WorldRouteCorridor(
            string id,
            RouteKind kind,
            IReadOnlyList<Vector3> nodes,
            float width,
            bool reservesPlacement)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Route id is required.", nameof(id));
            if (nodes == null || nodes.Count < 2)
                throw new ArgumentException("A route needs at least two nodes.", nameof(nodes));
            if (width <= 0f || float.IsNaN(width) || float.IsInfinity(width))
                throw new ArgumentOutOfRangeException(nameof(width));

            this.id = id;
            this.kind = kind;
            this.width = width;
            this.reservesPlacement = reservesPlacement;
            nodeValues = new Vector3[nodes.Count];
            for (int index = 0; index < nodes.Count; index++)
                nodeValues[index] = nodes[index];
            readOnlyNodes = Array.AsReadOnly(nodeValues);
        }
    }

    public static class WorldRouteCatalog
    {
        static readonly IReadOnlyList<WorldRouteCorridor> defaultRoutes = BuildRoutes();

        public static IReadOnlyList<WorldRouteCorridor> BuildDefault()
        {
            return defaultRoutes;
        }

        public static bool TryGet(string id, out WorldRouteCorridor corridor)
        {
            corridor = null;
            if (string.IsNullOrEmpty(id))
                return false;

            for (int index = 0; index < defaultRoutes.Count; index++)
            {
                WorldRouteCorridor candidate = defaultRoutes[index];
                if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    corridor = candidate;
                    return true;
                }
            }
            return false;
        }

        public static bool IsReserved(Vector3 point, float radius, out string corridorId)
        {
            corridorId = null;
            float safeRadius = Mathf.Max(0f, radius);
            for (int routeIndex = 0; routeIndex < defaultRoutes.Count; routeIndex++)
            {
                WorldRouteCorridor corridor = defaultRoutes[routeIndex];
                if (!corridor.ReservesPlacement)
                    continue;

                float clearance = safeRadius + corridor.Width * .5f;
                if (DistanceSquaredToRoute(point, corridor.Nodes) <= clearance * clearance)
                {
                    corridorId = corridor.Id;
                    return true;
                }
            }
            return false;
        }

        public static bool ValidateContinuous()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int routeIndex = 0; routeIndex < defaultRoutes.Count; routeIndex++)
            {
                WorldRouteCorridor corridor = defaultRoutes[routeIndex];
                if (string.IsNullOrWhiteSpace(corridor.Id) || !ids.Add(corridor.Id))
                    return false;
                if (corridor.Nodes == null || corridor.Nodes.Count < 2)
                    return false;
                if (corridor.Width <= 0f || !IsFinite(corridor.Width))
                    return false;

                for (int nodeIndex = 0; nodeIndex < corridor.Nodes.Count; nodeIndex++)
                {
                    if (!IsFinite(corridor.Nodes[nodeIndex]))
                        return false;
                    if (nodeIndex == 0)
                        continue;

                    Vector3 previous = corridor.Nodes[nodeIndex - 1];
                    Vector3 current = corridor.Nodes[nodeIndex];
                    if ((current - previous).sqrMagnitude <= .0001f)
                        return false;
                }
            }
            return defaultRoutes.Count > 0;
        }

        static float DistanceSquaredToRoute(Vector3 point, IReadOnlyList<Vector3> nodes)
        {
            float closest = float.PositiveInfinity;
            for (int index = 1; index < nodes.Count; index++)
            {
                float distance = DistanceSquaredToSegment(point, nodes[index - 1], nodes[index]);
                if (distance < closest)
                    closest = distance;
            }
            return closest;
        }

        // Khoảng cách theo mặt bằng XZ để vùng giữ chỗ không phụ thuộc độ cao collider.
        static float DistanceSquaredToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            float deltaX = end.x - start.x;
            float deltaZ = end.z - start.z;
            float lengthSquared = deltaX * deltaX + deltaZ * deltaZ;
            if (lengthSquared <= .000001f)
            {
                float pointX = point.x - start.x;
                float pointZ = point.z - start.z;
                return pointX * pointX + pointZ * pointZ;
            }

            float offsetX = point.x - start.x;
            float offsetZ = point.z - start.z;
            float amount = Mathf.Clamp01((offsetX * deltaX + offsetZ * deltaZ) / lengthSquared);
            float nearestX = start.x + deltaX * amount;
            float nearestZ = start.z + deltaZ * amount;
            float differenceX = point.x - nearestX;
            float differenceZ = point.z - nearestZ;
            return differenceX * differenceX + differenceZ * differenceZ;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static IReadOnlyList<WorldRouteCorridor> BuildRoutes()
        {
            var routes = new List<WorldRouteCorridor>
            {
                new WorldRouteCorridor(
                    "farm-dirt-path",
                    RouteKind.FarmPath,
                    new[]
                    {
                        new Vector3(-142, 0, 92),
                        new Vector3(-118, 0, 88),
                        new Vector3(-92, 0, 80),
                        new Vector3(-70, 0, 64)
                    },
                    4.5f,
                    true),
                new WorldRouteCorridor(
                    "farm-service-loop",
                    RouteKind.FarmPath,
                    new[]
                    {
                        new Vector3(-89, 0, -24),
                        new Vector3(-23, 0, -24),
                        new Vector3(-23, 0, 25),
                        new Vector3(-89, 0, 25),
                        new Vector3(-89, 0, -24)
                    },
                    3.2f,
                    true),
                new WorldRouteCorridor(
                    "sidewalk-crossings",
                    RouteKind.Sidewalk,
                    new[]
                    {
                        new Vector3(-105, 0, 29),
                        new Vector3(-82, 0, 29),
                        new Vector3(-46, 0, 29),
                        new Vector3(-5, 0, 29),
                        new Vector3(21, 0, 29),
                        new Vector3(46, 0, 29),
                        new Vector3(102, 0, 29)
                    },
                    3f,
                    true),
                new WorldRouteCorridor(
                    "highway-ring",
                    RouteKind.Highway,
                    new[]
                    {
                        new Vector3(-195, 0, -112),
                        new Vector3(-195, 0, 155),
                        new Vector3(190, 0, 155),
                        new Vector3(190, 0, -112),
                        new Vector3(-195, 0, -112)
                    },
                    8f,
                    true),
                new WorldRouteCorridor(
                    "rail-mainline",
                    RouteKind.Rail,
                    new[]
                    {
                        new Vector3(-130, 0, -64),
                        new Vector3(10, 0, -64),
                        new Vector3(145, 0, -64)
                    },
                    9f,
                    true),
                new WorldRouteCorridor(
                    "dock-harbor-approach",
                    RouteKind.Dock,
                    new[]
                    {
                        new Vector3(145, 0, -64),
                        new Vector3(180, 0, -64),
                        new Vector3(210, 0, -35),
                        new Vector3(218, 0, -5)
                    },
                    7f,
                    true),
                new WorldRouteCorridor(
                    "forest-access",
                    RouteKind.FarmPath,
                    new[]
                    {
                        new Vector3(46, 0, 91),
                        new Vector3(40, 0, 105),
                        new Vector3(35, 0, 119),
                        new Vector3(30, 0, 133)
                    },
                    4f,
                    true),
                new WorldRouteCorridor(
                    "expansion-forest-access",
                    RouteKind.FarmPath,
                    new[]
                    {
                        new Vector3(-370, 0, 275),
                        new Vector3(-270, 0, 235),
                        new Vector3(-170, 0, 195),
                        new Vector3(-70, 0, 165),
                        new Vector3(30, 0, 133)
                    },
                    4.5f,
                    true),
                new WorldRouteCorridor(
                    "expansion-industrial-approach",
                    RouteKind.Highway,
                    new[]
                    {
                        new Vector3(190, 0, 155),
                        new Vector3(250, 0, 205),
                        new Vector3(330, 0, 250),
                        new Vector3(465, 0, 330)
                    },
                    7f,
                    true),
                new WorldRouteCorridor(
                    "expansion-village-road",
                    RouteKind.Sidewalk,
                    new[]
                    {
                        new Vector3(-55, 0, 320),
                        new Vector3(0, 0, 330),
                        new Vector3(55, 0, 345)
                    },
                    3.5f,
                    true),
                new WorldRouteCorridor(
                    "expansion-marina-dock",
                    RouteKind.Dock,
                    new[]
                    {
                        new Vector3(370, 0, -135),
                        new Vector3(420, 0, -120),
                        new Vector3(470, 0, -105)
                    },
                    5f,
                    true),
                new WorldRouteCorridor(
                    "expansion-south-highway",
                    RouteKind.Highway,
                    new[]
                    {
                        new Vector3(-190, 0, -300),
                        new Vector3(0, 0, -300),
                        new Vector3(190, 0, -300)
                    },
                    8f,
                    true)
            };
            return new System.Collections.ObjectModel.ReadOnlyCollection<WorldRouteCorridor>(routes);
        }
    }
}
