using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class WorldStreetGeometry
    {
        const double PointTolerance = .000001;
        const double MinimumArea = .0000001;

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

            public Footprint(
                string routeId,
                int segmentIndex,
                WorldRouteSegment source,
                List<Point> outline)
            {
                RouteId = routeId;
                SegmentIndex = segmentIndex;
                Source = source;
                Outline = outline;
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
            var pavements = new List<WorldStreetSurface>();
            var sidewalks = new List<WorldStreetSurface>();
            var acceptedSidewalks = new List<AcceptedPolygon>();
            for (int roadIndex = 0; roadIndex < roads.Count; roadIndex++)
            {
                Footprint road = roads[roadIndex];
                var pieces = new List<List<Point>> { road.Outline };
                for (int previousIndex = 0; previousIndex < roadIndex && pieces.Count > 0; previousIndex++)
                {
                    Footprint previous = roads[previousIndex];
                    if (road.Source.Start.y == previous.Source.Start.y)
                        pieces = Subtract(pieces, previous.Outline);
                }
                AppendSurfaces(pavements, road, 0, pieces);
            }

            for (int roadIndex = 0; roadIndex < roads.Count; roadIndex++)
            {
                Footprint road = roads[roadIndex];
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
                for (int nodeIndex = 1; nodeIndex < route.Nodes.Count; nodeIndex++)
                {
                    var source = new WorldRouteSegment(
                        route.Nodes[nodeIndex - 1],
                        route.Nodes[nodeIndex],
                        route.Width);
                    if (!source.IsFinite || source.Length <= .0001f)
                        throw new ArgumentException(route.Id + " has an invalid street segment.", nameof(routes));
                    if (source.Start.y != source.End.y)
                        throw new ArgumentException(route.Id + " requires flat street surface segments.", nameof(routes));
                    footprints.Add(new Footprint(
                        route.Id,
                        nodeIndex - 1,
                        source,
                        Rectangle(source, source.Width, 0d)));
                }
            }
            return footprints;
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

        static void AppendSurfaces(
            List<WorldStreetSurface> surfaces,
            Footprint road,
            int side,
            List<List<Point>> pieces)
        {
            int pieceIndex = 0;
            for (int index = 0; index < pieces.Count; index++)
            {
                List<Point> piece = pieces[index];
                var rounded = new List<Point>(piece.Count);
                for (int pointIndex = 0; pointIndex < piece.Count; pointIndex++)
                    rounded.Add(new Point((float)piece[pointIndex].X, (float)piece[pointIndex].Z));
                Clean(rounded);
                if (rounded.Count < 3)
                    continue;
                var outline = new List<Vector3>(rounded.Count);
                for (int pointIndex = 0; pointIndex < rounded.Count; pointIndex++)
                    outline.Add(new Vector3((float)rounded[pointIndex].X, road.Source.Start.y, (float)rounded[pointIndex].Z));
                surfaces.Add(new WorldStreetSurface(
                    road.RouteId,
                    road.SegmentIndex,
                    side,
                    pieceIndex,
                    road.Source,
                    outline));
                pieceIndex++;
            }
        }
    }
}
