using UnityEngine;

namespace Tycoon
{
    /// <summary>
    /// Shared deterministic water and shoreline queries for floating environment props.
    /// </summary>
    public static class CityWaterSurface
    {
        public const float WaterBaseline = .0525f;
        public const float ShorelineX = 247f;
        public const float ShorelineWidth = 5f;

        public static float SampleWaterHeight(Vector3 worldPosition)
        {
            if (!IsFinite(worldPosition.x) || !IsFinite(worldPosition.z))
                return WaterBaseline;

            float height = WaterBaseline
                + CityCoastWaterMesh.LayeredDisplacement(worldPosition.x, worldPosition.z);
            return IsFinite(height) ? height : WaterBaseline;
        }

        public static float SampleShorelineBlend(Vector3 worldPosition)
        {
            if (!IsFinite(worldPosition.x) || !IsFinite(worldPosition.z))
                return 0f;

            float shorelineOffset = .6f * Mathf.Sin(worldPosition.z * .06f)
                + .25f * Mathf.Sin(worldPosition.z * .13f);
            float distance = Mathf.Abs(worldPosition.x - ShorelineX - shorelineOffset);
            float blend = 1f - Mathf.SmoothStep(0f, ShorelineWidth, distance);
            return Mathf.Clamp01(IsFinite(blend) ? blend : 0f);
        }

        public static Vector3 SnapFloatingObject(Vector3 worldPosition)
        {
            worldPosition.y = SampleWaterHeight(worldPosition);
            return worldPosition;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
