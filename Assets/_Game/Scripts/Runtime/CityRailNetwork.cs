using UnityEngine;

namespace Tycoon
{
    public static class CityRailNetwork
    {
        public const float StationX = 10;
        public const float WestEnd = -97;
        public const float EastEnd = 112;
        public const float PlatformHeight = .12f;
        public const float BridgeHeight = 4.8f;
        public const float BridgeX = 34;
        public const float RampLength = 18.5f;
        public static readonly float[] Tracks = { -61, -67 };
        public static readonly float[] Platforms = { -57.5f, -70.5f };
        public static Bounds Corridor => new(new Vector3(6, 2, -63), new Vector3(272, 8, 21));
        public static Bounds StationBounds => new(new Vector3(10, 2, -61), new Vector3(67, 8, 25));

        public static bool TryGetMainline(out WorldRouteCorridor route)
        {
            return WorldRouteCatalog.TryGet("rail-mainline", out route) &&
                route.Kind == RouteKind.Rail;
        }

        public static bool ValidateRouteCatalog(out string failure)
        {
            failure = null;
            if (!TryGetMainline(out WorldRouteCorridor route))
            {
                failure = "rail-mainline is missing or has the wrong kind";
                return false;
            }
            if (route.Nodes.Count < 2)
            {
                failure = "rail-mainline has fewer than two nodes";
                return false;
            }
            for (int index = 0; index < Tracks.Length; index++)
            {
                Vector3 trackPoint = new(route.Nodes[0].x, 0, Tracks[index]);
                if (!WorldRouteCatalog.TryGetNearestPoint(
                    route.Id, trackPoint, route.Width * .5f, out _, out _))
                {
                    failure = "track " + index + " is outside the rail corridor";
                    return false;
                }
            }
            return true;
        }

        public static Vector3 PlatformPoint(int track, float x = 10) =>
            new(x, PlatformHeight, Platforms[Mathf.Clamp(track, 0, 1)]);

        public static float SafeSpeed(float distance, float maximum, float acceleration)
        {
            return Mathf.Min(maximum, Mathf.Sqrt(2 * acceleration * Mathf.Max(0, distance)));
        }

        public static void BuildTracks(CityStaticGeometry g)
        {
            foreach (float z in Tracks)
            {
                g.Box(new(6, .018f, z), new(272, .035f, 3.35f), "stone", "#898C7C");
                for (float x = -129; x < 142; x += .92f)
                    g.Box(new(x, .065f, z), new(.23f, .11f, 2.35f), "wood", "#726F61");
                for (int side = -1; side <= 1; side += 2)
                {
                    g.Box(new(6, .15f, z + side * .72f), new(272, .07f, .075f), "metal", "#8C9595");
                    g.Box(new(6, .105f, z + side * .72f), new(272, .05f, .14f), "metal", "#616B69");
                }
            }
        }
    }
}
