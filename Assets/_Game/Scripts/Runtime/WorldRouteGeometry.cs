using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
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
        public static IEnumerable<WorldRouteSegment> Segments(WorldRouteCorridor corridor)
        {
            if (corridor == null)
                yield break;
            for (int index = 1; index < corridor.Nodes.Count; index++)
            {
                WorldRouteSegment segment = new WorldRouteSegment(
                    corridor.Nodes[index - 1],
                    corridor.Nodes[index],
                    corridor.Width);
                if (segment.Length > .0001f)
                    yield return segment;
            }
        }

        public static IEnumerable<WorldRouteSegment> Segments(WorldPublicApproach approach)
        {
            if (approach == null)
                yield break;
            for (int index = 1; index < approach.Nodes.Count; index++)
            {
                WorldRouteSegment segment = new WorldRouteSegment(
                    approach.Nodes[index - 1],
                    approach.Nodes[index],
                    approach.Width);
                if (segment.Length > .0001f)
                    yield return segment;
            }
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
    }
}
