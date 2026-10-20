using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class WorldRouteRounding
    {
        public const float MaximumChordError = .02f;

        public static IReadOnlyList<WorldRouteCorner> Corners(
            IReadOnlyList<Vector3> anchors,
            float preferredRadius)
        {
            ValidateRadius(preferredRadius);
            var result = new List<WorldRouteCorner>();
            if (!IsFiniteHorizontal(anchors) || anchors.Count < 3)
                return result.AsReadOnly();

            bool closed = IsClosed(anchors);
            int count = closed ? anchors.Count - 1 : anchors.Count;
            int first = closed ? 0 : 1;
            int end = closed ? count : count - 1;
            if (count < 3)
                return result.AsReadOnly();

            for (int index = first; index < end; index++)
            {
                Vector3 previous = anchors[(index + count - 1) % count];
                Vector3 anchor = anchors[index];
                Vector3 next = anchors[(index + 1) % count];
                if (TryCreateCorner(index, previous, anchor, next, preferredRadius,
                    out WorldRouteCorner corner))
                    result.Add(corner);
            }
            return result.AsReadOnly();
        }

        public static IReadOnlyList<Vector3> Sample(
            IReadOnlyList<Vector3> anchors,
            float preferredRadius)
        {
            ValidateRadius(preferredRadius);
            if (anchors == null || anchors.Count < 3 || !AllFinite(anchors))
                return CopyDistinct(anchors);
            if (!IsFiniteHorizontal(anchors))
            {
                return CopyDistinct(WorldRouteGeometry.CreateSmoothPath(
                    anchors, WorldRouteGeometry.RouteSubdivisionsPerSpan));
            }
            return BuildSamples(anchors, preferredRadius, out _);
        }

        public static IReadOnlyList<Vector3> Section(
            IReadOnlyList<Vector3> closedAnchors,
            float preferredRadius,
            int fromCorner,
            int toCorner)
        {
            ValidateRadius(preferredRadius);
            if (closedAnchors == null || closedAnchors.Count < 4
                || !IsClosed(closedAnchors) || !IsFiniteHorizontal(closedAnchors))
            {
                throw new ArgumentException(
                    "A section requires a finite horizontal closed route.", nameof(closedAnchors));
            }

            int count = closedAnchors.Count - 1;
            if (fromCorner < 0 || fromCorner >= count)
                throw new ArgumentOutOfRangeException(nameof(fromCorner));
            if (toCorner < 0 || toCorner >= count)
                throw new ArgumentOutOfRangeException(nameof(toCorner));
            if (toCorner != (fromCorner + 1) % count)
            {
                throw new ArgumentException(
                    "A section must connect adjacent corners in the forward direction.", nameof(toCorner));
            }
            for (int index = 1; index < closedAnchors.Count; index++)
            {
                if (SamePoint(closedAnchors[index - 1], closedAnchors[index]))
                    throw new ArgumentException("A section cannot contain duplicate edges.", nameof(closedAnchors));
            }

            IReadOnlyList<Vector3> samples = BuildSamples(
                closedAnchors, preferredRadius, out int[] midpointIndices);
            int loopCount = samples.Count - 1;
            var result = new List<Vector3>();
            int sampleIndex = midpointIndices[fromCorner];
            int lastIndex = midpointIndices[toCorner];
            result.Add(samples[sampleIndex]);
            do
            {
                sampleIndex = (sampleIndex + 1) % loopCount;
                result.Add(samples[sampleIndex]);
            }
            while (sampleIndex != lastIndex);
            return result.AsReadOnly();
        }

        static IReadOnlyList<Vector3> BuildSamples(
            IReadOnlyList<Vector3> anchors,
            float preferredRadius,
            out int[] midpointIndices)
        {
            bool closed = IsClosed(anchors);
            int count = closed ? anchors.Count - 1 : anchors.Count;
            var corners = new WorldRouteCorner?[count];
            foreach (WorldRouteCorner corner in Corners(anchors, preferredRadius))
                corners[corner.AnchorIndex] = corner;

            midpointIndices = new int[count];
            var samples = new List<Vector3>();
            for (int index = 0; index < count; index++)
            {
                if (!corners[index].HasValue)
                {
                    midpointIndices[index] = AddDistinct(samples, anchors[index]);
                    continue;
                }

                WorldRouteCorner corner = corners[index].Value;
                int segments = ArcSegments(corner);
                for (int segment = 0; segment <= segments; segment++)
                {
                    int sampleIndex = AddDistinct(samples,
                        corner.Evaluate(segment / (float)segments));
                    if (segment == segments / 2)
                        midpointIndices[index] = sampleIndex;
                }
            }
            if (closed && samples.Count > 1)
                AddDistinct(samples, samples[0]);
            return samples.AsReadOnly();
        }

        static bool TryCreateCorner(
            int index,
            Vector3 previous,
            Vector3 anchor,
            Vector3 next,
            float preferredRadius,
            out WorldRouteCorner corner)
        {
            corner = default;
            double incomingX = (double)anchor.x - previous.x;
            double incomingZ = (double)anchor.z - previous.z;
            double outgoingX = (double)next.x - anchor.x;
            double outgoingZ = (double)next.z - anchor.z;
            double incomingLength = Math.Sqrt(incomingX * incomingX + incomingZ * incomingZ);
            double outgoingLength = Math.Sqrt(outgoingX * outgoingX + outgoingZ * outgoingZ);
            if (incomingLength == 0 || outgoingLength == 0)
                return false;

            incomingX /= incomingLength;
            incomingZ /= incomingLength;
            outgoingX /= outgoingLength;
            outgoingZ /= outgoingLength;
            double cross = incomingX * outgoingZ - incomingZ * outgoingX;
            double dot = incomingX * outgoingX + incomingZ * outgoingZ;
            // Đường thẳng và quay đầu không có cung tiếp tuyến hữu hạn trong góc mốc.
            if (Math.Abs(cross) <= 1e-10)
                return false;
            double sweep = Math.Atan2(cross, dot);
            double tangent = Math.Tan(Math.Abs(sweep) * .5);
            double trim = Math.Min(preferredRadius * tangent,
                Math.Min(incomingLength * .5, outgoingLength * .5));
            double radius = trim / tangent;
            double sign = Math.Sign(cross);
            Vector3 entry = new Vector3((float)(anchor.x - incomingX * trim),
                anchor.y, (float)(anchor.z - incomingZ * trim));
            Vector3 exit = new Vector3((float)(anchor.x + outgoingX * trim),
                anchor.y, (float)(anchor.z + outgoingZ * trim));
            Vector3 center = new Vector3(
                (float)(anchor.x - incomingX * trim - sign * incomingZ * radius),
                anchor.y,
                (float)(anchor.z - incomingZ * trim + sign * incomingX * radius));
            if (SamePoint(entry, exit) || !Finite(center.x) || !Finite(center.z)
                || (float)radius <= 0 || !Finite((float)radius))
                return false;
            corner = new WorldRouteCorner(index, anchor, entry, exit, center,
                (float)radius, (float)sweep);
            return true;
        }

        static int ArcSegments(WorldRouteCorner corner)
        {
            // Công thức sin tránh mất độ chính xác của acos khi bán kính lớn.
            double ratio = Math.Min(1, MaximumChordError / (2.0 * corner.Radius));
            double maximumAngle = 4 * Math.Asin(Math.Sqrt(ratio));
            int segments = Math.Max(2, (int)Math.Ceiling(Math.Abs(corner.SweepRadians) / maximumAngle));
            if (segments % 2 != 0)
                segments++;
            return segments;
        }

        static IReadOnlyList<Vector3> CopyDistinct(IReadOnlyList<Vector3> source)
        {
            var copy = new List<Vector3>();
            if (source != null)
            {
                for (int index = 0; index < source.Count; index++)
                    AddDistinct(copy, source[index]);
            }
            return copy.AsReadOnly();
        }

        static int AddDistinct(List<Vector3> points, Vector3 point)
        {
            if (points.Count == 0 || !SamePoint(points[points.Count - 1], point))
                points.Add(point);
            return points.Count - 1;
        }

        static bool IsClosed(IReadOnlyList<Vector3> points)
        {
            return points != null && points.Count > 2
                && SamePoint(points[0], points[points.Count - 1]);
        }

        static bool SamePoint(Vector3 first, Vector3 second)
        {
            return first.x == second.x && first.y == second.y && first.z == second.z;
        }

        static bool IsFiniteHorizontal(IReadOnlyList<Vector3> points)
        {
            if (!AllFinite(points) || points.Count == 0)
                return false;
            for (int index = 1; index < points.Count; index++)
            {
                if (points[index].y != points[0].y)
                    return false;
            }
            return true;
        }

        static bool AllFinite(IReadOnlyList<Vector3> points)
        {
            if (points == null)
                return false;
            for (int index = 0; index < points.Count; index++)
            {
                Vector3 point = points[index];
                if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z))
                    return false;
            }
            return true;
        }

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static void ValidateRadius(float preferredRadius)
        {
            if (preferredRadius <= 0 || !Finite(preferredRadius))
                throw new ArgumentOutOfRangeException(nameof(preferredRadius));
        }
    }
}
