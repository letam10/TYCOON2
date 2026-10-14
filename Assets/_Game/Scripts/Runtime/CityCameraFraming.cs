using UnityEngine;

namespace Tycoon
{
    public static class CityCameraFraming
    {
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
