using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldRoundedIntegrationTests
    {
        [Test]
        public void EveryCurrentCatalogPolygonHasActualCookedColliderSupport()
        {
            var root = new GameObject("RoundedPathColliderRegression");
            try
            {
                WorldStreetLayout streets = WorldStreetGeometry.Build(WorldRouteCatalog.BuildStreetNetwork());
                WorldPathSurfaceLayout layout = WorldPathSurfaceGeometry.Build(
                    WorldPathSurfaceCatalog.BuildDefault(), streets.Pavements.Concat(streets.Sidewalks).ToArray());
                WorldPathSurfaceMesh.Build(root.transform, layout);
                MeshCollider[] colliders = root.GetComponentsInChildren<MeshCollider>();
                Physics.SyncTransforms();
                for (int index = 0; index < layout.Pieces.Count; index++)
                {
                    WorldPathSurfacePiece piece = layout.Pieces[index];
                    Vector3 center = Vector3.zero;
                    foreach (Vector3 point in piece.Geometry.Outline)
                        center += point;
                    center /= piece.Geometry.Outline.Count;
                    Vector3 expected = center + Vector3.up * piece.Request.Top;
                    var ray = new Ray(expected + Vector3.up * .5f, Vector3.down);
                    bool matched = colliders.Any(collider => collider.Raycast(ray, out RaycastHit hit, 1f)
                        && Vector3.Distance(hit.point, expected) < .002f);
                    double area = 0;
                    Vector3 origin = piece.Geometry.Outline[0];
                    for (int vertex = 1; vertex + 1 < piece.Geometry.Outline.Count; vertex++)
                    {
                        Vector3 first = piece.Geometry.Outline[vertex] - origin;
                        Vector3 second = piece.Geometry.Outline[vertex + 1] - origin;
                        area += System.Math.Abs((double)first.x * second.z - (double)first.z * second.x) * .5;
                    }
                    Assert.That(matched, Is.True, piece.Request.Id + ":" + index + " area=" + area
                        + " center=" + center.ToString("R") + " outline="
                        + string.Join(";", piece.Geometry.Outline.Select(point => point.ToString("R"))));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SignalsKeepIdsCyclesAndExactShoulderDistanceFromSampledRing()
        {
            string[] ids = { "west_south", "west_mid", "west_north", "east_south", "east_mid", "east_north" };
            float[] offsets = { 0, 4, 8, 4, 8, 0 };
            Assert.That(CityTrafficSignalSystem.Definitions.Count, Is.EqualTo(ids.Length));
            for (int index = 0; index < ids.Length; index++)
            {
                CityTrafficSignalDefinition signal = CityTrafficSignalSystem.Definitions[index];
                Assert.That(signal.Id, Is.EqualTo(ids[index]));
                Assert.That(signal.CycleOffset, Is.EqualTo(offsets[index]));
                Assert.That(WorldRouteCatalog.TryGetNearestPoint("highway-ring", signal.Position,
                    -1, out _, out float distance), Is.True);
                Assert.That(distance, Is.EqualTo(6.2f).Within(.0001f));
                Assert.That(WorldRouteCatalog.IsReserved(signal.Position, 5, out string owner), Is.True);
                Assert.That(owner, Is.EqualTo("highway-ring"));
            }
        }

        [Test]
        public void PublicApproachRecognizesActualWidthAndRejectsAirAndRemotePoints()
        {
            Assert.That(WorldRouteCatalog.IsPublicApproachPoint(new(330, 0, 250)), Is.True);
            Assert.That(WorldRouteCatalog.IsPublicApproachPoint(new(330, 10, 250)), Is.False);
            Assert.That(WorldRouteCatalog.IsPublicApproachPoint(new(520, 0, 40)), Is.False);
            Assert.That(WorldRouteCatalog.IsPublicApproachPoint(new(float.NaN, 0, 250)), Is.False);
        }

        [Test]
        public void RoundedJunctionRetainsOnlyTheLocalAuthoredConnection()
        {
            var bend = new WorldRouteCorridor("rounded-parent", RouteKind.Highway,
                new[] { new Vector3(0, 0, 20), Vector3.zero, new Vector3(20, 0, 0) }, 4, true);
            var branch = new WorldRouteCorridor("authored-branch", RouteKind.Highway,
                new[] { Vector3.zero, new Vector3(-20, 0, -20) }, 4, true);
            Assert.That(WorldRouteDiagnostics.ValidateCrossRouteOverlaps(new[] { bend, branch },
                out string failure), Is.True, failure);
        }
    }
}
