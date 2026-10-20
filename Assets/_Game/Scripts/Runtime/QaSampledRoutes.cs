using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        sealed class SampledRouteSnapshot
        {
            public string id;
            public Vector3[] anchors;
            public Vector3[] samples;
            public float width;
        }

        [Serializable]
        sealed class SampledRoutesSnapshot
        {
            public SampledRouteSnapshot[] routes;
            public int matchedColliders;
            public int liveColliders;
            public Vector3[] publicTrip;
        }

        IEnumerator SampledRouteAcceptance()
        {
            Transform root = game.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(part => part.name == "LandmarkFootpaths");
            Check(root, "Sampled routes have a live path geometry root");
            BoxCollider[] colliders = root.GetComponentsInChildren<BoxCollider>(true)
                .Where(collider => collider.name == "RouteCorridorCollision").ToArray();
            var used = new HashSet<BoxCollider>();
            var rows = new List<SampledRouteSnapshot>();
            foreach (WorldRouteCorridor corridor in WorldRouteCatalog.BuildDefault())
            {
                rows.Add(new SampledRouteSnapshot
                {
                    id = corridor.Id,
                    anchors = corridor.Nodes.ToArray(),
                    samples = corridor.SampledNodes.ToArray(),
                    width = corridor.Width
                });
                if (corridor.Kind == RouteKind.Rail)
                    continue;
                foreach (WorldRouteSegment segment in WorldRouteGeometry.Segments(corridor))
                    MatchRouteCollider(corridor.Id, segment, colliders, used);
            }
            foreach (WorldPublicApproach approach in WorldRouteCatalog.BuildPublicApproaches())
            {
                rows.Add(new SampledRouteSnapshot
                {
                    id = approach.Id,
                    anchors = approach.Nodes.ToArray(),
                    samples = approach.SampledNodes.ToArray(),
                    width = approach.Width
                });
                foreach (WorldRouteSegment segment in WorldRouteGeometry.Segments(approach))
                    MatchRouteCollider(approach.Id, segment, colliders, used);
            }
            Check(used.Count == colliders.Length,
                "Every live catalog route collider matches exactly one sampled edge");
            WorldPublicApproach village = WorldRouteCatalog.BuildPublicApproaches()[1];
            Check(CityPedestrianNetwork.TryPlan(village.Nodes[0], village.Nodes[^1],
                out List<Vector3> trip, out string failure),
                "Sampled public route is connected: " + failure);
            Check(trip.Count > village.Nodes.Count,
                "Public route retains intermediate samples instead of anchor chords");
            var snapshot = new SampledRoutesSnapshot
            {
                routes = rows.ToArray(),
                matchedColliders = used.Count,
                liveColliders = colliders.Length,
                publicTrip = trip.ToArray()
            };
            File.WriteAllText(Path.Combine(game.QaDirectory, "sampled-routes.json"),
                JsonUtility.ToJson(snapshot, true));

            Transform originalTarget = game.CameraRig.Target;
            float originalDistance = game.CameraRig.Distance;
            float originalYaw = game.CameraRig.Yaw;
            var focus = new GameObject("QaSampledRouteFocus");
            Vector3[] centers = { new(-118, 0, 88), new(220, 0, 187), new(440, 0, -152) };
            string[] names = { "farm", "industrial", "marina" };
            try
            {
                for (int index = 0; index < centers.Length; index++)
                {
                    focus.transform.position = root.TransformPoint(centers[index]);
                    game.CameraRig.Target = focus.transform;
                    game.CameraRig.Distance = 42;
                    game.CameraRig.Snap();
                    yield return new WaitForSecondsRealtime(.15f);
                    Check(Mathf.Abs(game.CameraRig.Yaw - originalYaw) < .001f,
                        "Sampled route capture retains gameplay yaw " + names[index]);
                    yield return Capture("sampled-route-" + names[index] + ".png");
                }
            }
            finally
            {
                game.CameraRig.Target = originalTarget;
                game.CameraRig.Distance = originalDistance;
                game.CameraRig.Snap();
                Destroy(focus);
            }
        }

        void MatchRouteCollider(string id, WorldRouteSegment segment,
            BoxCollider[] colliders, HashSet<BoxCollider> used)
        {
            Vector3 position = segment.Center + Vector3.up * .01f;
            Vector3 size = new(segment.Width, .08f, segment.Length + .05f);
            BoxCollider match = colliders.FirstOrDefault(collider => !used.Contains(collider)
                && Vector3.Distance(collider.transform.localPosition, position) < .001f
                && Vector3.Distance(collider.size, size) < .001f
                && Quaternion.Angle(collider.transform.localRotation, segment.Rotation) < .01f);
            Check(match && match.isTrigger && match.center == Vector3.zero
                && match.transform.localScale == Vector3.one,
                "Live route trigger matches sampled position rotation width length " + id);
            used.Add(match);
        }
    }
}
