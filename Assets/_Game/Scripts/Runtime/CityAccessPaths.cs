using UnityEngine;

namespace Tycoon
{
    public static class CityAccessPaths
    {
        public static void Build(Transform world)
        {
            var root = new GameObject("LandmarkFootpaths").transform;
            root.SetParent(world, false);
            var g = new CityStaticGeometry(root);
            foreach (var landmark in world.GetComponentsInChildren<CityLandmark>())
            {
                Vector3 a = world.InverseTransformPoint(landmark.Approach);
                if (landmark.Id == "CoffeeHouse")
                {
                    Vector3 b = new(-181, 0, a.z);
                    Pave(a, b);
                    Pave(b, new(-181, 0, -120));
                }
                else if (landmark.Id is "PoliceStation" or "FireStation" or "Bank")
                    Pave(a, new(-204, 0, a.z));
                else if (landmark.Id is "Harbor" or "Marina" or "Beach")
                    Pave(a, new(199, 0, a.z));
                else if (landmark.Id == "SeasideBar")
                    Pave(a, new(181, 0, a.z));
                else
                    Pave(a, new(a.x, 0, 164));
            }
            g.Finish();

            void Pave(Vector3 a, Vector3 b)
            {
                a.y = 0;
                b.y = 0;
                Vector3 delta = b - a;
                if (delta.sqrMagnitude < .001f) return;
                float length = delta.magnitude;
                Vector3 p = (a + b) * .5f;
                g.Box(p + Vector3.up * .025f, new(2.6f, .05f, length), "paving", "#C0B8A0",
                    Quaternion.LookRotation(delta).eulerAngles.y);
                Vector3 size = Mathf.Abs(delta.x) > Mathf.Abs(delta.z)
                    ? new(length + .05f, .08f, 2.6f) : new(2.6f, .08f, length + .05f);
                // Lối lát là vùng đi bộ, không phải tường; trigger giữ vùng định vị mà không chặn capsule.
                g.Collider("PedestrianLink", p + Vector3.up * .01f, size, true);
            }
        }
    }
}
