using System;
using System.Collections.Generic;
using UnityEngine;
namespace Tycoon
{
    public enum WorldRouteOverlapKind
    {
        SharedEndpoint,
        Crossing,
        ParallelClearance
    }

    public struct WorldRouteOverlap
    {
        public string FirstRouteId { get; }
        public string SecondRouteId { get; }
        public RouteKind FirstKind { get; }
        public RouteKind SecondKind { get; }
        public WorldRouteOverlapKind Kind { get; }
        public Vector3 Point { get; }
        public float Distance { get; }
        public float Clearance { get; }

        public WorldRouteOverlap(
            string firstRouteId,
            string secondRouteId,
            RouteKind firstKind,
            RouteKind secondKind,
            WorldRouteOverlapKind kind,
            Vector3 point,
            float distance,
            float clearance)
        {
            FirstRouteId = firstRouteId;
            SecondRouteId = secondRouteId;
            FirstKind = firstKind;
            SecondKind = secondKind;
            Kind = kind;
            Point = point;
            Distance = distance;
            Clearance = clearance;
        }
    }

    public static class WorldRouteDiagnostics
    {
        public static bool TryFindNearest(string routeId, Vector3 point, float maxDistance, out Vector3 nearest, out float distance)
        {
            nearest = default;
            distance = float.PositiveInfinity;
            if (!WorldRouteCatalog.TryGet(routeId, out WorldRouteCorridor corridor) || !IsFinite(point) || float.IsNaN(maxDistance) || maxDistance < 0f)
                return false;
            IReadOnlyList<Vector3> nodes = corridor.SampledNodes;
            if (nodes == null || nodes.Count < 2)
                return false;
            float closest = float.PositiveInfinity;
            for (int index = 1; index < nodes.Count; index++)
            {
                Vector3 start = nodes[index - 1];
                Vector3 end = nodes[index];
                if (!IsFinite(start) || !IsFinite(end))
                    return false;
                float candidate = DistanceSquaredToSegment(point, start, end, out Vector3 projected);
                if (candidate < closest)
                {
                    closest = candidate;
                    nearest = projected;
                }
            }
            if (!IsFinite(closest))
                return false;
            distance = Mathf.Sqrt(closest);
            if (!IsFinite(distance) || distance > maxDistance)
            {
                nearest = default;
                distance = float.PositiveInfinity;
                return false;
            }
            return true;
        }
        public static bool IsReservedByKind(Vector3 point, float radius, RouteKind kind, out string routeId)
        {
            routeId = null;
            if (!IsFinite(point) || float.IsNaN(radius) || float.IsInfinity(radius))
                return false;
            if (WorldRouteCatalog.IsReserved(point, radius, out string firstId) && WorldRouteCatalog.TryGet(firstId, out WorldRouteCorridor first) && first.Kind == kind)
            {
                routeId = first.Id;
                return true;
            }
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            if (routes == null)
                return false;
            float safeRadius = Mathf.Max(0f, radius);
            for (int index = 0; index < routes.Count; index++)
            {
                WorldRouteCorridor corridor = routes[index];
                if (corridor == null || !corridor.ReservesPlacement || corridor.Kind != kind)
                    continue;
                if (IsPointNearRoute(point, corridor.SampledNodes, safeRadius + corridor.Width * .5f))
                {
                    routeId = corridor.Id;
                    return true;
                }
            }
            return false;
        }
        public static bool ValidateCatalog(out string failure)
        {
            failure = null;
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            if (routes == null || routes.Count == 0)
            {
                failure = "route catalog is empty";
                return false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                WorldRouteCorridor corridor = routes[routeIndex];
                if (corridor == null)
                {
                    failure = "route " + routeIndex + " is null";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(corridor.Id) || !ids.Add(corridor.Id))
                {
                    failure = corridor.Id + " has a missing or duplicate id";
                    return false;
                }
                if (!IsFinite(corridor.Width) || corridor.Width <= 0f)
                {
                    failure = corridor.Id + " has an invalid width";
                    return false;
                }
                IReadOnlyList<Vector3> nodes = corridor.Nodes;
                if (nodes == null || nodes.Count < 2)
                {
                    failure = corridor.Id + " has fewer than two nodes";
                    return false;
                }
                for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
                {
                    if (!IsFinite(nodes[nodeIndex]))
                    {
                        failure = corridor.Id + " has a non finite node";
                        return false;
                    }
                    if (nodeIndex > 0 && (nodes[nodeIndex] - nodes[nodeIndex - 1]).sqrMagnitude <= 0f)
                    {
                        failure = corridor.Id + " has a zero length edge";
                        return false;
                    }
                }
            }
            return true;
        }

        public static IReadOnlyList<WorldRouteOverlap> FindCrossRouteOverlaps(
            IReadOnlyList<WorldRouteCorridor> routes,
            float tolerance = .05f)
        {
            var overlaps = new List<WorldRouteOverlap>();
            if (routes == null)
                return overlaps.AsReadOnly();

            float safeTolerance = Mathf.Max(.0001f, tolerance);
            for (int firstRouteIndex = 0;
                firstRouteIndex < routes.Count;
                firstRouteIndex++)
            {
                WorldRouteCorridor firstRoute = routes[firstRouteIndex];
                if (firstRoute == null)
                    continue;
                for (int secondRouteIndex = firstRouteIndex + 1;
                    secondRouteIndex < routes.Count;
                    secondRouteIndex++)
                {
                    WorldRouteCorridor secondRoute = routes[secondRouteIndex];
                    if (secondRoute == null)
                        continue;
                    WorldRouteOverlap pairBest = default;
                    bool pairFound = false;
                    var neighborhoods = CrossingNeighborhoods(firstRoute, secondRoute, safeTolerance);
                    foreach (WorldRouteSegment firstSegment in
                        WorldRouteGeometry.Segments(firstRoute))
                    {
                        foreach (WorldRouteSegment secondSegment in
                            WorldRouteGeometry.Segments(secondRoute))
                        {
                            float distance = Mathf.Sqrt(
                                DistanceSquaredBetweenSegments(
                                    firstSegment.Start,
                                    firstSegment.End,
                                    secondSegment.Start,
                                    secondSegment.End));
                            float clearance = firstRoute.Width * .5f
                                + secondRoute.Width * .5f
                                + safeTolerance;
                            if (!IsFinite(distance) || distance > clearance)
                                continue;

                            WorldRouteOverlapKind kind;
                            Vector3 point;
                            // Chung đầu mút vẫn có thể đè cả một phần chiều dài.
                            if (AreParallel(firstSegment, secondSegment)
                                && HasProjectionOverlap(
                                    firstSegment, secondSegment, safeTolerance))
                            {
                                kind = WorldRouteOverlapKind.ParallelClearance;
                                point = (firstSegment.Center + secondSegment.Center) * .5f;
                            }
                            else if (TrySharedEndpoint(
                                firstSegment,
                                secondSegment,
                                safeTolerance,
                                out point))
                            {
                                kind = WorldRouteOverlapKind.SharedEndpoint;
                            }
                            else if (!AreParallel(firstSegment, secondSegment)
                                && WorldRouteGeometry.TryIntersect(
                                    firstSegment,
                                    secondSegment,
                                    out point))
                            {
                                kind = WorldRouteOverlapKind.Crossing;
                            }
                            else
                            {
                                kind = WorldRouteOverlapKind.ParallelClearance;
                                point = (firstSegment.Center + secondSegment.Center) * .5f;
                                // Các mảnh mẫu sát cùng nút giao vẫn thuộc vùng nối đó.
                                foreach (var neighborhood in neighborhoods)
                                {
                                    float squaredRadius = neighborhood.radius * neighborhood.radius;
                                    if (DistanceSquaredToSegment(neighborhood.point,
                                        firstSegment.Start, firstSegment.End, out _) > squaredRadius
                                        || DistanceSquaredToSegment(neighborhood.point,
                                            secondSegment.Start, secondSegment.End, out _) > squaredRadius)
                                        continue;
                                    kind = neighborhood.kind;
                                    point = neighborhood.point;
                                    break;
                                }
                            }

                            var candidate = new WorldRouteOverlap(
                                firstRoute.Id,
                                secondRoute.Id,
                                firstRoute.Kind,
                                secondRoute.Kind,
                                kind,
                                point,
                                distance,
                                clearance);
                            if (!pairFound
                                || OverlapSeverity(candidate.Kind)
                                    > OverlapSeverity(pairBest.Kind))
                            {
                                pairBest = candidate;
                                pairFound = true;
                            }
                        }
                    }
                    if (pairFound)
                        overlaps.Add(pairBest);
                }
            }
            return overlaps.AsReadOnly();
        }

        static List<(Vector3 point, float radius, WorldRouteOverlapKind kind)> CrossingNeighborhoods(
            WorldRouteCorridor firstRoute, WorldRouteCorridor secondRoute, float tolerance)
        {
            var result = new List<(Vector3 point, float radius, WorldRouteOverlapKind kind)>();
            float clearance = (firstRoute.Width + secondRoute.Width) * .5f + tolerance;
            foreach (WorldRouteSegment first in WorldRouteGeometry.Segments(firstRoute))
            {
                foreach (WorldRouteSegment second in WorldRouteGeometry.Segments(secondRoute))
                {
                    bool parallel = AreParallel(first, second);
                    if (parallel && HasProjectionOverlap(first, second, tolerance))
                        continue;
                    if (TrySharedEndpoint(first, second, tolerance, out Vector3 shared))
                        result.Add((shared, clearance, WorldRouteOverlapKind.SharedEndpoint));
                    else if (!parallel && WorldRouteGeometry.TryIntersect(first, second, out Vector3 crossing))
                    {
                        // Chiều dài vùng giao trên tim đường bằng tổng nửa rộng / sin(góc giao).
                        float sine = Mathf.Abs(first.Forward.x * second.Forward.z
                            - first.Forward.z * second.Forward.x);
                        result.Add((crossing, clearance / Mathf.Max(.0001f, sine),
                            WorldRouteOverlapKind.Crossing));
                    }
                }
            }
            return result;
        }

        public static bool ValidateCrossRouteOverlaps(
            IReadOnlyList<WorldRouteCorridor> routes,
            out string failure,
            float tolerance = .05f)
        {
            failure = null;
            if (routes == null)
            {
                failure = "route list is null";
                return false;
            }
            IReadOnlyList<WorldRouteOverlap> overlaps =
                FindCrossRouteOverlaps(routes, tolerance);
            for (int index = 0; index < overlaps.Count; index++)
            {
                WorldRouteOverlap overlap = overlaps[index];
                if (overlap.Kind != WorldRouteOverlapKind.ParallelClearance)
                    continue;
                failure = overlap.FirstRouteId + " and " + overlap.SecondRouteId
                    + " overlap within " + overlap.Clearance.ToString("0.###")
                    + " units at " + overlap.Point;
                return false;
            }
            return true;
        }

        public static bool ValidateRouteKindBoundaries(
            IReadOnlyList<WorldRouteCorridor> routes,
            out string failure,
            float tolerance = .05f)
        {
            failure = null;
            if (routes == null)
            {
                failure = "route list is null";
                return false;
            }
            for (int index = 0; index < routes.Count; index++)
            {
                WorldRouteCorridor route = routes[index];
                if (route == null)
                    continue;
                if (!IsBoundaryKind(route.Kind))
                    continue;
                if (!WorldRouteGeometry.ValidateNoUnexpectedSelfIntersections(
                    route,
                    out failure))
                    return false;
            }
            IReadOnlyList<WorldRouteOverlap> overlaps =
                FindCrossRouteOverlaps(routes, tolerance);
            for (int index = 0; index < overlaps.Count; index++)
            {
                WorldRouteOverlap overlap = overlaps[index];
                if (overlap.Kind != WorldRouteOverlapKind.ParallelClearance)
                    continue;
                if (!IsBoundaryKind(overlap.FirstKind)
                    && !IsBoundaryKind(overlap.SecondKind))
                    continue;
                failure = "boundary overlap between " + overlap.FirstRouteId
                    + " and " + overlap.SecondRouteId;
                return false;
            }
            return true;
        }
        public static bool ValidateLeg(IReadOnlyList<Vector3> points, float clearance, out string failure)
        {
            failure = null;
            if (points == null)
            {
                failure = "leg points are null";
                return false;
            }
            if (points.Count < 2)
            {
                failure = "leg needs at least two points";
                return false;
            }
            if (!IsFinite(clearance))
            {
                failure = "leg clearance is not finite";
                return false;
            }
            for (int index = 0; index < points.Count; index++)
            {
                if (!IsFinite(points[index]))
                {
                    failure = "leg point " + index + " is not finite";
                    return false;
                }
                if (index > 0 && (points[index] - points[index - 1]).sqrMagnitude <= 0f)
                {
                    failure = "leg edge " + (index - 1) + " is zero length";
                    return false;
                }
            }
            if (clearance <= 0f)
                return true;
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            if (routes == null)
            {
                failure = "route catalog is null";
                return false;
            }
            bool localFarmRoute = IsExplicitLocalFarmRoute(points, clearance, routes);
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                WorldRouteCatalog.IsReserved(points[pointIndex], clearance, out _);
                for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                {
                    WorldRouteCorridor corridor = routes[routeIndex];
                    if (corridor == null || !corridor.ReservesPlacement || IsAllowed(corridor.Kind, localFarmRoute))
                        continue;
                    if (IsPointNearRoute(points[pointIndex], corridor.SampledNodes, corridor.Width * .5f + clearance))
                    {
                        failure = "leg point " + pointIndex + " enters " + corridor.Id;
                        return false;
                    }
                }
            }
            for (int edgeIndex = 1; edgeIndex < points.Count; edgeIndex++)
            {
                for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                {
                    WorldRouteCorridor corridor = routes[routeIndex];
                    if (corridor == null || !corridor.ReservesPlacement || IsAllowed(corridor.Kind, localFarmRoute))
                        continue;
                    if (IsSegmentNearRoute(points[edgeIndex - 1], points[edgeIndex], corridor.SampledNodes, corridor.Width * .5f + clearance))
                    {
                        failure = "leg edge " + (edgeIndex - 1) + " enters " + corridor.Id;
                        return false;
                    }
                }
            }
            return true;
        }
        static bool IsExplicitLocalFarmRoute(IReadOnlyList<Vector3> points, float clearance, IReadOnlyList<WorldRouteCorridor> routes)
        {
            string routeId = null;
            for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                string pointRouteId = null;
                for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                {
                    WorldRouteCorridor corridor = routes[routeIndex];
                    if (corridor == null || !corridor.ReservesPlacement || corridor.Kind != RouteKind.FarmPath)
                        continue;
                    if (IsPointNearRoute(points[pointIndex], corridor.SampledNodes, corridor.Width * .5f + clearance))
                    {
                        pointRouteId = corridor.Id;
                        break;
                    }
                }
                if (pointRouteId == null)
                    return false;
                if (routeId == null)
                    routeId = pointRouteId;
                else if (!string.Equals(routeId, pointRouteId, StringComparison.Ordinal))
                    return false;
            }
            return routeId != null;
        }
        static bool IsAllowed(RouteKind kind, bool localFarmRoute)
        {
            return kind == RouteKind.Sidewalk || kind == RouteKind.FarmPath && localFarmRoute;
        }
        static bool IsPointNearRoute(Vector3 point, IReadOnlyList<Vector3> nodes, float clearance)
        {
            if (nodes == null || nodes.Count < 2 || !IsFinite(clearance) || clearance < 0f)
                return false;
            float limitSquared = clearance * clearance;
            for (int index = 1; index < nodes.Count; index++)
            {
                if (DistanceSquaredToSegment(point, nodes[index - 1], nodes[index], out _) <= limitSquared)
                    return true;
            }
            return false;
        }
        static bool IsSegmentNearRoute(Vector3 start, Vector3 end, IReadOnlyList<Vector3> nodes, float clearance)
        {
            if (nodes == null || nodes.Count < 2 || !IsFinite(clearance) || clearance < 0f)
                return false;
            float limitSquared = clearance * clearance;
            for (int index = 1; index < nodes.Count; index++)
            {
                if (DistanceSquaredBetweenSegments(start, end, nodes[index - 1], nodes[index]) <= limitSquared)
                    return true;
            }
            return false;
        }

        static bool IsBoundaryKind(RouteKind kind)
        {
            return kind == RouteKind.Highway
                || kind == RouteKind.Rail
                || kind == RouteKind.Dock;
        }

        static int OverlapSeverity(WorldRouteOverlapKind kind)
        {
            return kind == WorldRouteOverlapKind.ParallelClearance
                ? 3
                : kind == WorldRouteOverlapKind.Crossing
                    ? 2
                    : 1;
        }

        static bool HasProjectionOverlap(
            WorldRouteSegment first,
            WorldRouteSegment second,
            float tolerance)
        {
            Vector2 direction = new Vector2(
                first.End.x - first.Start.x,
                first.End.z - first.Start.z);
            float length = direction.magnitude;
            if (length <= .0001f)
                return false;
            direction /= length;
            float start = Vector2.Dot(new Vector2(
                second.Start.x - first.Start.x,
                second.Start.z - first.Start.z), direction);
            float end = Vector2.Dot(new Vector2(
                second.End.x - first.Start.x,
                second.End.z - first.Start.z), direction);
            float overlap = Mathf.Min(length, Mathf.Max(start, end))
                - Mathf.Max(0f, Mathf.Min(start, end));
            return overlap > tolerance;
        }

        static bool AreParallel(
            WorldRouteSegment first,
            WorldRouteSegment second)
        {
            float cross = first.Forward.x * second.Forward.z
                - first.Forward.z * second.Forward.x;
            return Mathf.Abs(cross) <= .0001f;
        }

        static bool TrySharedEndpoint(
            WorldRouteSegment first,
            WorldRouteSegment second,
            float tolerance,
            out Vector3 point)
        {
            Vector3[] firstPoints = { first.Start, first.End };
            Vector3[] secondPoints = { second.Start, second.End };
            float squaredTolerance = tolerance * tolerance;
            for (int firstIndex = 0; firstIndex < firstPoints.Length; firstIndex++)
            {
                for (int secondIndex = 0; secondIndex < secondPoints.Length; secondIndex++)
                {
                    if ((firstPoints[firstIndex] - secondPoints[secondIndex]).sqrMagnitude
                        > squaredTolerance)
                        continue;
                    point = (firstPoints[firstIndex] + secondPoints[secondIndex]) * .5f;
                    return true;
                }
            }
            point = default;
            return false;
        }
        static float DistanceSquaredToSegment(Vector3 point, Vector3 start, Vector3 end, out Vector3 nearest)
        {
            float deltaX = end.x - start.x;
            float deltaZ = end.z - start.z;
            float lengthSquared = deltaX * deltaX + deltaZ * deltaZ;
            if (lengthSquared <= 0f)
            {
                nearest = start;
                float offsetX = point.x - start.x;
                float offsetZ = point.z - start.z;
                return offsetX * offsetX + offsetZ * offsetZ;
            }
            float offsetXFromStart = point.x - start.x;
            float offsetZFromStart = point.z - start.z;
            float amount = Mathf.Clamp01((offsetXFromStart * deltaX + offsetZFromStart * deltaZ) / lengthSquared);
            nearest = new Vector3(start.x + deltaX * amount, Mathf.Lerp(start.y, end.y, amount), start.z + deltaZ * amount);
            float differenceX = point.x - nearest.x;
            float differenceZ = point.z - nearest.z;
            return differenceX * differenceX + differenceZ * differenceZ;
        }
        static float DistanceSquaredBetweenSegments(Vector3 firstStart, Vector3 firstEnd, Vector3 secondStart, Vector3 secondEnd)
        {
            if (SegmentsIntersectXZ(firstStart, firstEnd, secondStart, secondEnd))
                return 0f;
            float closest = DistanceSquaredToSegment(firstStart, secondStart, secondEnd, out _);
            closest = Mathf.Min(closest, DistanceSquaredToSegment(firstEnd, secondStart, secondEnd, out _));
            closest = Mathf.Min(closest, DistanceSquaredToSegment(secondStart, firstStart, firstEnd, out _));
            return Mathf.Min(closest, DistanceSquaredToSegment(secondEnd, firstStart, firstEnd, out _));
        }
        // Định hướng XZ bắt cả trường hợp hai đoạn cắt nhau giữa các đầu mút.
        static bool SegmentsIntersectXZ(Vector3 firstStart, Vector3 firstEnd, Vector3 secondStart, Vector3 secondEnd)
        {
            float first = CrossXZ(firstStart, firstEnd, secondStart);
            float second = CrossXZ(firstStart, firstEnd, secondEnd);
            float third = CrossXZ(secondStart, secondEnd, firstStart);
            float fourth = CrossXZ(secondStart, secondEnd, firstEnd);
            if (OppositeSigns(first, second) && OppositeSigns(third, fourth))
                return true;
            return Mathf.Abs(first) <= Mathf.Epsilon && OnSegmentXZ(firstStart, firstEnd, secondStart) ||
                Mathf.Abs(second) <= Mathf.Epsilon && OnSegmentXZ(firstStart, firstEnd, secondEnd) ||
                Mathf.Abs(third) <= Mathf.Epsilon && OnSegmentXZ(secondStart, secondEnd, firstStart) ||
                Mathf.Abs(fourth) <= Mathf.Epsilon && OnSegmentXZ(secondStart, secondEnd, firstEnd);
        }
        static bool OppositeSigns(float first, float second)
        {
            return first > 0f && second < 0f || first < 0f && second > 0f;
        }
        static bool OnSegmentXZ(Vector3 start, Vector3 end, Vector3 point)
        {
            return point.x >= Mathf.Min(start.x, end.x) && point.x <= Mathf.Max(start.x, end.x) && point.z >= Mathf.Min(start.z, end.z) && point.z <= Mathf.Max(start.z, end.z);
        }
        static float CrossXZ(Vector3 start, Vector3 end, Vector3 point)
        {
            return (end.x - start.x) * (point.z - start.z) - (end.z - start.z) * (point.x - start.x);
        }
        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }
        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
