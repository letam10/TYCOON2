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
        sealed class RoundedRoutesSnapshot
        {
            public int corners;
            public int arcEdges;
            public float maximumRadiusError;
            public float maximumChordError;
            public int liveSupportSamples;
            public Vector3[] captureCenters;
        }

        IEnumerator RoundedRouteAcceptance()
        {
            var snapshot = new RoundedRoutesSnapshot();
            foreach (WorldRouteCorridor route in WorldRouteCatalog.BuildDefault())
                RecordRoundedRoute(snapshot, route.Id, route.Nodes, route.SampledNodes, route.Width);
            foreach (WorldPublicApproach route in WorldRouteCatalog.BuildPublicApproaches())
                RecordRoundedRoute(snapshot, route.Id, route.Nodes, route.SampledNodes, route.Width);
            Check(snapshot.corners >= 20 && snapshot.arcEdges > snapshot.corners,
                "Rounded route acceptance exercises real catalog arcs");

            WorldRouteCorridor ring = WorldRouteCatalog.BuildDefault()
                .First(route => route.Id == "highway-ring");
            IReadOnlyList<WorldRouteCorner> corners = WorldRouteRounding.Corners(ring.Nodes, ring.Width);
            Check(corners.Count == 4, "Closed highway ring rounds all four authored corners");
            WorldStreetMeshOwner owner = game.GetComponentsInChildren<WorldStreetMeshOwner>(true)
                .First(part => part.name == "CityStreets");
            MeshCollider road = owner.transform.Find("RoadPavement_Surface").GetComponent<MeshCollider>();
            MeshCollider sidewalk = owner.transform.Find("CitySidewalk_Surface").GetComponent<MeshCollider>();
            Physics.SyncTransforms();
            foreach (WorldRouteCorner corner in corners)
            {
                for (int index = 1; index < 10; index++)
                {
                    Vector3 point = corner.Evaluate(index / 10f);
                    Check(RoundedSupport(road, owner.transform.TransformPoint(point)),
                        "Live rounded ring pavement covers arc " + corner.AnchorIndex + ":" + index);
                    snapshot.liveSupportSamples++;
                    Vector3 radial = (point - corner.Center).normalized;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        // 0,9 m là tâm vỉa hè rộng 1,8 m, tính từ mép đường.
                        Vector3 edge = point + radial * side * (ring.Width * .5f + .9f);
                        Check(RoundedSupport(sidewalk, owner.transform.TransformPoint(edge)),
                            "Live rounded ring sidewalk covers arc " + corner.AnchorIndex + ":" + index + ":" + side);
                        snapshot.liveSupportSamples++;
                    }
                }
            }
            snapshot.captureCenters = corners.Select(corner => corner.Evaluate(.5f))
                .Concat(new[] { new Vector3(440, 0, -152), new Vector3(323, 0, 247) }).ToArray();
            File.WriteAllText(Path.Combine(game.QaDirectory, "rounded-routes.json"),
                JsonUtility.ToJson(snapshot, true));

            Transform originalTarget = game.CameraRig.Target;
            float originalDistance = game.CameraRig.Distance;
            float originalYaw = game.CameraRig.Yaw;
            var focus = new GameObject("QaRoundedRouteFocus");
            string[] names = { "ring-southwest", "ring-northwest", "ring-northeast", "ring-southeast", "marina", "industrial" };
            try
            {
                for (int index = 0; index < snapshot.captureCenters.Length; index++)
                {
                    focus.transform.position = owner.transform.TransformPoint(snapshot.captureCenters[index]);
                    game.CameraRig.Target = focus.transform;
                    game.CameraRig.Distance = 34;
                    game.CameraRig.Snap();
                    yield return new WaitForSecondsRealtime(.15f);
                    Check(Mathf.Abs(game.CameraRig.Yaw - originalYaw) < .001f,
                        "Rounded route capture retains gameplay yaw " + names[index]);
                    yield return Capture("rounded-route-" + names[index] + ".png");
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

        void RecordRoundedRoute(RoundedRoutesSnapshot snapshot, string id,
            IReadOnlyList<Vector3> anchors, IReadOnlyList<Vector3> samples, float width)
        {
            foreach (WorldRouteCorner corner in WorldRouteRounding.Corners(anchors, width))
            {
                snapshot.corners++;
                Check(samples.Any(point => Vector3.Distance(point, corner.Evaluate(.5f)) < .0005f),
                    "Canonical samples retain exact arc midpoint " + id + ":" + corner.AnchorIndex);
                for (int index = 0; index < samples.Count - 1; index++)
                {
                    float firstError = Mathf.Abs(Vector3.Distance(samples[index], corner.Center) - corner.Radius);
                    float secondError = Mathf.Abs(Vector3.Distance(samples[index + 1], corner.Center) - corner.Radius);
                    if (firstError > .0005f || secondError > .0005f)
                        continue;
                    float radiusError = Mathf.Max(firstError, secondError);
                    Vector3 midpoint = (samples[index] + samples[index + 1]) * .5f;
                    float chordError = corner.Radius - Vector3.Distance(midpoint, corner.Center);
                    snapshot.maximumRadiusError = Mathf.Max(snapshot.maximumRadiusError, radiusError);
                    snapshot.maximumChordError = Mathf.Max(snapshot.maximumChordError, chordError);
                    Check(chordError >= -.0005f && chordError <= WorldRouteRounding.MaximumChordError + .0005f,
                        "Canonical arc chord error stays within two centimetres " + id + ":" + index);
                    snapshot.arcEdges++;
                }
            }
        }

        static bool RoundedSupport(MeshCollider collider, Vector3 point)
        {
            return collider && collider.Raycast(new Ray(point + Vector3.up, Vector3.down),
                out RaycastHit hit, 2f) && Mathf.Abs(hit.point.y - point.y) < .12f;
        }
    }
}
