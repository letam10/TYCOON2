using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldExpansionContractTests
    {
        static readonly string[] ExpansionRouteIds =
        {
            "expansion-forest-access",
            "expansion-industrial-approach",
            "expansion-village-road",
            "expansion-marina-dock",
            "expansion-south-highway"
        };

        [Test]
        public void ExpansionCatalogIsFiniteInsideDoubledBounds()
        {
            Assert.That(WorldExpansionCatalog.Validate(out string reason), Is.True, reason);
            IReadOnlyList<WorldExpansionPoi> pois = WorldExpansionCatalog.BuildDefault();
            Assert.That(pois, Has.Count.EqualTo(5));

            for (int index = 0; index < pois.Count; index++)
            {
                WorldExpansionPoi poi = pois[index];
                Assert.That(WorldExpansionCatalog.ExpandedWorldBounds.Contains(poi.Center), Is.True,
                    poi.Id + " center is outside expanded bounds");
                Assert.That(poi.RouteAnchors, Is.Not.Empty, poi.Id + " has no approach anchors");
                for (int anchor = 0; anchor < poi.RouteAnchors.Count; anchor++)
                    Assert.That(WorldExpansionCatalog.ExpandedWorldBounds.Contains(poi.RouteAnchors[anchor]), Is.True,
                        poi.Id + " approach anchor is outside expanded bounds");
            }
        }

        [Test]
        public void ExpansionRoutesHaveUniqueReservedKindsAndContinuousNodes()
        {
            Assert.That(WorldRouteCatalog.ValidateContinuous(), Is.True);
            var seen = new HashSet<string>();
            for (int index = 0; index < ExpansionRouteIds.Length; index++)
            {
                string id = ExpansionRouteIds[index];
                Assert.That(WorldRouteCatalog.TryGet(id, out WorldRouteCorridor route), Is.True, id);
                Assert.That(seen.Add(route.Id), Is.True, id + " is duplicated");
                Assert.That(route.ReservesPlacement, Is.True, id + " must reserve placement");
                Assert.That(route.Nodes.Count, Is.GreaterThanOrEqualTo(2));
                for (int node = 1; node < route.Nodes.Count; node++)
                    Assert.That((route.Nodes[node] - route.Nodes[node - 1]).sqrMagnitude, Is.GreaterThan(.0001f));
            }
        }

        [Test]
        public void ExpansionMapDestinationsUseReservedApproachCorridors()
        {
            for (int index = 0; index < CityMapLocations.Points.Length; index++)
            {
                var point = CityMapLocations.Points[index];
                if (!point.id.Contains("_")) continue;
                bool expansion = point.id is "forest_reserve" or "industrial_yard" or "north_village"
                    or "east_marina" or "south_highway";
                if (!expansion) continue;

                bool reserved = WorldRouteCatalog.IsReserved(point.position, .25f, out string routeId);
                Assert.That(reserved, Is.True, point.id + " is not on an approach route");
                StringAssert.StartsWith("expansion-", routeId);
            }
        }
    }
}
