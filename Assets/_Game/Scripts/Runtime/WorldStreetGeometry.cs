using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class WorldStreetGeometry
    {
        const double PointTolerance = .000001;
        // Mảnh dưới 1 mm² suy biến khi đổi sang mesh/collider số thực Unity.
        const double MinimumArea = .000001;

        readonly struct Point
        {
            public readonly double X;
            public readonly double Z;

            public Point(double x, double z)
            {
                X = x;
                Z = z;
            }
        }

        sealed class Footprint
        {
            public readonly string RouteId;
            public readonly int SegmentIndex;
            public readonly WorldRouteSegment Source;
            public readonly List<Point> Outline;
            public readonly bool IsJoin;

            public Footprint(
                string routeId,
                int segmentIndex,
                WorldRouteSegment source,
                List<Point> outline,
                bool isJoin = false)
            {
                RouteId = routeId;
                SegmentIndex = segmentIndex;
                Source = source;
                Outline = outline;
                IsJoin = isJoin;
            }
        }

        readonly struct AcceptedPolygon
        {
            public readonly float Height;
            public readonly List<Point> Outline;

            public AcceptedPolygon(float height, List<Point> outline)
            {
                Height = height;
                Outline = outline;
            }
        }

        public static WorldStreetLayout Build(
            IReadOnlyList<WorldRouteCorridor> routes,
            float sidewalkWidth = 1.8f)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (sidewalkWidth <= 0f || float.IsNaN(sidewalkWidth) || float.IsInfinity(sidewalkWidth))
                throw new ArgumentOutOfRangeException(nameof(sidewalkWidth));

            var roads = CreateFootprints(routes);
            var pavements = UnionPavements(roads);
            var sidewalks = new List<WorldStreetSurface>();
            var acceptedSidewalks = new List<AcceptedPolygon>();

            for (int roadIndex = 0; roadIndex < roads.Count; roadIndex++)
            {
                Footprint road = roads[roadIndex];
                if (road.IsJoin)
                    continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    double offset = side * ((double)road.Source.Width + sidewalkWidth) * .5;
                    var pieces = new List<List<Point>>
                    {
                        Rectangle(road.Source, sidewalkWidth, offset)
                    };
                    for (int blockerIndex = 0; blockerIndex < roads.Count && pieces.Count > 0; blockerIndex++)
                    {
                        Footprint blocker = roads[blockerIndex];
                        if (road.Source.Start.y == blocker.Source.Start.y)
                            pieces = Subtract(pieces, blocker.Outline);
                    }
                    for (int previousIndex = 0; previousIndex < acceptedSidewalks.Count && pieces.Count > 0; previousIndex++)
                    {
                        AcceptedPolygon previous = acceptedSidewalks[previousIndex];
                        if (road.Source.Start.y == previous.Height)
                            pieces = Subtract(pieces, previous.Outline);
                    }
                    AppendSurfaces(sidewalks, road, side, pieces);
                    for (int pieceIndex = 0; pieceIndex < pieces.Count; pieceIndex++)
                        acceptedSidewalks.Add(new AcceptedPolygon(road.Source.Start.y, pieces[pieceIndex]));
                }
            }
            return new WorldStreetLayout(pavements, sidewalks);
        }

        static List<Footprint> CreateFootprints(IReadOnlyList<WorldRouteCorridor> routes)
        {
            var footprints = new List<Footprint>();
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                WorldRouteCorridor route = routes[routeIndex];
                if (route == null)
                    throw new ArgumentException("Street routes cannot contain null entries.", nameof(routes));
                AddFootprints(footprints, route.Id, route.SampledNodes, route.Width);
            }
            return footprints;
        }

        public static IReadOnlyList<WorldStreetSurface> BuildPavement(
            string id, IReadOnlyList<Vector3> points, float width)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("Pavement requires at least two sampled points.", nameof(points));
            var footprints = new List<Footprint>();
            AddFootprints(footprints, id, points, width);
            return UnionPavements(footprints).AsReadOnly();
        }

        public static IReadOnlyList<WorldStreetSurface> SubtractSurfaces(
            IReadOnlyList<WorldStreetSurface> subjects,
            IReadOnlyList<WorldStreetSurface> blockers)
        {
            if (subjects == null)
                throw new ArgumentNullException(nameof(subjects));
            if (blockers == null)
                throw new ArgumentNullException(nameof(blockers));

            var blockerPolygons = new List<AcceptedPolygon>(blockers.Count);
            for (int index = 0; index < blockers.Count; index++)
            {
                WorldStreetSurface blocker = blockers[index];
                List<Point> polygon = SurfacePolygon(blocker, nameof(blockers));
                blockerPolygons.Add(new AcceptedPolygon(blocker.Source.Start.y, polygon));
            }

            var result = new List<WorldStreetSurface>();
            var nextPieceIndices = new Dictionary<(string, int, int), int>();
            for (int index = 0; index < subjects.Count; index++)
            {
                WorldStreetSurface subject = subjects[index];
                var pieces = new List<List<Point>> { SurfacePolygon(subject, nameof(subjects)) };
                for (int blockerIndex = 0; blockerIndex < blockerPolygons.Count && pieces.Count > 0; blockerIndex++)
                {
                    AcceptedPolygon blocker = blockerPolygons[blockerIndex];
                    if (subject.Source.Start.y == blocker.Height)
                        pieces = Subtract(pieces, blocker.Outline);
                }

                var key = (subject.RouteId, subject.SegmentIndex, subject.Side);
                nextPieceIndices.TryGetValue(key, out int pieceIndex);
                AppendSurfaces(result, subject.RouteId, subject.SegmentIndex, subject.Side,
                    subject.Source, pieces, ref pieceIndex);
                nextPieceIndices[key] = pieceIndex;
            }
            return result.AsReadOnly();
        }

        static List<Point> SurfacePolygon(WorldStreetSurface surface, string parameterName)
        {
            if (surface == null || !surface.Source.IsFinite
                || surface.Source.Start.y != surface.Source.End.y)
                throw new ArgumentException("Surface clipping requires finite flat sources.", parameterName);

            var polygon = new List<Point>(surface.Outline.Count);
            for (int index = 0; index < surface.Outline.Count; index++)
            {
                Vector3 point = surface.Outline[index];
                if (float.IsNaN(point.x) || float.IsInfinity(point.x)
                    || float.IsNaN(point.z) || float.IsInfinity(point.z)
                    || point.y != surface.Source.Start.y)
                    throw new ArgumentException("Surface outlines must be finite and share their source height.", parameterName);
                polygon.Add(new Point(point.x, point.z));
            }
            Clean(polygon);
            if (polygon.Count < 3)
                throw new ArgumentException("Surface clipping requires nondegenerate polygons.", parameterName);
            for (int index = 0; index < polygon.Count; index++)
                if (Cross(polygon[index], polygon[(index + 1) % polygon.Count],
                    polygon[(index + 2) % polygon.Count]) < -MinimumArea)
                    throw new ArgumentException("Surface clipping requires convex polygons: "
                        + surface.RouteId + ":" + surface.SegmentIndex + ":" + surface.PieceIndex
                        + " vertex=" + index + " cross="
                        + Cross(polygon[index], polygon[(index + 1) % polygon.Count],
                            polygon[(index + 2) % polygon.Count]), parameterName);
            return polygon;
        }

        static List<WorldStreetSurface> UnionPavements(List<Footprint> footprints)
        {
            var result = new List<WorldStreetSurface>();
            for (int index = 0; index < footprints.Count; index++)
            {
                Footprint current = footprints[index];
                var pieces = new List<List<Point>> { current.Outline };
                for (int previous = 0; previous < index && pieces.Count > 0; previous++)
                    if (current.Source.Start.y == footprints[previous].Source.Start.y)
                        pieces = Subtract(pieces, footprints[previous].Outline);
                AppendSurfaces(result, current, 0, pieces);
            }
            return result;
        }

        static void AddFootprints(List<Footprint> footprints,
            string id, IReadOnlyList<Vector3> points, float width)
        {
            var segments = new List<WorldRouteSegment>();
            for (int index = 1; index < points.Count; index++)
            {
                var segment = new WorldRouteSegment(points[index - 1], points[index], width);
                if (!segment.IsFinite || segment.Length <= .0001f || width <= 0)
                    throw new ArgumentException(id + " has an invalid street segment.", nameof(points));
                if (segment.Start.y != segment.End.y)
                    throw new ArgumentException(id + " requires flat street surface segments.", nameof(points));
                segments.Add(segment);
                footprints.Add(new Footprint(id, index - 1, segment, Rectangle(segment, width, 0d)));
            }
            for (int index = 1; index < segments.Count; index++)
                AddJoin(footprints, id, segments.Count + index * 2,
                    segments[index - 1], segments[index]);
            if (points[0] == points[^1] && segments.Count > 1)
                AddJoin(footprints, id, segments.Count, segments[^1], segments[0]);
        }

        static void AddJoin(List<Footprint> footprints, string id, int index,
            WorldRouteSegment incoming, WorldRouteSegment outgoing)
        {
            Vector3 center = incoming.End;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 first = center + WorldRouteGeometry.Offset(incoming, incoming.Width * .5f * side);
                Vector3 second = center + WorldRouteGeometry.Offset(outgoing, outgoing.Width * .5f * side);
                // Nối vát nằm trong tam giác hai mép, không kéo dài góc nhọn thành spike.
                var outline = new List<Point>
                {
                    new Point(center.x, center.z),
                    new Point(first.x, first.z),
                    new Point(second.x, second.z)
                };
                Clean(outline);
                if (outline.Count >= 3)
                    footprints.Add(new Footprint(id, index + (side > 0 ? 1 : 0),
                        incoming, outline, true));
            }
        }

        static List<Point> Rectangle(WorldRouteSegment source, double width, double offset)
        {
            double deltaX = (double)source.End.x - source.Start.x;
            double deltaZ = (double)source.End.z - source.Start.z;
            double length = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
            double rightX = deltaZ / length;
            double rightZ = -deltaX / length;
            double firstOffset = offset - width * .5;
            double secondOffset = offset + width * .5;
            return new List<Point>
            {
                new Point(source.Start.x + rightX * firstOffset, source.Start.z + rightZ * firstOffset),
                new Point(source.Start.x + rightX * secondOffset, source.Start.z + rightZ * secondOffset),
                new Point(source.End.x + rightX * secondOffset, source.End.z + rightZ * secondOffset),
                new Point(source.End.x + rightX * firstOffset, source.End.z + rightZ * firstOffset)
            };
        }

        static List<List<Point>> Subtract(List<List<Point>> subjects, List<Point> blocker)
        {
            var result = new List<List<Point>>();
            for (int subjectIndex = 0; subjectIndex < subjects.Count; subjectIndex++)
            {
                List<Point> remainder = subjects[subjectIndex];
                if (!HasAreaIntersection(remainder, blocker))
                {
                    result.Add(remainder);
                    continue;
                }
                // Mỗi cạnh nhả phần ngoài; chỉ phần trong tiếp tục bị cắt để các mảnh không chồng nhau.
                for (int edgeIndex = 0; edgeIndex < blocker.Count && remainder.Count >= 3; edgeIndex++)
                {
                    Point edgeStart = blocker[edgeIndex];
                    Point edgeEnd = blocker[(edgeIndex + 1) % blocker.Count];
                    Split(remainder, edgeStart, edgeEnd, out List<Point> inside, out List<Point> outside);
                    Clean(outside);
                    if (outside.Count >= 3)
                        result.Add(outside);
                    Clean(inside);
                    remainder = inside;
                }
            }
            return result;
        }

        static bool HasAreaIntersection(List<Point> subject, List<Point> blocker)
        {
            if (!BoundsOverlap(subject, blocker))
                return false;
            List<Point> intersection = subject;
            for (int edgeIndex = 0; edgeIndex < blocker.Count; edgeIndex++)
            {
                Split(
                    intersection,
                    blocker[edgeIndex],
                    blocker[(edgeIndex + 1) % blocker.Count],
                    out List<Point> inside,
                    out _);
                Clean(inside);
                if (inside.Count < 3)
                    return false;
                intersection = inside;
            }
            return true;
        }

        static bool BoundsOverlap(List<Point> subject, List<Point> blocker)
        {
            double subjectMinX = double.PositiveInfinity;
            double subjectMaxX = double.NegativeInfinity;
            double subjectMinZ = double.PositiveInfinity;
            double subjectMaxZ = double.NegativeInfinity;
            for (int index = 0; index < subject.Count; index++)
            {
                subjectMinX = Math.Min(subjectMinX, subject[index].X);
                subjectMaxX = Math.Max(subjectMaxX, subject[index].X);
                subjectMinZ = Math.Min(subjectMinZ, subject[index].Z);
                subjectMaxZ = Math.Max(subjectMaxZ, subject[index].Z);
            }
            double blockerMinX = double.PositiveInfinity;
            double blockerMaxX = double.NegativeInfinity;
            double blockerMinZ = double.PositiveInfinity;
            double blockerMaxZ = double.NegativeInfinity;
            for (int index = 0; index < blocker.Count; index++)
            {
                blockerMinX = Math.Min(blockerMinX, blocker[index].X);
                blockerMaxX = Math.Max(blockerMaxX, blocker[index].X);
                blockerMinZ = Math.Min(blockerMinZ, blocker[index].Z);
                blockerMaxZ = Math.Max(blockerMaxZ, blocker[index].Z);
            }
            return subjectMaxX > blockerMinX && blockerMaxX > subjectMinX
                && subjectMaxZ > blockerMinZ && blockerMaxZ > subjectMinZ;
        }

        static void Split(
            List<Point> polygon,
            Point edgeStart,
            Point edgeEnd,
            out List<Point> inside,
            out List<Point> outside)
        {
            inside = new List<Point>(polygon.Count + 1);
            outside = new List<Point>(polygon.Count + 1);
            for (int index = 0; index < polygon.Count; index++)
            {
                Point current = polygon[index];
                Point next = polygon[(index + 1) % polygon.Count];
                double currentDistance = Cross(edgeStart, edgeEnd, current);
                double nextDistance = Cross(edgeStart, edgeEnd, next);
                if (currentDistance >= 0d)
                    inside.Add(current);
                if (currentDistance <= 0d)
                    outside.Add(current);
                if ((currentDistance > 0d && nextDistance < 0d)
                    || (currentDistance < 0d && nextDistance > 0d))
                {
                    double amount = currentDistance / (currentDistance - nextDistance);
                    var intersection = new Point(
                        current.X + (next.X - current.X) * amount,
                        current.Z + (next.Z - current.Z) * amount);
                    inside.Add(intersection);
                    outside.Add(intersection);
                }
            }
        }

        static void Clean(List<Point> polygon)
        {
            double squaredTolerance = PointTolerance * PointTolerance;
            bool changed = true;
            while (changed && polygon.Count >= 3)
            {
                changed = false;
                for (int index = 0; index < polygon.Count; index++)
                {
                    Point previous = polygon[(index + polygon.Count - 1) % polygon.Count];
                    Point current = polygon[index];
                    Point next = polygon[(index + 1) % polygon.Count];
                    double firstX = current.X - previous.X;
                    double firstZ = current.Z - previous.Z;
                    double secondX = next.X - current.X;
                    double secondZ = next.Z - current.Z;
                    double chordX = next.X - previous.X;
                    double chordZ = next.Z - previous.Z;
                    double chordLength = Math.Sqrt(chordX * chordX + chordZ * chordZ);
                    bool duplicate = firstX * firstX + firstZ * firstZ <= squaredTolerance
                        || secondX * secondX + secondZ * secondZ <= squaredTolerance;
                    bool collinear = Math.Abs(Cross(previous, current, next)) <= PointTolerance * chordLength
                        && firstX * secondX + firstZ * secondZ >= 0d;
                    if (!duplicate && !collinear)
                        continue;
                    polygon.RemoveAt(index);
                    changed = true;
                    break;
                }
            }
            double area = SignedArea(polygon);
            if (polygon.Count < 3 || Math.Abs(area) <= MinimumArea)
                polygon.Clear();
            else if (area < 0d)
                polygon.Reverse();
        }

        static double Cross(Point start, Point end, Point point)
        {
            return (end.X - start.X) * (point.Z - start.Z)
                - (end.Z - start.Z) * (point.X - start.X);
        }

        static double SignedArea(List<Point> polygon)
        {
            double twiceArea = 0d;
            for (int index = 1; index + 1 < polygon.Count; index++)
                twiceArea += Cross(polygon[0], polygon[index], polygon[index + 1]);
            return twiceArea * .5;
        }

        static List<Point> QuantizedConvexHull(List<Point> points)
        {
            points.Sort((first, second) =>
            {
                int x = first.X.CompareTo(second.X);
                return x != 0 ? x : first.Z.CompareTo(second.Z);
            });
            var hull = new List<Point>(points.Count * 2);
            for (int index = 0; index < points.Count; index++)
            {
                Point point = points[index];
                while (hull.Count >= 2
                    && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0d)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            int lowerCount = hull.Count;
            for (int index = points.Count - 2; index >= 0; index--)
            {
                Point point = points[index];
                while (hull.Count > lowerCount
                    && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0d)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            if (hull.Count > 1)
                hull.RemoveAt(hull.Count - 1);
            Clean(hull);
            return hull;
        }

        static void AppendSurfaces(
            List<WorldStreetSurface> surfaces,
            Footprint road,
            int side,
            List<List<Point>> pieces)
        {
            int pieceIndex = 0;
            AppendSurfaces(surfaces, road.RouteId, road.SegmentIndex, side,
                road.Source, pieces, ref pieceIndex);
        }

        static void AppendSurfaces(
            List<WorldStreetSurface> surfaces,
            string routeId,
            int segmentIndex,
            int side,
            WorldRouteSegment source,
            List<List<Point>> pieces,
            ref int pieceIndex)
        {
            for (int index = 0; index < pieces.Count; index++)
            {
                List<Point> piece = pieces[index];
                var rounded = new List<Point>(piece.Count);
                for (int pointIndex = 0; pointIndex < piece.Count; pointIndex++)
                    rounded.Add(new Point((float)piece[pointIndex].X, (float)piece[pointIndex].Z));
                // Phép cắt cho mảnh lồi; lượng tử float có thể tạo đỉnh lõm rất nhỏ.
                rounded = QuantizedConvexHull(rounded);
                if (rounded.Count < 3)
                    continue;
                var outline = new List<Vector3>(rounded.Count);
                for (int pointIndex = 0; pointIndex < rounded.Count; pointIndex++)
                    outline.Add(new Vector3((float)rounded[pointIndex].X, source.Start.y, (float)rounded[pointIndex].Z));
                surfaces.Add(new WorldStreetSurface(
                    routeId,
                    segmentIndex,
                    side,
                    pieceIndex,
                    source,
                    outline));
                pieceIndex++;
            }
        }
    }
}
