using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Stage3RouteTrafficContractTests
    {
        static readonly string[] RequiredRoutes =
        {
            "farm-dirt-path",
            "farm-service-loop",
            "sidewalk-crossings",
            "farm-sidewalk-connector",
            "highway-ring",
            "rail-mainline",
            "dock-harbor-approach",
            "forest-access",
            "expansion-forest-access",
            "expansion-industrial-approach",
            "expansion-village-road",
            "expansion-marina-dock",
            "expansion-south-highway"
        };

        [Test]
        public void CatalogKeepsStableCorridorsAndReportsContinuousGeometry()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            Assert.That(routes, Has.Count.EqualTo(RequiredRoutes.Length));
            Assert.That(WorldRouteCatalog.ValidateContinuous(out string failure), Is.True, failure);
            for (int index = 0; index < RequiredRoutes.Length; index++)
                Assert.That(WorldRouteCatalog.TryGet(RequiredRoutes[index], out _), Is.True,
                    RequiredRoutes[index]);
        }

        [Test]
        public void CatalogProjectionAndReservationUseTheSameSegmentGeometry()
        {
            Assert.That(WorldRouteCatalog.TryGetNearestPoint(
                "rail-mainline", new Vector3(0, 2, -61), 5, out Vector3 nearest, out float distance),
                Is.True);
            Assert.That(nearest.z, Is.EqualTo(-64).Within(.001f));
            Assert.That(distance, Is.EqualTo(3).Within(.001f));
            Assert.That(WorldRouteCatalog.TryGetNearestPoint(
                "rail-mainline", new Vector3(0, 0, -61), 2, out _, out _), Is.False);

            Assert.That(WorldRouteCatalog.TryGet("highway-ring", out WorldRouteCorridor highway), Is.True);
            Vector3 point = Vector3.Lerp(highway.Nodes[0], highway.Nodes[1], .5f);
            Assert.That(WorldRouteCatalog.IsReserved(point, .25f, out string routeId), Is.True);
            Assert.That(routeId, Is.EqualTo(highway.Id));
        }

        [Test]
        public void FarmToShopRouteHasCapsuleClearanceAndNoBlockingCorridor()
        {
            bool planned = CityPedestrianNetwork.TryPlan(
                CityDistricts.Center("farm"),
                CityDistricts.Center("supermarket"),
                out List<Vector3> route,
                out string failure);

            Assert.That(planned, Is.True, failure);
            Assert.That(CityPedestrianNetwork.ValidateRoute(route, .3f, out failure), Is.True, failure);
        }

        [Test]
        public void EveryExpansionApproachIsReachableFromThePublicSidewalkNetwork()
        {
            Vector3 source = new(141, 0, -40.1f);
            IReadOnlyList<WorldExpansionPoi> pois = WorldExpansionCatalog.BuildDefault();
            for (int poiIndex = 0; poiIndex < pois.Count; poiIndex++)
            {
                WorldExpansionPoi poi = pois[poiIndex];
                for (int anchorIndex = 0; anchorIndex < poi.RouteAnchors.Count; anchorIndex++)
                {
                    bool planned = CityPedestrianNetwork.TryPlan(
                        source, poi.RouteAnchors[anchorIndex], out List<Vector3> route,
                        out string failure);
                    Assert.That(planned, Is.True, poi.Id + " anchor " + anchorIndex + ": " + failure);
                    Assert.That(CityPedestrianNetwork.ValidateRoute(
                        route, .3f, out failure), Is.True,
                        poi.Id + " anchor " + anchorIndex + ": " + failure);
                }
            }
        }

        [Test]
        public void RailAndSignalContractsRemainOnCatalogHighwayAndRailCorridors()
        {
            Assert.That(CityRailNetwork.ValidateRouteCatalog(out string railFailure), Is.True, railFailure);
            for (int index = 0; index < CityTrafficSignalSystem.Definitions.Count; index++)
            {
                Vector3 position = CityTrafficSignalSystem.Definitions[index].Position;
                Assert.That(WorldRouteCatalog.IsReserved(position, 5, out string routeId), Is.True,
                    "signal " + CityTrafficSignalSystem.Definitions[index].Id);
                Assert.That(routeId, Is.EqualTo("highway-ring"));
            }
        }
    }
}
