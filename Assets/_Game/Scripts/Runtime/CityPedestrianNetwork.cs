using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityPedestrianNetwork
    {
        public const float LocalTrip = 22;
        const float MaxConnectorDistance = 120;
        public static readonly Vector3[] Nodes =
        {
            new(-101.1f, 0, -40.1f), new(-31.1f, 0, -40.1f), new(17.6f, 0, -40.1f),
            new(42.1f, 0, -40.1f), new(97.6f, 0, -40.1f), new(141, 0, -40.1f),
            new(-101.1f, 0, 24.1f), new(-78.6f, 0, 24.1f), new(-31.1f, 0, 24.1f),
            new(17.6f, 0, 24.1f), new(42.1f, 0, 24.1f), new(97.6f, 0, 24.1f),
            new(-78.6f, 0, 61.6f), new(-66.7f, 0, 61.6f), new(-31.1f, 0, 10),
            new(17.6f, 0, -3.6f), new(-1, 0, -3.6f), new(97.6f, 0, -14.6f),
            new(79, 0, -14.6f), new(42.1f, 0, 52.6f), new(57, 0, 52.6f),
            new(42.1f, 0, 87.1f), new(97.6f, 0, 87.1f), new(12.4f, 0, 87.1f),
            new(12.4f, 0, 81),
            new(-190.1f, 0, -40.1f), new(-190.1f, 0, 24.1f), new(-190.1f, 0, 150.1f),
            new(185.1f, 0, 150.1f), new(185.1f, 0, 24.1f), new(185.1f, 0, -40.1f),
            new(-190.1f, 0, -107.1f), new(185.1f, 0, -107.1f)
        };

        static readonly int[,] Edges =
        {
            {0, 1}, {1, 2}, {2, 3}, {3, 4}, {4, 5}, {0, 6},
            {6, 7}, {7, 8}, {8, 9}, {9, 10}, {10, 11},
            {7, 12}, {12, 13}, {8, 14}, {9, 15}, {15, 16},
            {13, 21},
            {4, 17}, {17, 11}, {17, 18}, {10, 19}, {19, 20},
            {19, 21}, {21, 22}, {11, 22}, {21, 23}, {23, 24},
            {0, 25}, {6, 26}, {5, 30}, {11, 29}, {25, 26}, {26, 27},
            {27, 28}, {28, 29}, {29, 30}, {25, 31}, {31, 32}, {32, 30}
        };

        public static int Nearest(Vector3 point)
        {
            int best = 0;
            for (int index = 1; index < Nodes.Length; index++)
                if ((Nodes[index] - point).sqrMagnitude < (Nodes[best] - point).sqrMagnitude)
                    best = index;
            return best;
        }

        public static List<Vector3> Plan(Vector3 from, Vector3 destination)
        {
            return TryPlan(from, destination, out var route, out _)
                ? route
                : new List<Vector3>();
        }

        public static bool TryPlan(
            Vector3 from,
            Vector3 destination,
            out List<Vector3> route,
            out string failure)
        {
            route = new List<Vector3>();
            failure = null;
            if (!IsFinite(from) || !IsFinite(destination))
            {
                failure = "from or destination is not finite";
                return false;
            }
            if (!IsInsideWorld(from) || !IsInsideWorld(destination))
            {
                failure = "from or destination is outside the pedestrian world";
                return false;
            }
            if (Vector3.Distance(from, destination) <= LocalTrip)
            {
                route.Add(destination);
                return true;
            }

            bool avoidFarm = IsFarmPoint(from) || IsFarmPoint(destination) ||
                SegmentIntersectsFarm(from, destination);
            RoutingGraph graph = BuildGraph(avoidFarm);
            int start = graph.AddEndpoint(from, IsFarmPoint(from));
            int end = graph.AddEndpoint(destination, IsFarmPoint(destination));
            if (start < 0 || end < 0)
            {
                failure = "from or destination is disconnected from pedestrian corridors";
                return false;
            }
            List<int> path = FindPath(graph, start, end, avoidFarm);
            if (path == null)
            {
                failure = "pedestrian graph is disconnected";
                return false;
            }
            for (int index = 0; index < path.Count; index++)
                route.Add(graph.Points[path[index]]);
            if (route.Count == 0)
            {
                failure = "pedestrian graph returned an empty route";
                return false;
            }
            if (!avoidFarm && route.Count == 2 &&
                Vector3.Distance(from, destination) > LocalTrip)
            {
                // Giữ một waypoint giữa đoạn dài để agent không bỏ qua hành lang đã chọn.
                route.Insert(1, Vector3.Lerp(from, destination, .5f));
            }
            route[0] = from;
            route[^1] = destination;
            return true;
        }

        sealed class RoutingGraph
        {
            public readonly List<Vector3> Points = new(Nodes);
            public readonly List<Vector2Int> Links = new();
            readonly bool avoidFarm;
            readonly int anchorCount;
            public int AnchorCount => anchorCount;

            public RoutingGraph(bool avoidFarm)
            {
                this.avoidFarm = avoidFarm;
                for (int edge = 0; edge < Edges.GetLength(0); edge++)
                    AddLink(Edges[edge, 0], Edges[edge, 1]);
                AddCorridorLinks();
                anchorCount = Points.Count;
            }

            public int AddEndpoint(Vector3 point, bool farmEndpoint)
            {
                var candidates = new List<int>();
                if (farmEndpoint && avoidFarm)
                {
                    for (int index = Nodes.Length; index < anchorCount; index++)
                        if (IsSidewalkPoint(Points[index])) candidates.Add(index);
                }
                else
                {
                    for (int index = 0; index < anchorCount; index++)
                        if (!avoidFarm || !IsFarmPoint(Points[index])) candidates.Add(index);
                }
                int nearest = -1;
                float distance = float.PositiveInfinity;
                for (int index = 0; index < candidates.Count; index++)
                {
                    int candidate = candidates[index];
                    float next = (Points[candidate] - point).sqrMagnitude;
                    if (next >= distance) continue;
                    distance = next;
                    nearest = candidate;
                }
                if (nearest < 0 || distance > MaxConnectorDistance * MaxConnectorDistance)
                    return -1;
                if (distance < .0001f)
                    return nearest;
                Points.Add(point);
                int endpoint = Points.Count - 1;
                Links.Add(new Vector2Int(endpoint, nearest));
                return endpoint;
            }

            void AddCorridorLinks()
            {
                foreach (var corridor in WorldRouteCatalog.BuildDefault())
                {
                    if (corridor.Kind != RouteKind.Sidewalk && corridor.Kind != RouteKind.FarmPath)
                        continue;
                    int first = Points.Count;
                    for (int index = 0; index < corridor.Nodes.Count; index++)
                        Points.Add(corridor.Nodes[index]);
                    for (int index = first + 1; index < Points.Count; index++)
                        AddLink(index - 1, index);
                    ConnectCorridorEnd(first);
                    ConnectCorridorEnd(Points.Count - 1);
                }
            }

            void ConnectCorridorEnd(int corridorIndex)
            {
                int nearest = -1;
                float distance = float.PositiveInfinity;
                for (int index = 0; index < Nodes.Length; index++)
                {
                    if (avoidFarm && IsFarmPoint(Points[index])) continue;
                    float next = (Points[index] - Points[corridorIndex]).sqrMagnitude;
                    if (next >= distance) continue;
                    distance = next;
                    nearest = index;
                }
                if (nearest >= 0 && distance <= MaxConnectorDistance * MaxConnectorDistance)
                    AddLink(corridorIndex, nearest);
            }

            void AddLink(int first, int second)
            {
                if (first == second || avoidFarm && IsFarmEdge(Points[first], Points[second])) return;
                Links.Add(new Vector2Int(first, second));
            }
        }

        static RoutingGraph BuildGraph(bool avoidFarm)
        {
            return new RoutingGraph(avoidFarm);
        }

        static List<int> FindPath(RoutingGraph graph, int start, int end, bool avoidFarm)
        {
            int count = graph.Points.Count;
            var cost = new float[count];
            var previous = new int[count];
            var visited = new bool[count];
            for (int index = 0; index < count; index++)
            {
                cost[index] = float.PositiveInfinity;
                previous[index] = -1;
            }
            cost[start] = 0;
            for (int pass = 0; pass < count; pass++)
            {
                int at = LowestUnvisited(cost, visited);
                if (at < 0 || at == end) break;
                visited[at] = true;
                for (int edge = 0; edge < graph.Links.Count; edge++)
                {
                    Vector2Int link = graph.Links[edge];
                    int next = link.x == at ? link.y : link.y == at ? link.x : -1;
                    bool endpointLink = at >= graph.AnchorCount || next >= graph.AnchorCount;
                    if (next < 0 || avoidFarm && !endpointLink &&
                        IsFarmEdge(graph.Points[at], graph.Points[next])) continue;
                    float candidate = cost[at] + Vector3.Distance(graph.Points[at], graph.Points[next]);
                    if (candidate >= cost[next]) continue;
                    cost[next] = candidate;
                    previous[next] = at;
                }
            }
            if (float.IsPositiveInfinity(cost[end])) return null;
            var path = new List<int>();
            for (int at = end; at >= 0; at = previous[at]) path.Add(at);
            path.Reverse();
            return path;
        }

        static int LowestUnvisited(float[] cost, bool[] visited)
        {
            int best = -1;
            for (int index = 0; index < cost.Length; index++)
                if (!visited[index] && (best < 0 || cost[index] < cost[best])) best = index;
            return best;
        }

        static bool IsSidewalkPoint(Vector3 point)
        {
            if (!WorldRouteCatalog.TryGet("sidewalk-crossings", out WorldRouteCorridor corridor))
                return false;
            for (int index = 0; index < corridor.Nodes.Count; index++)
                if ((corridor.Nodes[index] - point).sqrMagnitude < .0001f) return true;
            return false;
        }

        static bool IsFarmEdge(Vector3 first, Vector3 second)
        {
            return IsFarmPoint(first) || IsFarmPoint(second) || SegmentIntersectsFarm(first, second);
        }

        public static bool SegmentIntersectsFarm(Vector3 first, Vector3 second)
        {
            Bounds farm = CityDistricts.Footprint("farm");
            float minimumX = farm.min.x + .5f;
            float maximumX = farm.max.x - .5f;
            float minimumZ = farm.min.z + .5f;
            float maximumZ = farm.max.z - .5f;
            float lower = 0;
            float upper = 1;
            if (!ClipAxis(first.x, second.x - first.x, minimumX, maximumX, ref lower, ref upper))
                return false;
            if (!ClipAxis(first.z, second.z - first.z, minimumZ, maximumZ, ref lower, ref upper))
                return false;
            return lower <= upper;
        }

        static bool ClipAxis(
            float start,
            float delta,
            float minimum,
            float maximum,
            ref float lower,
            ref float upper)
        {
            if (Mathf.Abs(delta) <= .000001f)
                return start > minimum && start < maximum;
            float first = (minimum - start) / delta;
            float second = (maximum - start) / delta;
            if (first > second)
            {
                float swap = first;
                first = second;
                second = swap;
            }
            lower = Mathf.Max(lower, first);
            upper = Mathf.Min(upper, second);
            return lower <= upper;
        }

        static bool IsFarmPoint(Vector3 point)
        {
            Bounds farm = CityDistricts.Footprint("farm");
            return point.x > farm.min.x + .5f && point.x < farm.max.x - .5f &&
                point.z > farm.min.z + .5f && point.z < farm.max.z - .5f;
        }

        static bool IsInsideWorld(Vector3 point)
        {
            Bounds world = CityDistricts.WorldBounds;
            return world.Contains(new Vector3(point.x, world.center.y, point.z));
        }

        static bool IsFinite(Vector3 point)
        {
            return IsFinite(point.x) && IsFinite(point.y) && IsFinite(point.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
