using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Tycoon
{
    public sealed class WorldVegetationProfile
    {
        public string Id { get; }
        public int Seed { get; }
        public float NearLodDistance { get; }
        public float FarLodDistance { get; }
        public float TargetDensity { get; }
        public float MinimumSpacing { get; }
        public float CorridorClearance { get; }
        public int LodCount { get; }

        public WorldVegetationProfile(
            string id,
            int seed,
            float nearLodDistance,
            float farLodDistance,
            float targetDensity,
            float minimumSpacing,
            float corridorClearance,
            int lodCount)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Vegetation id is required.", nameof(id));
            if (nearLodDistance < 0f || farLodDistance <= nearLodDistance)
                throw new ArgumentOutOfRangeException(nameof(farLodDistance));
            if (targetDensity < 0f || targetDensity > 1f)
                throw new ArgumentOutOfRangeException(nameof(targetDensity));
            if (minimumSpacing <= 0f || corridorClearance < 0f || lodCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(lodCount));

            Id = id;
            Seed = seed;
            NearLodDistance = nearLodDistance;
            FarLodDistance = farLodDistance;
            TargetDensity = targetDensity;
            MinimumSpacing = minimumSpacing;
            CorridorClearance = corridorClearance;
            LodCount = lodCount;
        }
    }

    public static class WorldVegetationCatalog
    {
        public const int Seed = 20261015;

        const int FarmSeedOffset = 17;
        const int ForestSeedOffset = 43;
        const int CoastalSeedOffset = 71;
        const float FarmNearLod = 28f;
        const float FarmFarLod = 86f;
        const float ForestNearLod = 34f;
        const float ForestFarLod = 118f;
        const float CoastalNearLod = 30f;
        const float CoastalFarLod = 102f;
        const float FarmDensity = 0.58f;
        const float ForestDensity = 0.82f;
        const float CoastalDensity = 0.46f;
        const float FarmSpacing = 3.5f;
        const float ForestSpacing = 2.75f;
        const float CoastalSpacing = 4.25f;
        const float FarmCorridorClearance = 4.5f;
        const float ForestCorridorClearance = 5.5f;
        const float CoastalCorridorClearance = 6f;
        const int FarmLodCount = 3;
        const int ForestLodCount = 4;
        const int CoastalLodCount = 3;
        const int SpeciesVariantCount = 4;
        const int CoordinateScale = 100;

        static readonly IReadOnlyList<WorldVegetationProfile> profileValues =
            new ReadOnlyCollection<WorldVegetationProfile>(new[]
            {
                new WorldVegetationProfile(
                    "farm",
                    Seed + FarmSeedOffset,
                    FarmNearLod,
                    FarmFarLod,
                    FarmDensity,
                    FarmSpacing,
                    FarmCorridorClearance,
                    FarmLodCount),
                new WorldVegetationProfile(
                    "forest",
                    Seed + ForestSeedOffset,
                    ForestNearLod,
                    ForestFarLod,
                    ForestDensity,
                    ForestSpacing,
                    ForestCorridorClearance,
                    ForestLodCount),
                new WorldVegetationProfile(
                    "coastal",
                    Seed + CoastalSeedOffset,
                    CoastalNearLod,
                    CoastalFarLod,
                    CoastalDensity,
                    CoastalSpacing,
                    CoastalCorridorClearance,
                    CoastalLodCount)
            });

        public static IReadOnlyList<WorldVegetationProfile> Profiles => profileValues;

        public static WorldVegetationProfile Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (int index = 0; index < profileValues.Count; index++)
            {
                WorldVegetationProfile profile = profileValues[index];
                if (string.Equals(profile.Id, id, StringComparison.Ordinal))
                    return profile;
            }
            return null;
        }

        public static int StableSpecies(string biome, Vector3 position)
        {
            return (int)(StableHash(biome, position) % SpeciesVariantCount);
        }

        public static float StableValue(string biome, Vector3 position)
        {
            uint hash = StableHash(biome, position);
            return hash / (float)uint.MaxValue;
        }

        public static bool IsInsideCorridor(
            Vector3 position,
            IEnumerable<WorldRouteCorridor> corridors,
            float clearance)
        {
            if (corridors == null || !IsFinite(position))
                return false;

            float safeClearance = Mathf.Max(0f, clearance);
            foreach (WorldRouteCorridor corridor in corridors)
            {
                if (corridor == null || corridor.Nodes == null || corridor.Nodes.Count < 2)
                    continue;

                float radius = corridor.Width * 0.5f + safeClearance;
                if (!IsFinite(radius) || radius < 0f)
                    continue;
                float radiusSquared = radius * radius;
                for (int nodeIndex = 1; nodeIndex < corridor.Nodes.Count; nodeIndex++)
                {
                    Vector3 start = corridor.Nodes[nodeIndex - 1];
                    Vector3 end = corridor.Nodes[nodeIndex];
                    if (!IsFinite(start) || !IsFinite(end))
                        continue;
                    if (DistanceSquaredToSegmentXZ(position, start, end) <= radiusSquared)
                        return true;
                }
            }
            return false;
        }

        // Băm FNV-1a độc lập runtime để cùng biome và tọa độ luôn ổn định.
        static uint StableHash(string biome, Vector3 position)
        {
            unchecked
            {
                uint hash = 2166136261u;
                string text = biome ?? string.Empty;
                for (int index = 0; index < text.Length; index++)
                {
                    hash ^= text[index];
                    hash *= 16777619u;
                }

                hash ^= (uint)Quantize(position.x);
                hash *= 16777619u;
                hash ^= (uint)Quantize(position.y);
                hash *= 16777619u;
                hash ^= (uint)Quantize(position.z);
                hash *= 16777619u;
                hash ^= (uint)Seed;
                hash *= 16777619u;
                return hash;
            }
        }

        static int Quantize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0;
            return Mathf.FloorToInt(value * CoordinateScale);
        }

        // Kiểm tra khoảng cách trên mặt bằng XZ, cộng bề rộng corridor và clearance.
        static float DistanceSquaredToSegmentXZ(Vector3 point, Vector3 start, Vector3 end)
        {
            float dx = end.x - start.x;
            float dz = end.z - start.z;
            float lengthSquared = dx * dx + dz * dz;
            if (lengthSquared <= 0.000001f)
            {
                float pointDx = point.x - start.x;
                float pointDz = point.z - start.z;
                return pointDx * pointDx + pointDz * pointDz;
            }

            float offsetX = point.x - start.x;
            float offsetZ = point.z - start.z;
            float projection = Mathf.Clamp01((offsetX * dx + offsetZ * dz) / lengthSquared);
            float nearestX = start.x + dx * projection;
            float nearestZ = start.z + dz * projection;
            float deltaX = point.x - nearestX;
            float deltaZ = point.z - nearestZ;
            return deltaX * deltaX + deltaZ * deltaZ;
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
