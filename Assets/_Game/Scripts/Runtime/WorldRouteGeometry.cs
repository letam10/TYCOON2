using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public struct WorldRouteJunction
    {
        public Vector3 Point { get; }
        public float Width { get; private set; }

        public WorldRouteJunction(Vector3 point, float width)
        {
            Point = point;
            Width = width;
        }

        public void IncludeWidth(float width)
        {
            Width = Mathf.Max(Width, width);
        }
    }

    public struct WorldRouteSegment
    {
        public Vector3 Start { get; }
        public Vector3 End { get; }
        public Vector3 Center { get; }
        public Vector3 Forward { get; }
        public Quaternion Rotation { get; }
        public float Length { get; }
        public float Width { get; }

        public WorldRouteSegment(Vector3 start, Vector3 end, float width)
        {
            Start = start;
            End = end;
            Center = (start + end) * .5f;
            Width = width;
            Vector3 delta = end - start;
            Length = delta.magnitude;
            Forward = Length > .0001f ? delta / Length : Vector3.forward;
            Rotation = Quaternion.LookRotation(Forward, Vector3.up);
        }

        public bool IsFinite
        {
            get
            {
                return IsFinitePoint(Start)
                    && IsFinitePoint(End)
                    && IsFinitePoint(Center)
                    && IsFinitePoint(Forward)
                    && IsFiniteValue(Length)
                    && IsFiniteValue(Width);
            }
        }

        static bool IsFinitePoint(Vector3 value)
        {
            return IsFiniteValue(value.x)
                && IsFiniteValue(value.y)
                && IsFiniteValue(value.z);
        }

        static bool IsFiniteValue(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public static class WorldRouteGeometry
    {
        public const int RouteSubdivisionsPerSpan = 4;

        readonly struct IndexedSegment
        {
            public readonly WorldRouteSegment Segment;
            public readonly int RouteIndex;
            public readonly int EdgeIndex;
            public readonly int EdgeCount;
            public readonly bool Closed;

            public IndexedSegment(WorldRouteSegment segment, int routeIndex,
                int edgeIndex, int edgeCount, bool closed)
            {
                Segment = segment;
                RouteIndex = routeIndex;
                EdgeIndex = edgeIndex;
                EdgeCount = edgeCount;
                Closed = closed;
            }
        }

        public static IReadOnlyList<Vector3> CreateSmoothPath(
            IReadOnlyList<Vector3> anchors,
            int subdivisionsPerSpan)
        {
            if (anchors == null || anchors.Count == 0)
                return new List<Vector3>().AsReadOnly();
            if (anchors.Count == 1)
                return new List<Vector3> { anchors[0] }.AsReadOnly();

            int subdivisions = Mathf.Max(1, subdivisionsPerSpan);
            var points = new List<Vector3>((anchors.Count - 1) * subdivisions + 1);
            points.Add(anchors[0]);
            for (int span = 0; span < anchors.Count - 1; span++)
            {
                Vector3 previous = span > 0 ? anchors[span - 1] : anchors[span];
                Vector3 start = anchors[span];
                Vector3 end = anchors[span + 1];
                Vector3 next = span + 2 < anchors.Count ? anchors[span + 2] : end;
                Vector3 startTangent = (end - previous) * .5f;
                Vector3 endTangent = (next - start) * .5f;
                for (int sample = 1; sample <= subdivisions; sample++)
                {
                    float t = sample / (float)subdivisions;
                    float tSquared = t * t;
                    float tCubed = tSquared * t;
                    float startWeight = 2f * tCubed - 3f * tSquared + 1f;
                    float startTangentWeight = tCubed - 2f * tSquared + t;
                    float endWeight = -2f * tCubed + 3f * tSquared;
                    float endTangentWeight = tCubed - tSquared;
                    Vector3 point = startWeight * start
                        + startTangentWeight * startTangent
                        + endWeight * end
                        + endTangentWeight * endTangent;
                    if (sample == subdivisions)
                        point = end;
                    if (!IsFinitePoint(point))
                        point = sample == subdivisions ? end : start;
                    points.Add(ClampToEndpointBounds(point, start, end));
                }
            }
            return points.AsReadOnly();
        }

        public static IReadOnlyList<WorldRouteJunction> FindJunctions(
            IReadOnlyList<WorldRouteCorridor> routes,
            float tolerance = .05f)
        {
            var junctions = new List<WorldRouteJunction>();
            if (routes == null)
                return junctions.AsReadOnly();

            var segments = new List<IndexedSegment>();
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                WorldRouteCorridor route = routes[routeIndex];
                if (route == null)
                    continue;
                IReadOnlyList<Vector3> points = route.SampledNodes;
                bool closed = points.Count > 2 && points[0] == points[^1];
                for (int nodeIndex = 1; nodeIndex < points.Count; nodeIndex++)
                {
                    WorldRouteSegment segment = new WorldRouteSegment(
                        points[nodeIndex - 1],
                        points[nodeIndex],
                        route.Width);
                    if (segment.Length > .0001f)
                        segments.Add(new IndexedSegment(segment, routeIndex,
                            nodeIndex - 1, points.Count - 1, closed));
                }
                IncludeRouteBends(junctions, route, tolerance);
            }

            for (int firstIndex = 0; firstIndex < segments.Count; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < segments.Count; secondIndex++)
                {
                    IndexedSegment firstEntry = segments[firstIndex];
                    IndexedSegment secondEntry = segments[secondIndex];
                    if (firstEntry.RouteIndex == secondEntry.RouteIndex
                        && (secondEntry.EdgeIndex == firstEntry.EdgeIndex + 1
                            || (firstEntry.Closed && firstEntry.EdgeIndex == 0
                                && secondEntry.EdgeIndex == firstEntry.EdgeCount - 1)))
                        continue;
                    WorldRouteSegment first = firstEntry.Segment;
                    WorldRouteSegment second = secondEntry.Segment;
                    // Hai đoạn thẳng hàng nối tiếp không tạo giao lộ hay khoảng cắt vỉa hè.
                    if (Mathf.Abs(Vector3.Dot(first.Forward, second.Forward)) > .99999f)
                        continue;
                    if (!TryIntersect(first, second, out Vector3 point))
                        continue;
                    IncludeJunction(
                        junctions,
                        point,
                        Mathf.Max(first.Width, second.Width),
                        tolerance);
                }
            }
            return junctions.AsReadOnly();
        }

        static void IncludeRouteBends(List<WorldRouteJunction> junctions,
            WorldRouteCorridor route, float tolerance)
        {
            IReadOnlyList<WorldRouteCorner> corners = WorldRouteRounding.Corners(route.Nodes, route.Width);
            var roundedIndices = new HashSet<int>();
            for (int index = 0; index < corners.Count; index++)
            {
                WorldRouteCorner corner = corners[index];
                roundedIndices.Add(corner.AnchorIndex);
                // Một góc logic nằm trên cung thật, không tạo giao lộ cho từng dây cung nhỏ.
                IncludeJunction(junctions, corner.Evaluate(.5f), route.Width, tolerance);
            }
            bool closed = route.Nodes.Count > 2 && route.Nodes[0] == route.Nodes[^1];
            int uniqueCount = closed ? route.Nodes.Count - 1 : route.Nodes.Count;
            int first = closed ? 0 : 1;
            int last = closed ? uniqueCount : uniqueCount - 1;
            for (int index = first; index < last; index++)
            {
                if (roundedIndices.Contains(index))
                    continue;
                Vector3 incoming = route.Nodes[index] - route.Nodes[(index + uniqueCount - 1) % uniqueCount];
                Vector3 outgoing = route.Nodes[(index + 1) % uniqueCount] - route.Nodes[index];
                if (incoming.sqrMagnitude <= .0001f || outgoing.sqrMagnitude <= .0001f)
                    continue;
                if (Vector3.Dot(incoming.normalized, outgoing.normalized) < .999f)
                    IncludeJunction(junctions, route.Nodes[index], route.Width, tolerance);
            }
        }

        public static IEnumerable<WorldRouteSegment> Segments(WorldRouteCorridor corridor)
        {
            if (corridor == null)
                yield break;
            for (int index = 1; index < corridor.SampledNodes.Count; index++)
            {
                WorldRouteSegment segment = new WorldRouteSegment(
                    corridor.SampledNodes[index - 1],
                    corridor.SampledNodes[index],
                    corridor.Width);
                if (segment.Length > .0001f)
                    yield return segment;
            }
        }

        public static IEnumerable<WorldRouteSegment> Segments(WorldPublicApproach approach)
        {
            if (approach == null)
                yield break;
            for (int index = 1; index < approach.SampledNodes.Count; index++)
            {
                WorldRouteSegment segment = new WorldRouteSegment(
                    approach.SampledNodes[index - 1],
                    approach.SampledNodes[index],
                    approach.Width);
                if (segment.Length > .0001f)
                    yield return segment;
            }
        }

        public static IEnumerable<WorldRouteSegment> Segments(
            IReadOnlyList<Vector3> points,
            float width)
        {
            if (points == null)
                yield break;
            for (int index = 1; index < points.Count; index++)
            {
                WorldRouteSegment segment = new WorldRouteSegment(
                    points[index - 1],
                    points[index],
                    width);
                if (segment.Length > .0001f)
                    yield return segment;
            }
        }

        public static WorldRouteSegment TrimAtJunctions(
            WorldRouteSegment segment,
            IReadOnlyList<WorldRouteJunction> junctions,
            float trimDistance)
        {
            float safeTrim = Mathf.Max(0f, trimDistance);
            float maximumTrim = segment.Length * .45f;
            float startTrim = IsJunctionPoint(segment.Start, junctions)
                ? Mathf.Min(safeTrim, maximumTrim)
                : 0f;
            float endTrim = IsJunctionPoint(segment.End, junctions)
                ? Mathf.Min(safeTrim, maximumTrim)
                : 0f;
            return new WorldRouteSegment(
                segment.Start + segment.Forward * startTrim,
                segment.End - segment.Forward * endTrim,
                segment.Width);
        }

        public static bool ValidateNoUnexpectedSelfIntersections(
            WorldRouteCorridor route,
            out string failure)
        {
            failure = null;
            if (route == null)
            {
                failure = "route is null";
                return false;
            }

            var segments = new List<WorldRouteSegment>();
            foreach (WorldRouteSegment segment in Segments(route))
                segments.Add(segment);
            for (int firstIndex = 0; firstIndex < segments.Count; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < segments.Count; secondIndex++)
                {
                    if (secondIndex == firstIndex + 1)
                        continue;
                    if (!TryIntersect(segments[firstIndex], segments[secondIndex], out _))
                        continue;
                    if (TrySharedEndpoint(segments[firstIndex], segments[secondIndex], out _))
                        continue;
                    failure = route.Id + " has an unexpected self-intersection between segments "
                        + firstIndex + " and " + secondIndex;
                    return false;
                }
            }
            return true;
        }

        public static Vector3 Offset(WorldRouteSegment segment, float lateralDistance)
        {
            return segment.Rotation * (Vector3.right * lateralDistance);
        }

        public static Bounds ToBounds(WorldRouteSegment segment, float verticalSize = 2f)
        {
            Vector3 lateral = Offset(segment, segment.Width * .5f);
            Vector3 first = segment.Start + lateral;
            Vector3 second = segment.Start - lateral;
            Vector3 third = segment.End + lateral;
            Vector3 fourth = segment.End - lateral;
            float minX = Mathf.Min(first.x, second.x, third.x, fourth.x);
            float maxX = Mathf.Max(first.x, second.x, third.x, fourth.x);
            float minZ = Mathf.Min(first.z, second.z, third.z, fourth.z);
            float maxZ = Mathf.Max(first.z, second.z, third.z, fourth.z);
            float minY = Mathf.Min(segment.Start.y, segment.End.y) - verticalSize * .5f;
            float maxY = Mathf.Max(segment.Start.y, segment.End.y) + verticalSize * .5f;
            Vector3 minimum = new Vector3(minX, minY, minZ);
            Vector3 maximum = new Vector3(maxX, maxY, maxZ);
            return new Bounds((minimum + maximum) * .5f, maximum - minimum);
        }

        internal static bool TryIntersect(
            WorldRouteSegment first,
            WorldRouteSegment second,
            out Vector3 point)
        {
            point = default;
            Vector2 start = new Vector2(first.Start.x, first.Start.z);
            Vector2 firstEnd = new Vector2(first.End.x, first.End.z);
            Vector2 otherStart = new Vector2(second.Start.x, second.Start.z);
            Vector2 otherEnd = new Vector2(second.End.x, second.End.z);
            Vector2 firstDelta = firstEnd - start;
            Vector2 secondDelta = otherEnd - otherStart;
            float determinant = firstDelta.x * secondDelta.y - firstDelta.y * secondDelta.x;
            if (Mathf.Abs(determinant) <= .000001f)
                return TrySharedEndpoint(first, second, out point);

            Vector2 between = otherStart - start;
            float firstAmount = (between.x * secondDelta.y - between.y * secondDelta.x)
                / determinant;
            float secondAmount = (between.x * firstDelta.y - between.y * firstDelta.x)
                / determinant;
            if (firstAmount < -.0001f || firstAmount > 1.0001f
                || secondAmount < -.0001f || secondAmount > 1.0001f)
                return false;
            point = Vector3.Lerp(first.Start, first.End, Mathf.Clamp01(firstAmount));
            Vector3 otherPoint = Vector3.Lerp(second.Start, second.End, Mathf.Clamp01(secondAmount));
            if (point.y != otherPoint.y)
            {
                point = default;
                return false;
            }
            return true;
        }

        static bool TrySharedEndpoint(
            WorldRouteSegment first,
            WorldRouteSegment second,
            out Vector3 point)
        {
            Vector3[] firstPoints = { first.Start, first.End };
            Vector3[] secondPoints = { second.Start, second.End };
            for (int firstIndex = 0; firstIndex < firstPoints.Length; firstIndex++)
            {
                for (int secondIndex = 0; secondIndex < secondPoints.Length; secondIndex++)
                {
                    if (firstPoints[firstIndex].y != secondPoints[secondIndex].y
                        || (firstPoints[firstIndex] - secondPoints[secondIndex]).sqrMagnitude > .0025f)
                        continue;
                    point = (firstPoints[firstIndex] + secondPoints[secondIndex]) * .5f;
                    return true;
                }
            }
            point = default;
            return false;
        }

        static void IncludeJunction(
            List<WorldRouteJunction> junctions,
            Vector3 point,
            float width,
            float tolerance)
        {
            float squaredTolerance = Mathf.Max(.0001f, tolerance * tolerance);
            for (int index = 0; index < junctions.Count; index++)
            {
                if (junctions[index].Point.y != point.y
                    || (junctions[index].Point - point).sqrMagnitude > squaredTolerance)
                    continue;
                WorldRouteJunction existing = junctions[index];
                existing.IncludeWidth(width);
                junctions[index] = existing;
                return;
            }
            junctions.Add(new WorldRouteJunction(point, width));
        }

        static bool IsJunctionPoint(
            Vector3 point,
            IReadOnlyList<WorldRouteJunction> junctions)
        {
            if (junctions == null)
                return false;
            for (int index = 0; index < junctions.Count; index++)
                if (junctions[index].Point.y == point.y
                    && (junctions[index].Point - point).sqrMagnitude <= .0025f)
                    return true;
            return false;
        }

        static Vector3 ClampToEndpointBounds(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            return new Vector3(
                Mathf.Clamp(point.x, Mathf.Min(start.x, end.x), Mathf.Max(start.x, end.x)),
                Mathf.Clamp(point.y, Mathf.Min(start.y, end.y), Mathf.Max(start.y, end.y)),
                Mathf.Clamp(point.z, Mathf.Min(start.z, end.z), Mathf.Max(start.z, end.z)));
        }

        static bool IsFinitePoint(Vector3 value)
        {
            return IsFiniteValue(value.x)
                && IsFiniteValue(value.y)
                && IsFiniteValue(value.z);
        }

        static bool IsFiniteValue(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
