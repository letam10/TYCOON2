using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class WorldPathSurfaceCatalog
    {
        static readonly IReadOnlyList<WorldPathSurfaceRequest> defaults = CreateDefault();

        public static IReadOnlyList<WorldPathSurfaceRequest> BuildDefault()
        {
            return defaults;
        }

        public static WorldPathSurfaceRequest CreateLandmark(string landmarkId, int leg,
            IReadOnlyList<Vector3> points, float width)
        {
            return new WorldPathSurfaceRequest("landmark:" + landmarkId + ":" + leg,
                points, width, "paving", "#C0B8A0", 0, .05f, 200);
        }

        public static IReadOnlyList<WorldStreetSurface> BuildStreetBlockers()
        {
            WorldStreetLayout streets = WorldStreetGeometry.Build(WorldRouteCatalog.BuildStreetNetwork());
            var result = new List<WorldStreetSurface>(streets.Pavements);
            result.AddRange(streets.Sidewalks);
            return result.AsReadOnly();
        }

        static IReadOnlyList<WorldPathSurfaceRequest> CreateDefault()
        {
            var requests = new List<WorldPathSurfaceRequest>();
            foreach (WorldRouteCorridor route in WorldRouteCatalog.BuildDefault())
            {
                if (route.Kind == RouteKind.Rail)
                    continue;
                string surface = "paving";
                string tint = "#C0B8A0";
                int priority = 200;
                if (route.Kind == RouteKind.Dock)
                {
                    surface = "wood";
                    tint = "#A18B68";
                    priority = 400;
                }
                else if (route.Kind == RouteKind.Highway)
                {
                    surface = "stone";
                    tint = "#596365";
                    priority = 300;
                }
                else if (route.Kind == RouteKind.FarmPath)
                {
                    surface = "soil";
                    tint = "#9A754C";
                    priority = 100;
                }
                requests.Add(new WorldPathSurfaceRequest(route.Id, route.SampledNodes,
                    route.Width, surface, tint, .0005f, .0555f, priority));
            }
            foreach (WorldPublicApproach approach in WorldRouteCatalog.BuildPublicApproaches())
                requests.Add(new WorldPathSurfaceRequest(approach.Id, approach.SampledNodes,
                    approach.Width, "paving", "#B8B0A0", .0005f, .0555f, 200));
            return requests.AsReadOnly();
        }
    }
}
