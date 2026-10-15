using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldRouteCorridorTests
    {
        [Test]
        public void DefaultRoutesAreContinuousAndCoverRequiredKinds()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();

            Assert.That(routes, Is.Not.Null);
            Assert.That(routes.Count, Is.EqualTo(13));
            Assert.That(WorldRouteCatalog.ValidateContinuous(), Is.True);
            Assert.That(WorldRouteCatalog.TryGet("farm-service-loop", out _), Is.True);

            var kinds = new HashSet<RouteKind>();
            for (int index = 0; index < routes.Count; index++)
                kinds.Add(routes[index].Kind);
            Assert.That(kinds.Count, Is.EqualTo(5));
        }

        [Test]
        public void ReservedCorridorBlocksPointOnItsPath()
        {
            WorldRouteCorridor route = WorldRouteCatalog.BuildDefault()[0];
            Vector3 point = Vector3.Lerp(route.Nodes[0], route.Nodes[1], .5f);

            bool reserved = WorldRouteCatalog.IsReserved(point, .25f, out string corridorId);

            Assert.That(reserved, Is.True);
            Assert.That(corridorId, Is.EqualTo(route.Id));
        }

        [Test]
        public void ReservedCorridorDoesNotBlockPointFarFromEveryRoute()
        {
            bool reserved = WorldRouteCatalog.IsReserved(new Vector3(520, 0, 40), .5f,
                out string corridorId);

            Assert.That(reserved, Is.False);
            Assert.That(corridorId, Is.Null);
        }

        [Test]
        public void CorridorCopiesInputNodesAndSupportsExactLookup()
        {
            var sourceNodes = new[]
            {
                new Vector3(1, 0, 2),
                new Vector3(3, 0, 2)
            };
            var corridor = new WorldRouteCorridor("test-route", RouteKind.Sidewalk, sourceNodes, 2, true);
            sourceNodes[0] = new Vector3(100, 0, 100);

            Assert.That(corridor.Nodes[0], Is.EqualTo(new Vector3(1, 0, 2)));
            Assert.That(WorldRouteCatalog.TryGet("rail-mainline", out WorldRouteCorridor rail), Is.True);
            Assert.That(rail.Kind, Is.EqualTo(RouteKind.Rail));
            Assert.That(WorldRouteCatalog.TryGet("missing-route", out _), Is.False);
        }

        [Test]
        public void ExpansionAccessRoutesUseCurvedIntermediateNodes()
        {
            Assert.That(WorldRouteCatalog.TryGet("expansion-forest-access", out WorldRouteCorridor forest),
                Is.True);
            Assert.That(WorldRouteCatalog.TryGet("expansion-industrial-approach", out WorldRouteCorridor industrial),
                Is.True);
            Assert.That(forest.Nodes.Count, Is.GreaterThanOrEqualTo(8));
            Assert.That(industrial.Nodes.Count, Is.GreaterThanOrEqualTo(8));
            Assert.That(HasHeadingChange(forest.Nodes), Is.True);
            Assert.That(HasHeadingChange(industrial.Nodes), Is.True);
        }

        [Test]
        public void SouthHighwayConnectsRingToExpansionRoad()
        {
            Assert.That(WorldRouteCatalog.TryGet("highway-ring", out WorldRouteCorridor ring), Is.True);
            Assert.That(WorldRouteCatalog.TryGet("expansion-south-highway", out WorldRouteCorridor south),
                Is.True);
            Assert.That(Vector3.Distance(ring.Nodes[3], south.Nodes[0]), Is.LessThan(.01f));
            Assert.That(Vector3.Distance(south.Nodes[4], new Vector3(190, 0, -300)), Is.LessThan(.01f));
            Assert.That(HasHeadingChange(south.Nodes), Is.True);
        }

        static bool HasHeadingChange(IReadOnlyList<Vector3> nodes)
        {
            for (int index = 1; index < nodes.Count - 1; index++)
            {
                Vector3 incoming = nodes[index] - nodes[index - 1];
                Vector3 outgoing = nodes[index + 1] - nodes[index];
                float turn = incoming.x * outgoing.z - incoming.z * outgoing.x;
                if (Mathf.Abs(turn) > .01f)
                    return true;
            }
            return false;
        }
    }
}
