using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityPedestrianNetwork
    {
        public const float LocalTrip = 22;
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
            new(12.4f, 0, 81)
        };

        static readonly int[,] Edges =
        {
            {0, 1}, {1, 2}, {2, 3}, {3, 4}, {4, 5}, {0, 6},
            {6, 7}, {7, 8}, {8, 9}, {9, 10}, {10, 11},
            {7, 12}, {12, 13}, {8, 14}, {9, 15}, {15, 16},
            {4, 17}, {17, 11}, {17, 18}, {10, 19}, {19, 20},
            {19, 21}, {21, 22}, {11, 22}, {21, 23}, {23, 24}
        };

        public static int Nearest(Vector3 point)
        {
            int best = 0;
            for (int i = 1; i < Nodes.Length; i++)
                if ((Nodes[i] - point).sqrMagnitude < (Nodes[best] - point).sqrMagnitude) best = i;
            return best;
        }

        public static List<Vector3> Plan(Vector3 from, Vector3 destination)
        {
            var result = new List<Vector3>();
            if (Vector3.Distance(from, destination) <= LocalTrip)
            {
                result.Add(destination);
                return result;
            }
            int start = Nearest(from);
            int end = Nearest(destination);
            var cost = new float[Nodes.Length];
            var previous = new int[Nodes.Length];
            var visited = new bool[Nodes.Length];
            for (int i = 0; i < cost.Length; i++)
            {
                cost[i] = float.PositiveInfinity;
                previous[i] = -1;
            }
            cost[start] = 0;
            for (int pass = 0; pass < Nodes.Length; pass++)
            {
                int at = -1;
                for (int i = 0; i < cost.Length; i++)
                    if (!visited[i] && (at < 0 || cost[i] < cost[at])) at = i;
                if (at < 0 || at == end) break;
                visited[at] = true;
                for (int edge = 0; edge < Edges.GetLength(0); edge++)
                {
                    int next = Edges[edge, 0] == at ? Edges[edge, 1] :
                        Edges[edge, 1] == at ? Edges[edge, 0] : -1;
                    if (next < 0) continue;
                    float candidate = cost[at] + Vector3.Distance(Nodes[at], Nodes[next]);
                    if (candidate >= cost[next]) continue;
                    cost[next] = candidate;
                    previous[next] = at;
                }
            }
            for (int at = end; at >= 0; at = previous[at]) result.Add(Nodes[at]);
            result.Reverse();
            result.Add(destination);
            return result;
        }
    }
}
