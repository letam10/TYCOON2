using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public readonly struct RiverSection
    {
        public readonly Vector3 Center, Left, Right;
        public readonly float HalfWidth;

        public RiverSection(Vector3 center, Vector3 left, Vector3 right, float halfWidth)
        {
            Center = center;
            Left = left;
            Right = right;
            HalfWidth = halfWidth;
        }
    }

    public static class WorldHydrologyLayout
    {
        public const float WaterLevel = CityWaterSurface.WaterBaseline;
        public const float BankWidth = 3f;
        static readonly Vector2[] shore =
        {
            new(-400, 260), new(-290, 250), new(-275, 235), new(-270, 218),
            new(-160, 218), new(-155, 225), new(-90, 225), new(-66, 237),
            new(-62, 225), new(-18, 225), new(8, 247), new(160, 247),
            new(230, 260), new(300, 248), new(430, 260)
        };
        public static Vector3 Mouth => new(CoastX(151), WaterLevel, 151);
        public static readonly IReadOnlyList<RiverSection> River = CreateRiver();

        public static float CoastX(float z)
        {
            float x = shore[0].y;
            for (int index = 1; index < shore.Length; index++)
            {
                if (z > shore[index].x)
                {
                    x = shore[index].y;
                    continue;
                }
                float amount = Mathf.InverseLerp(shore[index - 1].x, shore[index].x, z);
                x = Mathf.Lerp(shore[index - 1].y, shore[index].y, Mathf.SmoothStep(0, 1, amount));
                break;
            }
            bool quay = z is >= -270 and <= -160 or >= -155 and <= -90 or >= -62 and <= -18;
            return quay ? x : x + .6f * Mathf.Sin(z * .06f) + .25f * Mathf.Sin(z * .13f);
        }

        public static bool IsOcean(Vector3 point, float inset = 0)
        {
            return point.x >= CoastX(point.z) + inset;
        }

        public static bool IsRiver(Vector3 point, float inset = 0)
        {
            return RiverDepth(point) >= inset;
        }

        public static bool IsWater(Vector3 point, float inset = 0)
        {
            return IsOcean(point, inset) || IsRiver(point, inset);
        }

        public static float SignedWaterDistance(Vector3 point)
        {
            return Mathf.Max(point.x - CoastX(point.z), RiverDepth(point));
        }

        public static float ShoreDistance(Vector3 point)
        {
            if (IsOcean(point))
            {
                float mouthDepth = Mathf.Max(0, 11f - Mathf.Abs(point.z - Mouth.z));
                return point.x - CoastX(point.z) + mouthDepth;
            }
            return Mathf.Max(0, RiverDepth(point));
        }

        public static bool CrossesWater(Bounds footprint)
        {
            for (int z = 0; z <= 2; z++)
                for (int x = 0; x <= 2; x++)
                    if (IsWater(new Vector3(Mathf.Lerp(footprint.min.x, footprint.max.x, x * .5f),
                        0, Mathf.Lerp(footprint.min.z, footprint.max.z, z * .5f))))
                        return true;
            return false;
        }

        static float RiverDepth(Vector3 point)
        {
            float depth = float.NegativeInfinity;
            Vector2 position = new(point.x, point.z);
            for (int index = 1; index < River.Count; index++)
            {
                RiverSection first = River[index - 1];
                RiverSection second = River[index];
                Vector2 a = new(first.Center.x, first.Center.z);
                Vector2 b = new(second.Center.x, second.Center.z);
                Vector2 delta = b - a;
                float amount = delta.sqrMagnitude > .000001f
                    ? Mathf.Clamp01(Vector2.Dot(position - a, delta) / delta.sqrMagnitude) : 0;
                float radius = Mathf.Lerp(first.HalfWidth, second.HalfWidth, amount);
                depth = Mathf.Max(depth, radius - Vector2.Distance(position, a + delta * amount));
            }
            return depth;
        }

        static IReadOnlyList<RiverSection> CreateRiver()
        {
            Vector3[] nodes =
            {
                new(-310, 0, CityDistricts.WorldBounds.max.z + 30), new(-310, 0, 360),
                new(-310, 0, 285), new(-310, 0, 230), new(-298, 0, 198), new(-270, 0, 180),
                new(-240, 0, 167), new(-210, 0, 155), new(-181, 0, 130), new(-140, 0, 108),
                new(-80, 0, 104), new(-20, 0, 102), new(40, 0, 103), new(100, 0, 103),
                new(160, 0, 102), new(200, 0, 120), new(228, 0, 143), Mouth
            };
            var centers = new List<Vector3>();
            var widths = new List<float>();
            for (int index = 0; index < nodes.Length - 1; index++)
            {
                Vector3 a = nodes[Mathf.Max(0, index - 1)];
                Vector3 b = nodes[index];
                Vector3 c = nodes[index + 1];
                Vector3 d = nodes[Mathf.Min(nodes.Length - 1, index + 2)];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(b, c) / 2f));
                for (int step = 0; step < steps; step++)
                {
                    float t = step / (float)steps;
                    Vector3 point = .5f * (2 * b + (-a + c) * t
                        + (2 * a - 5 * b + 4 * c - d) * t * t
                        + (-a + 3 * b - 3 * c + d) * t * t * t);
                    point.y = WaterLevel;
                    centers.Add(point);
                    float progress = (index + t) / (nodes.Length - 1f);
                    widths.Add(Mathf.Lerp(4.5f, 6f, progress)
                        + 5f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.83f, 1f, progress)));
                }
            }
            centers.Add(Mouth);
            widths.Add(11);
            var result = new List<RiverSection>();
            for (int index = 0; index < centers.Count; index++)
            {
                Vector3 direction = centers[Mathf.Min(centers.Count - 1, index + 1)]
                    - centers[Mathf.Max(0, index - 1)];
                direction.y = 0;
                Vector3 side = Vector3.Cross(Vector3.up, direction.normalized) * widths[index];
                Vector3 left = centers[index] + side;
                Vector3 right = centers[index] - side;
                if (index == centers.Count - 1)
                {
                    // Mép cuối dùng đúng đường bờ biển, không chồng một hộp nước lên cửa sông.
                    left.x = CoastX(left.z);
                    right.x = CoastX(right.z);
                }
                result.Add(new RiverSection(centers[index], left, right, widths[index]));
            }
            return Array.AsReadOnly(result.ToArray());
        }
    }
}
