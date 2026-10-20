using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        sealed class StreetSurfaceSnapshot
        {
            public string[] routeIds;
            public int pavementPieces;
            public int sidewalkPieces;
            public float pavementArea;
            public float sidewalkArea;
            public int colliderSamples;
            public Vector3[] captureCenters;
        }

        IEnumerator StreetSurfaceAcceptance()
        {
            WorldStreetMeshOwner owner = game.GetComponentInChildren<WorldStreetMeshOwner>();
            Check(owner, "Street surfaces own their generated meshes");
            WorldStreetLayout layout = WorldStreetGeometry.Build(WorldRouteCatalog.BuildStreetNetwork());
            var centers = new[]
            {
                new Vector3(-116, 0, -45),
                new Vector3(46, 0, 29),
                new Vector3(102, 0, 29)
            };
            var snapshot = new StreetSurfaceSnapshot
            {
                routeIds = WorldRouteCatalog.BuildStreetNetwork().Select(route => route.Id).ToArray(),
                pavementPieces = layout.Pavements.Count,
                sidewalkPieces = layout.Sidewalks.Count,
                pavementArea = layout.Pavements.Sum(StreetSurfaceArea),
                sidewalkArea = layout.Sidewalks.Sum(StreetSurfaceArea),
                captureCenters = centers
            };
            Physics.SyncTransforms();
            for (int group = 0; group < 2; group++)
            {
                bool sidewalk = group == 1;
                string name = sidewalk ? "CitySidewalk_Surface" : "RoadPavement_Surface";
                Transform part = owner.transform.Find(name);
                Check(part, "Street surface exists " + name);
                Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
                MeshCollider collider = part.GetComponent<MeshCollider>();
                Check(mesh && collider && collider.enabled && !collider.isTrigger
                    && collider.sharedMesh == mesh, "Street rendering and collider share exact mesh " + name);
                Check(part.localPosition == Vector3.zero && part.localRotation == Quaternion.identity
                    && part.localScale == Vector3.one,
                    "Street surface mesh and collider use the same identity transform " + name);
                float bottom = sidewalk ? .045f : .030f;
                float top = sidewalk ? .085f : .058f;
                Check(Mathf.Abs(mesh.bounds.min.y - bottom) < .0001f
                    && Mathf.Abs(mesh.bounds.max.y - top) < .0001f,
                    "Street surface preserves the specified ground heights " + name);
                var surfaces = sidewalk ? layout.Sidewalks : layout.Pavements;
                foreach (WorldStreetSurface surface in surfaces)
                {
                    Vector3 center = Vector3.zero;
                    foreach (Vector3 point in surface.Outline)
                        center += point;
                    center /= surface.Outline.Count;
                    Vector3 expected = part.TransformPoint(center + Vector3.up * top);
                    var ray = new Ray(expected + part.up * .5f, -part.up);
                    bool hit = collider.Raycast(ray, out RaycastHit contact, 1f);
                    Check(hit && Vector3.Distance(contact.point, expected) < .002f,
                        "Street collider covers rendered polygon " + surface.RouteId
                        + " side=" + surface.Side + " piece=" + surface.PieceIndex);
                    snapshot.colliderSamples++;
                }
            }
            File.WriteAllText(Path.Combine(game.QaDirectory, "street-surfaces.json"),
                JsonUtility.ToJson(snapshot, true));

            Transform originalTarget = game.CameraRig.Target;
            float originalDistance = game.CameraRig.Distance;
            float originalYaw = game.CameraRig.Yaw;
            var focus = new GameObject("QaStreetSurfaceFocus");
            string[] names = { "collinear", "tee", "cross" };
            try
            {
                for (int index = 0; index < centers.Length; index++)
                {
                    focus.transform.position = owner.transform.TransformPoint(centers[index]);
                    game.CameraRig.Target = focus.transform;
                    game.CameraRig.Distance = 24;
                    game.CameraRig.Snap();
                    yield return new WaitForSecondsRealtime(.15f);
                    Check(Mathf.Abs(game.CameraRig.Yaw - originalYaw) < .001f,
                        "Street capture retains the gameplay camera yaw " + names[index]);
                    yield return Capture("street-surface-" + names[index] + ".png");
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

        static float StreetSurfaceArea(WorldStreetSurface surface)
        {
            Vector3 origin = surface.Outline[0];
            float area = 0;
            for (int index = 1; index < surface.Outline.Count - 1; index++)
                area += Vector3.Cross(surface.Outline[index] - origin,
                    surface.Outline[index + 1] - origin).magnitude * .5f;
            return area;
        }
    }
}
