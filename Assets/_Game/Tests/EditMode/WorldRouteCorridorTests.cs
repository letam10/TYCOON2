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
            Assert.That(routes.Count, Is.EqualTo(11));
            Assert.That(WorldRouteCatalog.ValidateContinuous(), Is.True);

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
            bool reserved = WorldRouteCatalog.IsReserved(new Vector3(260, 0, 205), .5f,
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
    }
}
