using UnityEngine;

namespace Tycoon
{
    public static class CityCameraFraming
    {
        public static float ResolveDistance(float desiredDistance, float blockingDistance,
            float padding, float minimumDistance)
        {
            if (!float.IsFinite(desiredDistance) || desiredDistance <= 0f)
                return Mathf.Max(0.1f, minimumDistance);

            if (!float.IsFinite(blockingDistance) || blockingDistance >= desiredDistance)
                return desiredDistance;

            float safePadding = Mathf.Max(0f, float.IsFinite(padding) ? padding : 0f);
            float safeMinimum = Mathf.Clamp(
                float.IsFinite(minimumDistance) ? minimumDistance : .5f,
                .1f,
                desiredDistance);
            return Mathf.Clamp(blockingDistance - safePadding, safeMinimum, desiredDistance);
        }

        public static float OverviewDistance(Camera lens, Quaternion rotation)
        {
            var bounds = CityDistricts.WorldBounds;
            bounds.size = new Vector3(bounds.size.x, 24, bounds.size.z);
            float tangent = Mathf.Tan(lens.fieldOfView * Mathf.Deg2Rad * .5f);
            float distance = 0;
            var inverse = Quaternion.Inverse(rotation);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        Vector3 local = inverse * corner;
                        distance = Mathf.Max(distance, Mathf.Abs(local.x) / (tangent * lens.aspect) - local.z);
                        distance = Mathf.Max(distance, Mathf.Abs(local.y) / tangent - local.z);
                    }
            return distance * 1.06f;
        }
    }
}
