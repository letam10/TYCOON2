using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Tycoon
{
    public static class CityBiomeVegetation
    {
        public const string ForestBiomeId = "forest";
        public const string FarmBiomeId = "farm";
        public const string CoastalBiomeId = "coastal";

        static readonly IReadOnlyList<Placement> EmptyPlacements =
            new ReadOnlyCollection<Placement>(new List<Placement>());

        public readonly struct Placement
        {
            public Placement(
                string biomeId,
                int index,
                Vector3 position,
                float yaw,
                float scale,
                string category)
            {
                BiomeId = biomeId;
                Index = index;
                Position = position;
                Yaw = yaw;
                Scale = scale;
                Category = category;
            }

            public string BiomeId { get; }
            public int Index { get; }
            public int SampleIndex => Index;
            public Vector3 Position { get; }
            public float Yaw { get; }
            public float Scale { get; }
            public string Category { get; }
        }

        public static IReadOnlyList<Placement> Generate(
            string biomeId,
            int seed,
            IReadOnlyList<Vector3> samples,
            float clearanceRadius)
        {
            if (!IsAllowedBiome(biomeId) || samples == null || samples.Count == 0)
                return EmptyPlacements;

            float clearance = IsFinite(clearanceRadius) ? Mathf.Max(0f, clearanceRadius) : 0f;
            float clearanceSquared = clearance * clearance;
            List<Placement> placements = new List<Placement>(samples.Count);

            for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
            {
                Vector3 position = samples[sampleIndex];
                if (!IsFinite(position))
                    continue;
                if (OverlapsAccepted(position, placements, clearanceSquared))
                    continue;

                uint hash = StableHash(biomeId, seed, sampleIndex);
                float yaw = UnitValue(hash) * 360f;
                float scale = ScaleFor(biomeId, hash);
                string category = CategoryFor(biomeId, hash);
                placements.Add(new Placement(
                    biomeId,
                    sampleIndex,
                    position,
                    yaw,
                    scale,
                    category));
            }

            return new ReadOnlyCollection<Placement>(placements);
        }

        public static bool IsAllowedBiome(string biomeId)
        {
            return string.Equals(biomeId, ForestBiomeId, StringComparison.Ordinal)
                || string.Equals(biomeId, FarmBiomeId, StringComparison.Ordinal)
                || string.Equals(biomeId, CoastalBiomeId, StringComparison.Ordinal);
        }

        static bool OverlapsAccepted(
            Vector3 candidate,
            IReadOnlyList<Placement> placements,
            float clearanceSquared)
        {
            if (clearanceSquared <= 0f)
                return false;

            for (int index = 0; index < placements.Count; index++)
            {
                Vector3 existing = placements[index].Position;
                float dx = candidate.x - existing.x;
                float dz = candidate.z - existing.z;
                if (dx * dx + dz * dz < clearanceSquared)
                    return true;
            }

            return false;
        }

        static float ScaleFor(string biomeId, uint hash)
        {
            float value = UnitValue(hash >> 8);
            if (string.Equals(biomeId, ForestBiomeId, StringComparison.Ordinal))
                return Mathf.Lerp(0.85f, 1.25f, value);
            if (string.Equals(biomeId, FarmBiomeId, StringComparison.Ordinal))
                return Mathf.Lerp(0.75f, 1.05f, value);
            return Mathf.Lerp(0.8f, 1.15f, value);
        }

        static string CategoryFor(string biomeId, uint hash)
        {
            if (string.Equals(biomeId, ForestBiomeId, StringComparison.Ordinal))
                return (hash % 3u) switch
                {
                    0u => "tree",
                    1u => "shrub",
                    _ => "fern"
                };
            if (string.Equals(biomeId, FarmBiomeId, StringComparison.Ordinal))
                return (hash % 3u) switch
                {
                    0u => "crop",
                    1u => "hay",
                    _ => "hedge"
                };
            return (hash % 3u) switch
            {
                0u => "palm",
                1u => "grass",
                _ => "rock"
            };
        }

        // FNV-1a cục bộ giữ placement ổn định giữa các phiên chạy và nền tảng.
        static uint StableHash(string biomeId, int seed, int sampleIndex)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int index = 0; index < biomeId.Length; index++)
                {
                    hash ^= biomeId[index];
                    hash *= 16777619u;
                }

                hash ^= (uint)seed;
                hash *= 16777619u;
                hash ^= (uint)sampleIndex;
                hash *= 16777619u;
                return hash;
            }
        }

        static float UnitValue(uint hash)
        {
            return (hash & 0x00ffffffu) / 16777215f;
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
