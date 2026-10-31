using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace Tycoon
{
    public static class WorldHydrologyNavigation
    {
        public static readonly Bounds[] PierBounds =
        {
            new(new Vector3(233, 0, -141), new Vector3(30, 4, 3.2f)),
            new(new Vector3(233, 0, -105), new Vector3(30, 4, 3.2f)),
            new(new Vector3(233, 0, -52), new Vector3(30, 4, 3.2f)),
            new(new Vector3(233, 0, -28), new Vector3(30, 4, 3.2f)),
            new(new Vector3(228, 0, -247), new Vector3(120, 4, 5.6f)),
            new(new Vector3(228, 0, -188), new Vector3(120, 4, 5.6f))
        };

        public static void BuildOcean(Transform root, Bounds world)
        {
            var rows = new SortedSet<float> { world.min.z, world.max.z };
            for (float z = world.min.z; z < world.max.z; z += 4)
                rows.Add(z);
            foreach (Bounds pier in PierBounds)
            {
                rows.Add(pier.min.z);
                rows.Add(pier.max.z);
            }
            float previous = world.min.z;
            foreach (float next in rows)
            {
                if (next <= previous)
                    continue;
                float z = (previous + next) * .5f;
                float coast = Mathf.Min(WorldHydrologyLayout.CoastX(previous), WorldHydrologyLayout.CoastX(next));
                var gaps = new List<Vector2>();
                foreach (Bounds pier in PierBounds)
                    if (z > pier.min.z && z < pier.max.z)
                        gaps.Add(new Vector2(pier.min.x, pier.max.x));
                gaps.Sort((a, b) => a.x.CompareTo(b.x));
                float start = coast;
                foreach (Vector2 gap in gaps)
                {
                    if (gap.x > start)
                        Volume(root, new Vector3((start + gap.x) * .5f, 0, z),
                            new Vector3(gap.x - start, 4, next - previous), Quaternion.identity);
                    start = Mathf.Max(start, gap.y);
                }
                float end = world.max.x + 3;
                if (start < end)
                    Volume(root, new Vector3((start + end) * .5f, 0, z),
                        new Vector3(end - start, 4, next - previous), Quaternion.identity);
                Vector3 shore = new(WorldHydrologyLayout.CoastX(z), 0, z);
                if (!IsPier(shore) && Mathf.Abs(z - WorldHydrologyLayout.Mouth.z) > 14)
                {
                    Vector3 a = new(WorldHydrologyLayout.CoastX(previous), 0, previous);
                    Vector3 b = new(WorldHydrologyLayout.CoastX(next), 0, next);
                    Barrier(root, "CoastalWaterBoundary", a, b);
                }
                previous = next;
            }
        }

        public static void BuildRiver(Transform root)
        {
            var river = WorldHydrologyLayout.River;
            Bounds world = CityDistricts.WorldBounds;
            for (int index = 1; index < river.Count; index++)
            {
                RiverSection a = river[index - 1];
                RiverSection b = river[index];
                Vector3 center = (a.Center + b.Center) * .5f;
                if (center.z > world.max.z || center.z < world.min.z)
                    continue;
                Vector3 delta = b.Center - a.Center;
                delta.y = 0;
                if (!IsRouteCrossing(center, 1.4f))
                    Volume(root, center, new Vector3(a.HalfWidth + b.HalfWidth, 4, delta.magnitude + .1f),
                        Quaternion.LookRotation(delta));
                if (!IsRouteCrossing((a.Left + b.Left) * .5f, 1.4f))
                    Barrier(root, "RiverBank", a.Left, b.Left);
                if (!IsRouteCrossing((a.Right + b.Right) * .5f, 1.4f))
                    Barrier(root, "RiverBank", a.Right, b.Right);
            }
        }

        public static bool IsPier(Vector3 point)
        {
            foreach (Bounds bounds in PierBounds)
                if (point.x >= bounds.min.x && point.x <= bounds.max.x
                    && point.z >= bounds.min.z && point.z <= bounds.max.z)
                    return true;
            return false;
        }

        public static bool IsRouteCrossing(Vector3 point, float padding)
        {
            foreach (WorldRouteCorridor route in WorldRouteCatalog.BuildDefault())
            {
                if (route.Kind == RouteKind.Rail)
                    continue;
                if (Near(point, route.SampledNodes, route.Width * .5f + padding))
                    return true;
            }
            foreach (WorldPublicApproach route in WorldRouteCatalog.BuildPublicApproaches())
                if (Near(point, route.SampledNodes, route.Width * .5f + padding))
                    return true;
            return false;
        }

        static bool Near(Vector3 point, IReadOnlyList<Vector3> nodes, float radius)
        {
            point.y = 0;
            for (int index = 1; index < nodes.Count; index++)
            {
                Vector3 a = nodes[index - 1];
                Vector3 b = nodes[index];
                a.y = 0;
                b.y = 0;
                Vector3 delta = b - a;
                float t = delta.sqrMagnitude > .000001f
                    ? Mathf.Clamp01(Vector3.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
                if ((point - a - delta * t).sqrMagnitude <= radius * radius)
                    return true;
            }
            return false;
        }

        static void Volume(Transform root, Vector3 center, Vector3 size, Quaternion rotation)
        {
            if (size.x <= .01f || size.z <= .01f)
                return;
            var node = new GameObject("HydrologyNotWalkable");
            node.transform.SetParent(root, false);
            node.transform.localPosition = center;
            node.transform.localRotation = rotation;
            var volume = node.AddComponent<NavMeshModifierVolume>();
            volume.size = size;
            volume.area = 1;
        }

        static void Barrier(Transform root, string name, Vector3 a, Vector3 b)
        {
            a.y = 0;
            b.y = 0;
            Vector3 delta = b - a;
            if (delta.sqrMagnitude < .0001f)
                return;
            var node = new GameObject(name);
            node.transform.SetParent(root, false);
            node.transform.localPosition = (a + b) * .5f + Vector3.up * .55f;
            node.transform.localRotation = Quaternion.LookRotation(delta);
            var collider = node.AddComponent<BoxCollider>();
            collider.size = new Vector3(.12f, 1.2f, delta.magnitude + .03f);
        }
    }
}
