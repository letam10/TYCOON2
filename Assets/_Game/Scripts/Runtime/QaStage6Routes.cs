using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    // Assumption: catalog coordinates are authoritative; runtime acceptance uses the real NavMesh.
    // Pass requires finite catalog legs, matching route kinds, complete NavMesh paths and exact travel stops.
    public sealed partial class QaDriver
    {
        const float Stage6NavMeshSampleRadius = 3f;
        const float Stage6EndpointTolerance = 1.5f;

        IEnumerator Stage6RouteAcceptance()
        {
            yield return CompletionWait(
                () => game.NavigationReady && !game.IsRestoring,
                30,
                "Stage 6 waits for NavigationReady and a stable restore");

            Check(
                WorldRouteCatalog.ValidateContinuous(out string routeFailure),
                "Stage 6 route catalog is continuous: " + routeFailure);

            WorldRouteCorridor connector = FindRoute("farm-sidewalk-connector");
            CheckRouteDefinition(connector, RouteKind.Sidewalk, "farm-sidewalk-connector");

            WorldExpansionPoi industrial = FindPoi("IndustrialYard");
            Check(industrial.RouteAnchors != null && industrial.RouteAnchors.Count > 0,
                "IndustrialYard has a route anchor");
            Vector3 industrialAnchor = industrial.RouteAnchors[industrial.RouteAnchors.Count - 1];
            Check(IsFinite(industrialAnchor), "IndustrialYard route anchor is finite");

            if (!TryFindMapPoint("station", out Vector3 stationPoint))
                Check(false, "station map point exists");

            CheckRouteKind(
                CityDistricts.PlayerSpawn,
                RouteKind.FarmPath,
                8f,
                "farm endpoint uses a farm route");
            CheckRouteKind(
                connector.Nodes[0],
                RouteKind.Sidewalk,
                connector.Width + 1f,
                "connector start uses a sidewalk route");
            CheckRouteKind(
                connector.Nodes[connector.Nodes.Count - 1],
                RouteKind.Sidewalk,
                connector.Width + 1f,
                "connector end uses a sidewalk route");
            CheckRouteKind(
                industrialAnchor,
                RouteKind.Highway,
                12f,
                "IndustrialYard anchor uses a highway route");
            CheckRouteKind(
                stationPoint,
                RouteKind.Rail,
                20f,
                "station endpoint uses the rail route");

            yield return Stage6Visit(CityDistricts.PlayerSpawn, "farm");
            yield return Capture("stage6-farm.png");

            yield return Stage6Visit(connector.Nodes[0], "connector-start");
            yield return Stage6Visit(
                connector.Nodes[connector.Nodes.Count - 1],
                "connector-end");
            yield return Capture("stage6-connector.png");

            yield return Stage6Visit(industrialAnchor, "industrial");
            yield return Capture("stage6-industrial.png");

            yield return Stage6Visit(stationPoint, "station");
            yield return Capture("stage6-station.png");

            Check(game.RuntimeErrors.Count == 0, "Stage 6 route acceptance has no runtime errors");
        }

        IEnumerator Stage6Visit(Vector3 target, string label)
        {
            Vector3 start = game.Player.transform.position;
            Check(IsFinite(target), label + " endpoint is finite");
            CheckNavMeshLeg(start, target, label);
            yield return Travel(target);
            Check(
                Vector3.Distance(game.Player.transform.position, target) <= Stage6EndpointTolerance,
                label + " travel reaches its endpoint");
        }

        void CheckNavMeshLeg(Vector3 start, Vector3 target, string label)
        {
            bool startSampled = NavMesh.SamplePosition(
                start,
                out NavMeshHit startHit,
                Stage6NavMeshSampleRadius,
                NavMesh.AllAreas);
            Check(startSampled, label + " start has a NavMesh sample");

            bool targetSampled = NavMesh.SamplePosition(
                target,
                out NavMeshHit targetHit,
                Stage6NavMeshSampleRadius,
                NavMesh.AllAreas);
            Check(targetSampled, label + " endpoint has a NavMesh sample");

            var path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(
                startHit.position,
                targetHit.position,
                NavMesh.AllAreas,
                path);
            Check(
                calculated && path.status == NavMeshPathStatus.PathComplete,
                label + " NavMesh path is complete");
        }

        void CheckRouteDefinition(WorldRouteCorridor route, RouteKind expected, string label)
        {
            Check(route != null, label + " exists in WorldRouteCatalog");
            if (route == null)
                return;
            Check(route.Kind == expected, label + " keeps route kind " + expected);
            Check(route.ReservesPlacement, label + " reserves its corridor");
            Check(route.Nodes != null && route.Nodes.Count >= 2, label + " has route nodes");
        }

        void CheckRouteKind(Vector3 point, RouteKind expected, float maxDistance, string label)
        {
            WorldRouteCorridor route = FindNearestRoute(point, expected, out float distance);
            Check(route != null, label + " finds a " + expected + " route");
            if (route == null)
                return;
            Check(route.ReservesPlacement, label + " route reserves placement");
            Check(distance <= maxDistance, label + " stays on the catalog route");
        }

        static WorldRouteCorridor FindRoute(string id)
        {
            return WorldRouteCatalog.TryGet(id, out WorldRouteCorridor route) ? route : null;
        }

        static WorldRouteCorridor FindNearestRoute(
            Vector3 point,
            RouteKind kind,
            out float distance)
        {
            WorldRouteCorridor nearest = null;
            distance = float.PositiveInfinity;
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildDefault();
            for (int index = 0; index < routes.Count; index++)
            {
                WorldRouteCorridor route = routes[index];
                if (route.Kind != kind)
                    continue;
                if (!WorldRouteCatalog.TryGetNearestPoint(
                    route.Id,
                    point,
                    float.PositiveInfinity,
                    out _,
                    out float candidate))
                    continue;
                if (candidate >= distance)
                    continue;
                nearest = route;
                distance = candidate;
            }
            return nearest;
        }

        static WorldExpansionPoi FindPoi(string id)
        {
            IReadOnlyList<WorldExpansionPoi> pois = WorldExpansionCatalog.BuildDefault();
            for (int index = 0; index < pois.Count; index++)
                if (string.Equals(pois[index].Id, id, StringComparison.Ordinal))
                    return pois[index];
            return default;
        }

        static bool TryFindMapPoint(string id, out Vector3 point)
        {
            for (int index = 0; index < CityMapLocations.Points.Length; index++)
            {
                if (!string.Equals(CityMapLocations.Points[index].id, id, StringComparison.Ordinal))
                    continue;
                point = CityMapLocations.Points[index].position;
                return IsFinite(point);
            }
            point = default;
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
