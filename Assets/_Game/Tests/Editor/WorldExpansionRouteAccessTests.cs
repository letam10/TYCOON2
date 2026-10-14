using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class WorldExpansionRouteAccessTests
    {
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("WorldExpansionRouteAccessTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MarinaDockRoutePassesTheExpansionBoatDoor()
        {
            root.transform.position = new Vector3(420, 0, -120);
            WorldExpansionBoat boat = WorldExpansionBoat.Build(
                root.transform, new Vector3(20, 0, -27));
            Vector3 door = boat.DoorOutside;
            WorldRouteCorridor route = WorldRouteCatalog.BuildDefault()
                .Single(candidate => candidate.Id == "expansion-marina-dock");

            float distance = DistanceToRoute(door, route.Nodes);
            Assert.That(distance, Is.LessThanOrEqualTo(route.Width * .5f + .5f));

            WorldExpansionPoi poi = WorldExpansionCatalog.BuildDefault()
                .Single(candidate => candidate.Id == "EastMarina");
            Assert.That(poi.RouteAnchors.Any(anchor => Vector3.Distance(anchor, door) < .5f), Is.True);
        }

        static float DistanceToRoute(Vector3 point, System.Collections.Generic.IReadOnlyList<Vector3> nodes)
        {
            float closest = float.PositiveInfinity;
            for (int index = 1; index < nodes.Count; index++)
                closest = Mathf.Min(closest, DistanceToSegment(point, nodes[index - 1], nodes[index]));
            return closest;
        }

        static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 delta = end - start;
            delta.y = 0;
            Vector3 offset = point - start;
            offset.y = 0;
            float length = delta.sqrMagnitude;
            float amount = length <= .000001f
                ? 0
                : Mathf.Clamp01(Vector3.Dot(offset, delta) / length);
            Vector3 nearest = start + delta * amount;
            nearest.y = point.y;
            return Vector3.Distance(point, nearest);
        }
    }
}
