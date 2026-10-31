using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class WorldHydrologyBridges
    {
        public const float DeckHeight = .3f;

        public static void Build(Transform parent)
        {
            var root = new GameObject("RiverBridges").transform;
            root.SetParent(parent, false);
            var geometry = new CityStaticGeometry(root);
            foreach (WorldRouteCorridor route in WorldRouteCatalog.BuildDefault())
            {
                if (route.Kind == RouteKind.Rail)
                    continue;
                Route(route.Id, route.SampledNodes, route.Width);
            }
            foreach (WorldPublicApproach route in WorldRouteCatalog.BuildPublicApproaches())
                Route(route.Id, route.SampledNodes, route.Width);
            geometry.Finish();

            void Route(string id, IReadOnlyList<Vector3> nodes, float width)
            {
                var samples = new List<Vector3> { nodes[0] };
                for (int index = 1; index < nodes.Count; index++)
                {
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(nodes[index - 1], nodes[index]) / 2));
                    for (int step = 1; step <= steps; step++)
                        samples.Add(Vector3.Lerp(nodes[index - 1], nodes[index], step / (float)steps));
                }
                nodes = samples;
                int first = -1;
                for (int index = 1; index <= nodes.Count; index++)
                {
                    bool wet = index < nodes.Count
                        && WorldHydrologyLayout.IsRiver((nodes[index - 1] + nodes[index]) * .5f,
                            -WorldHydrologyLayout.BankWidth);
                    if (wet && first < 0)
                        first = index - 1;
                    if (wet || first < 0)
                        continue;
                    int last = index - 1;
                    for (int segment = first + 1; segment <= last; segment++)
                        Deck(nodes[segment - 1], nodes[segment], width + 1.2f);
                    Vector3 start = nodes[first];
                    Vector3 end = nodes[last];
                    Vector3 startDirection = (nodes[first + 1] - start).normalized;
                    Vector3 endDirection = (end - nodes[last - 1]).normalized;
                    float rampLength = (DeckHeight - .0555f) / .08f;
                    Ramp(start - startDirection * rampLength, start, width + 1.2f);
                    Ramp(end + endDirection * rampLength, end, width + 1.2f);
                    first = -1;
                }
            }

            void Deck(Vector3 a, Vector3 b, float width)
            {
                a.y = 0;
                b.y = 0;
                Vector3 delta = b - a;
                Quaternion rotation = Quaternion.LookRotation(delta);
                Vector3 size = new(width, .26f, delta.magnitude + .035f);
                Vector3 center = (a + b) * .5f + Vector3.up * .17f;
                geometry.RotatedBox(center, size, rotation, "wood", "#8D785A");
                geometry.Collider("RiverBridgeDeck", center, size, rotation);
                Vector3 side = Vector3.Cross(Vector3.up, delta.normalized) * (width * .5f - .08f);
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 offset = side * sign;
                    geometry.Beam(a + offset + Vector3.up * 1.1f,
                        b + offset + Vector3.up * 1.1f, .1f, "wood", "#665641");
                    geometry.Cylinder(a + offset + Vector3.up * .7f, .09f, .8f, "wood", "#665641");
                    geometry.Collider("BridgeRailing", center + offset + Vector3.up * .55f,
                        new Vector3(.12f, 1f, delta.magnitude + .03f), rotation);
                }
            }

            void Ramp(Vector3 bottom, Vector3 top, float width)
            {
                bottom.y = .0555f;
                top.y = DeckHeight;
                Vector3 direction = top - bottom;
                direction.y = 0;
                Vector3 side = Vector3.Cross(Vector3.up, direction.normalized) * (width * .5f);
                var mesh = new Mesh { name = "HydrologyBridgeRamp" };
                mesh.vertices = new[] { bottom - side, bottom + side, top + side, top - side };
                mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                geometry.Sculpt(mesh, Vector3.zero, "wood", "#8D785A");
                var collision = new GameObject("RiverBridgeRamp");
                collision.transform.SetParent(root, false);
                collision.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
        }
    }
}
