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
        sealed class PathOwnershipRow
        {
            public string id;
            public int priority;
            public int pieces;
            public float area;
        }

        [Serializable]
        sealed class PathOwnershipSnapshot
        {
            public PathOwnershipRow[] requests;
            public int polygonPieces;
            public int meshBatches;
            public int colliderSamples;
            public float maximumPathOverlap;
            public float maximumStreetOverlap;
        }

        IEnumerator PathSurfaceOwnershipAcceptance()
        {
            WorldPathSurfaceState state = game.GetComponentInChildren<WorldPathSurfaceState>(true);
            Check(state && state.Layout != null, "Owned path surfaces expose their live layout");
            WorldPathSurfaceLayout layout = state.Layout;
            foreach (WorldPathSurfaceRequest expected in WorldPathSurfaceCatalog.BuildDefault())
                Check(layout.Requests.Any(request => ReferenceEquals(request, expected)),
                    "Path surface keeps the authoritative sampled request " + expected.Id);
            Check(layout.Requests.Select(request => request.Id).Distinct().Count() == layout.Requests.Count,
                "Path surface request IDs are unique including landmark legs");
            CityMapGraphic[] maps = game.GetComponentsInChildren<CityMapGraphic>(true);
            Check(maps.Length >= 2, "Owned path layout is available to minimap and full map");
            foreach (CityMapGraphic graphic in maps)
                Check(ReferenceEquals(graphic.PathSurfaces, layout),
                    "Map reads the exact owned path layout " + graphic.name);

            MeshFilter[] filters = state.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.name.StartsWith("PathSurface_", StringComparison.Ordinal)).ToArray();
            Check(filters.Length > 0, "Owned paths have rendered mesh batches");
            var colliders = new List<MeshCollider>();
            foreach (MeshFilter filter in filters)
            {
                MeshCollider collider = filter.GetComponent<MeshCollider>();
                Check(filter.sharedMesh && collider && collider.enabled && !collider.isTrigger
                    && collider.sharedMesh == filter.sharedMesh,
                    "Owned path rendering and collision share one mesh " + filter.name);
                Check(filter.transform.localPosition == Vector3.zero
                    && filter.transform.localRotation == Quaternion.identity
                    && filter.transform.localScale == Vector3.one,
                    "Owned path batch has identity local pose " + filter.name);
                colliders.Add(collider);
            }
            Physics.SyncTransforms();
            int samples = 0;
            double maximumPath = 0;
            double maximumStreet = 0;
            string pathPair = "";
            string streetPair = "";
            for (int index = 0; index < layout.Pieces.Count; index++)
            {
                WorldPathSurfacePiece piece = layout.Pieces[index];
                Vector3 center = Vector3.zero;
                foreach (Vector3 point in piece.Geometry.Outline)
                    center += point;
                center /= piece.Geometry.Outline.Count;
                Vector3 expected = state.transform.TransformPoint(center + Vector3.up * piece.Request.Top);
                var ray = new Ray(expected + state.transform.up * .5f, -state.transform.up);
                bool matched = colliders.Any(collider => collider.Raycast(ray, out RaycastHit hit, 1f)
                    && Vector3.Distance(hit.point, expected) < .002f);
                Check(matched, "Live path collider covers owned polygon " + piece.Request.Id + ":" + index);
                samples++;
                for (int previous = 0; previous < index; previous++)
                {
                    WorldPathSurfacePiece other = layout.Pieces[previous];
                    if (piece.Geometry.Source.Start.y != other.Geometry.Source.Start.y)
                        continue;
                    double overlap = OwnershipIntersectionArea(piece.Geometry.Outline, other.Geometry.Outline);
                    if (overlap <= maximumPath)
                        continue;
                    maximumPath = overlap;
                    pathPair = piece.Request.Id + " / " + other.Request.Id;
                }
                foreach (WorldStreetSurface blocker in layout.Blockers)
                {
                    if (piece.Geometry.Source.Start.y != blocker.Source.Start.y)
                        continue;
                    double overlap = OwnershipIntersectionArea(piece.Geometry.Outline, blocker.Outline);
                    if (overlap <= maximumStreet)
                        continue;
                    maximumStreet = overlap;
                    streetPair = piece.Request.Id + " / " + blocker.RouteId;
                }
            }
            Check(maximumPath < .005, "Owned path polygons have no area overlap: "
                + maximumPath + " " + pathPair);
            Check(maximumStreet < .005, "Owned paths respect street footprints: "
                + maximumStreet + " " + streetPair);
            var snapshot = new PathOwnershipSnapshot
            {
                requests = layout.Requests.Select(request => new PathOwnershipRow
                {
                    id = request.Id,
                    priority = request.Priority,
                    pieces = layout.Pieces.Count(piece => ReferenceEquals(piece.Request, request)),
                    area = (float)layout.Pieces.Where(piece => ReferenceEquals(piece.Request, request))
                        .Sum(piece => OwnershipArea(piece.Geometry.Outline))
                }).ToArray(),
                polygonPieces = layout.Pieces.Count,
                meshBatches = filters.Length,
                colliderSamples = samples,
                maximumPathOverlap = (float)maximumPath,
                maximumStreetOverlap = (float)maximumStreet
            };
            File.WriteAllText(Path.Combine(game.QaDirectory, "path-surface-ownership.json"),
                JsonUtility.ToJson(snapshot, true));

            Transform originalTarget = game.CameraRig.Target;
            float originalDistance = game.CameraRig.Distance;
            float originalYaw = game.CameraRig.Yaw;
            var focus = new GameObject("QaPathOwnershipFocus");
            Vector3[] centers = { new(325, 0, 250), new(420, 0, -120), new(190, 0, 29) };
            string[] names = { "industrial", "marina", "ring" };
            try
            {
                for (int index = 0; index < centers.Length; index++)
                {
                    focus.transform.position = state.transform.TransformPoint(centers[index]);
                    game.CameraRig.Target = focus.transform;
                    game.CameraRig.Distance = 42;
                    game.CameraRig.Snap();
                    yield return new WaitForSecondsRealtime(.15f);
                    Check(Mathf.Abs(game.CameraRig.Yaw - originalYaw) < .001f,
                        "Owned surface capture preserves camera yaw " + names[index]);
                    yield return Capture("path-owner-" + names[index] + ".png");
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

        static double OwnershipArea(IReadOnlyList<Vector3> polygon)
        {
            double area = 0;
            for (int index = 1; index + 1 < polygon.Count; index++)
                area += ((double)polygon[index].x - polygon[0].x)
                    * ((double)polygon[index + 1].z - polygon[0].z)
                    - ((double)polygon[index].z - polygon[0].z)
                    * ((double)polygon[index + 1].x - polygon[0].x);
            return Math.Abs(area) * .5;
        }

        // Oracle giao đa giác độc lập với phép trừ polygon của bộ dựng hình.
        static double OwnershipIntersectionArea(IReadOnlyList<Vector3> first, IReadOnlyList<Vector3> second)
        {
            if (first.Max(point => point.x) <= second.Min(point => point.x)
                || second.Max(point => point.x) <= first.Min(point => point.x)
                || first.Max(point => point.z) <= second.Min(point => point.z)
                || second.Max(point => point.z) <= first.Min(point => point.z))
                return 0;
            var points = first.Select(point => ((double)point.x, (double)point.z)).ToList();
            for (int edge = 0; edge < second.Count && points.Count > 0; edge++)
            {
                Vector3 a = second[edge];
                Vector3 b = second[(edge + 1) % second.Count];
                var clipped = new List<(double, double)>();
                for (int index = 0; index < points.Count; index++)
                {
                    var current = points[index];
                    var next = points[(index + 1) % points.Count];
                    double side = ((double)b.x - a.x) * (current.Item2 - a.z)
                        - ((double)b.z - a.z) * (current.Item1 - a.x);
                    double nextSide = ((double)b.x - a.x) * (next.Item2 - a.z)
                        - ((double)b.z - a.z) * (next.Item1 - a.x);
                    if (side >= 0)
                        clipped.Add(current);
                    if ((side < 0 && nextSide > 0) || (side > 0 && nextSide < 0))
                    {
                        double amount = side / (side - nextSide);
                        clipped.Add((current.Item1 + (next.Item1 - current.Item1) * amount,
                            current.Item2 + (next.Item2 - current.Item2) * amount));
                    }
                }
                points = clipped;
            }
            double area = 0;
            for (int index = 1; index + 1 < points.Count; index++)
                area += (points[index].Item1 - points[0].Item1) * (points[index + 1].Item2 - points[0].Item2)
                    - (points[index].Item2 - points[0].Item2) * (points[index + 1].Item1 - points[0].Item1);
            return Math.Abs(area) * .5;
        }
    }
}
