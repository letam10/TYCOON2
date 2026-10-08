using UnityEngine;

namespace Tycoon
{
    public static class TownFurnitureCollision
    {
        public static GameObject Model(string key, Vector3 point, Transform parent, float scale = 1,
            float yaw = 0, float support = .06f)
        {
            var model = Art.Model(key, point, parent, scale, yaw);
            Ground(model.transform, support + point.y);
            Fit(model.transform);
            return model;
        }

        public static void Ground(Transform model, float support)
        {
            var bounds = BoundsIn(model, model.parent);
            model.localPosition += Vector3.up * (support - bounds.min.y);
        }

        public static BoxCollider Fit(Transform model)
        {
            foreach (var previous in model.GetComponentsInChildren<Collider>(true))
            {
                previous.enabled = false;
                if (Application.isPlaying) Object.Destroy(previous);
                else Object.DestroyImmediate(previous);
            }
            var bounds = BoundsIn(model, model);
            var collider = model.gameObject.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            return collider;
        }

        public static Bounds BoundsIn(Transform model, Transform space)
        {
            var bounds = new Bounds();
            bool found = false;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                var local = renderer.localBounds;
                var matrix = (space ? space.worldToLocalMatrix : Matrix4x4.identity)
                    * renderer.localToWorldMatrix;
                // Đổi tám góc về hệ của model để yaw không làm phình collider.
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = new(
                        (corner & 1) == 0 ? local.min.x : local.max.x,
                        (corner & 2) == 0 ? local.min.y : local.max.y,
                        (corner & 4) == 0 ? local.min.z : local.max.z);
                    point = matrix.MultiplyPoint3x4(point);
                    if (!found)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                    else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }
    }
}
