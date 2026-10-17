using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    // Giả định Stage 6: catalog là nguồn route duy nhất; EditMode không dựng scene/NavMesh.
    // Pass khi route/anchor hữu hạn, corridor giữ chỗ được áp dụng và graph công khai không cắt route cấm.
    public sealed class Stage6RouteTransitionTests
    {
        static readonly Dictionary<string, RouteKind> RequiredRoutes =
            new Dictionary<string, RouteKind>(StringComparer.Ordinal)
            {
                ["farm-dirt-path"] = RouteKind.FarmPath,
                ["sidewalk-crossings"] = RouteKind.Sidewalk,
                ["farm-sidewalk-connector"] = RouteKind.Sidewalk,
                ["highway-ring"] = RouteKind.Highway,
                ["rail-mainline"] = RouteKind.Rail,
                ["dock-harbor-approach"] = RouteKind.Dock,
                ["forest-access"] = RouteKind.FarmPath,
                ["expansion-industrial-approach"] = RouteKind.Highway
            };

        [Test]
        public void CatalogIsSingleFiniteSourceForStage6RouteKinds()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            Assert.That(routes, Is.Not.Null);
            Assert.That(WorldRouteCatalog.ValidateContinuous(out string failure), Is.True, failure);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < routes.Count; index++)
            {
                WorldRouteCorridor route = routes[index];
                Assert.That(route, Is.Not.Null);
                Assert.That(seen.Add(route.Id), Is.True, route.Id + " is duplicated");
                Assert.That(route.ReservesPlacement, Is.True, route.Id + " must reserve placement");
                Assert.That(route.Nodes, Is.Not.Null);
                Assert.That(route.Nodes.Count, Is.GreaterThanOrEqualTo(2), route.Id);
                for (int node = 1; node < route.Nodes.Count; node++)
                    Assert.That((route.Nodes[node] - route.Nodes[node - 1]).sqrMagnitude,
                        Is.GreaterThan(.0001f), route.Id + " has a zero edge");
            }

            foreach (KeyValuePair<string, RouteKind> required in RequiredRoutes)
            {
                Assert.That(WorldRouteCatalog.TryGet(required.Key, out WorldRouteCorridor route),
                    Is.True, required.Key + " is missing from the catalog");
                Assert.That(route.Kind, Is.EqualTo(required.Value), required.Key);
            }
        }

        [Test]
        public void ReservedCorridorsBlockPlacementAndTreeClearance()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            for (int index = 0; index < routes.Count; index++)
            {
                WorldRouteCorridor route = routes[index];
                Vector3 midpoint = Vector3.Lerp(route.Nodes[0], route.Nodes[1], .5f);

                Assert.That(WorldRouteCatalog.IsReserved(midpoint, .1f, out string routeId), Is.True,
                    route.Id + " midpoint is not reserved");
                Assert.That(WorldRouteDiagnostics.IsReservedByKind(
                    midpoint, .1f, route.Kind, out string kindRoute), Is.True, route.Id);
                Assert.That(kindRoute, Is.EqualTo(route.Id), route.Id);

                for (int profile = 0; profile < WorldVegetationCatalog.Profiles.Count; profile++)
                {
                    WorldVegetationProfile vegetation = WorldVegetationCatalog.Profiles[profile];
                    Assert.That(WorldVegetationCatalog.IsInsideCorridor(
                        midpoint, routes, vegetation.CorridorClearance), Is.True,
                        vegetation.Id + " may place on " + route.Id);
                }

                if (route.Kind != RouteKind.Highway && route.Kind != RouteKind.Rail
                    && route.Kind != RouteKind.Dock)
                    continue;

                Vector3 direction = route.Nodes[1] - route.Nodes[0];
                direction.y = 0;
                Vector3 lateral = new Vector3(-direction.z, 0, direction.x).normalized;
                Vector3 first = midpoint - lateral * 20f;
                Vector3 second = midpoint + lateral * 20f;
                Assert.That(WorldRouteDiagnostics.ValidateLeg(
                    new[] { first, second }, .25f, out _), Is.False,
                    route.Id + " must reject a prop/item leg crossing its corridor");
            }
        }

        [Test]
        public void FarmConnectorIndustrialStationTransitionHasValidAnchorsAndPublicRoute()
        {
            Assert.That(WorldExpansionCatalog.Validate(out string expansionFailure), Is.True,
                expansionFailure);
            WorldRouteCorridor farm = GetRoute("farm-dirt-path");
            WorldRouteCorridor connector = GetRoute("farm-sidewalk-connector");
            WorldRouteCorridor industrial = GetRoute("expansion-industrial-approach");
            WorldRouteCorridor rail = GetRoute("rail-mainline");

            WorldExpansionPoi industrialPoi = FindPoi("IndustrialYard");
            Vector3 farmPoint = CityDistricts.Center("farm");
            Vector3 connectorPoint = connector.Nodes[connector.Nodes.Count - 1];
            Vector3 industrialPoint = industrialPoi.RouteAnchors[industrialPoi.RouteAnchors.Count - 1];
            Vector3 stationPoint = FindMapPoint("station");
            Vector3[] transition = { farmPoint, connectorPoint, industrialPoint, stationPoint };

            AssertNearest(farm, farmPoint, 120f);
            AssertNearest(connector, connectorPoint, connector.Width + .1f);
            AssertNearest(industrial, industrialPoint, industrial.Width + .1f);
            AssertNearest(rail, stationPoint, 20f);

            IReadOnlyList<Vector3> smooth = CityAccessPaths.CreateSmoothPath(transition, 4);
            Assert.That(smooth, Is.Not.Null);
            Assert.That(smooth[0], Is.EqualTo(farmPoint));
            Assert.That(smooth[smooth.Count - 1], Is.EqualTo(stationPoint));
            for (int index = 0; index < transition.Length; index++)
            {
                Assert.That(IsFinite(transition[index]), Is.True, "transition anchor " + index);
                Assert.That(IsInsideStreetBounds(transition[index]), Is.False,
                    "transition anchor " + index + " is inside a static street collision");
            }

            bool planned = CityPedestrianNetwork.TryPlan(
                farmPoint, stationPoint, out List<Vector3> pedestrianRoute, out string failure);
            Assert.That(planned, Is.True, failure);
            Assert.That(pedestrianRoute, Is.Not.Null);
            Assert.That(pedestrianRoute[pedestrianRoute.Count - 1], Is.EqualTo(stationPoint));
            Assert.That(CityPedestrianNetwork.ValidateRoute(
                pedestrianRoute, .3f, out failure), Is.True, failure);
        }

        static WorldRouteCorridor GetRoute(string id)
        {
            Assert.That(WorldRouteCatalog.TryGet(id, out WorldRouteCorridor route), Is.True, id);
            return route;
        }

        static WorldExpansionPoi FindPoi(string id)
        {
            IReadOnlyList<WorldExpansionPoi> pois = WorldExpansionCatalog.BuildDefault();
            for (int index = 0; index < pois.Count; index++)
                if (string.Equals(pois[index].Id, id, StringComparison.Ordinal))
                    return pois[index];
            Assert.Fail("Missing expansion POI " + id);
            return default;
        }

        static Vector3 FindMapPoint(string id)
        {
            for (int index = 0; index < CityMapLocations.Points.Length; index++)
                if (string.Equals(CityMapLocations.Points[index].id, id, StringComparison.Ordinal))
                    return CityMapLocations.Points[index].position;
            Assert.Fail("Missing map point " + id);
            return default;
        }

        static void AssertNearest(WorldRouteCorridor route, Vector3 point, float maxDistance)
        {
            Assert.That(WorldRouteCatalog.TryGetNearestPoint(
                route.Id, point, maxDistance, out _, out float distance), Is.True,
                route.Id + " anchor is too far from its route");
            Assert.That(distance, Is.LessThanOrEqualTo(maxDistance));
        }

        static bool IsInsideStreetBounds(Vector3 point)
        {
            foreach (Bounds street in CityStreetNetwork.Bounds())
                if (street.Contains(point))
                    return true;
            return false;
        }

        static bool IsFinite(Vector3 point)
        {
            return IsFinite(point.x) && IsFinite(point.y) && IsFinite(point.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
