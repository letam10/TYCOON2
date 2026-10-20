using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public sealed class WorldStreetSurface
    {
        public string RouteId { get; }
        public int SegmentIndex { get; }
        public int Side { get; }
        public int PieceIndex { get; }
        public WorldRouteSegment Source { get; }
        public IReadOnlyList<Vector3> Outline { get; }

        public WorldStreetSurface(
            string routeId,
            int segmentIndex,
            int side,
            int pieceIndex,
            WorldRouteSegment source,
            IReadOnlyList<Vector3> outline)
        {
            RouteId = routeId;
            SegmentIndex = segmentIndex;
            Side = side;
            PieceIndex = pieceIndex;
            Source = source;
            Outline = new List<Vector3>(outline).AsReadOnly();
        }
    }

    public sealed class WorldStreetLayout
    {
        public IReadOnlyList<WorldStreetSurface> Pavements { get; }
        public IReadOnlyList<WorldStreetSurface> Sidewalks { get; }

        public WorldStreetLayout(
            IReadOnlyList<WorldStreetSurface> pavements,
            IReadOnlyList<WorldStreetSurface> sidewalks)
        {
            Pavements = new List<WorldStreetSurface>(pavements).AsReadOnly();
            Sidewalks = new List<WorldStreetSurface>(sidewalks).AsReadOnly();
        }
    }
}
